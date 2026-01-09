# Resolution: GitHub Actions LocalDB Test Failures

## Problem
Four tests in `UserEmailTriggerTests` were failing in GitHub Actions with:
```
System.PlatformNotSupportedException : LocalDB is not supported on this platform.
```

## Root Cause
The `UserEmailTriggerTests.cs` class had a **hardcoded LocalDB connection string** on line 30:
```csharp
.UseSqlServer($"Server=(localdb)\\mssqllocaldb;Database={_testDbName};Trusted_Connection=True;TrustServerCertificate=True;")
```

This meant:
- ? Other tests were using the SQL Server container we configured in the workflow
- ? `UserEmailTriggerTests` were ignoring the environment variables and trying to use LocalDB

## Solution

### 1. Updated `UserEmailTriggerTests.cs`
Added a `GetConnectionString()` method that:
- **Checks for CI/CD environment variables first** (`LocalBudgetConnection` or `BudgetConnection`)
- **Falls back to LocalDB** for local development
- **Replaces the database name** with a unique test database name

```csharp
private string GetConnectionString()
{
  // Check for CI/CD environment variables first
  var ciConnectionString = Environment.GetEnvironmentVariable("LocalBudgetConnection") 
                           ?? Environment.GetEnvironmentVariable("BudgetConnection");
  
  if (!string.IsNullOrEmpty(ciConnectionString))
  {
    // Replace database name in CI connection string
    var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(ciConnectionString)
    {
      InitialCatalog = _testDbName
    };
    return builder.ConnectionString;
  }
  
  // Fallback to LocalDB for local development
  return $"Server=(localdb)\\mssqllocaldb;Database={_testDbName};Trusted_Connection=True;TrustServerCertificate=True;";
}
```

### 2. GitHub Actions Workflow Already Configured
The `.github/workflows/project-tests.yml` workflow already has:
- ? SQL Server 2022 container service
- ? Environment variables set:
  - `BudgetConnection`
  - `LocalBudgetConnection`

## How It Works Now

### Local Development
- Tests detect no environment variables
- Falls back to LocalDB connection string
- Works as before for developers with LocalDB installed

### GitHub Actions CI/CD
- Tests detect `LocalBudgetConnection` environment variable
- Use SQL Server container on `localhost:1433`
- Create unique test database for isolation
- Tests pass successfully ?

## Files Changed
1. **Budget.ApiTests/UserEmailTriggerTests.cs**
   - Added `GetConnectionString()` method
   - Updated constructor to use dynamic connection string
   - Updated XML documentation comments

## Testing
```bash
# Local (uses LocalDB)
dotnet test

# CI/CD (uses SQL Server container - already configured in workflow)
# Runs automatically on push to any branch
```

## Benefits
? Tests work both locally and in CI/CD  
? No breaking changes for local development  
? SQL Server trigger tests now pass in GitHub Actions  
? Proper test isolation with unique database names  
? Single source of truth for connection strings  

## Related Files
- `.github/workflows/project-tests.yml` - GitHub Actions workflow configuration
- `Budget.ApiTests/UserEmailTriggerTests.cs` - Fixed test class
