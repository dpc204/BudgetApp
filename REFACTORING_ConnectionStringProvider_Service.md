# Refactoring: Connection String Provider Service

## Overview

Refactored connection string resolution from repeated `Misc.GetConnectionString()` calls to a centralized `IConnectionStringProvider` service pattern.

## Benefits

### Before
- **Repeated logic**: `Misc.GetConnectionString()` called multiple times with `WebApplicationBuilder` and `ILogger`
- **Tight coupling**: Components needed `WebApplicationBuilder` reference
- **Hard to test**: Difficult to mock connection strings
- **Scattered resolution**: Connection logic duplicated across projects

### After
- **Single source of truth**: Connection strings resolved once at startup
- **Dependency injection**: Service can be injected anywhere
- **Easy to test**: Simple interface to mock
- **Cleaner code**: No need to pass `WebApplicationBuilder` around

## Implementation

### 1. Interface

```csharp
public interface IConnectionStringProvider
{
  string BudgetConnectionString { get; }
  string IdentityConnectionString { get; }
  bool UseAzureDatabase { get; }
}
```

### 2. Implementation

```csharp
public sealed class ConnectionStringProvider : IConnectionStringProvider
{
  public string BudgetConnectionString { get; }
  public string IdentityConnectionString { get; }
  public bool UseAzureDatabase { get; }

  // Factory method that calls Misc.GetConnectionString once
  public static ConnectionStringProvider Create(WebApplicationBuilder builder, ILogger logger)
  {
    var useAzureDb = Misc.UseAzureDB(builder, logger);
    var budgetConnectionString = Misc.GetConnectionString(builder, Misc.ConnectionStringType.Budget, logger);
    var identityConnectionString = Misc.GetConnectionString(builder, Misc.ConnectionStringType.Identity, logger);
    
    return new ConnectionStringProvider(budgetConnectionString, identityConnectionString, useAzureDb);
  }
}
```

### 3. Registration

**Budget.Api\Program.cs:**
```csharp
// Register connection string provider as a singleton
IConnectionStringProvider connectionStringProvider;
if (isTest)
{
  connectionStringProvider = new ConnectionStringProvider("TestConnection", "TestConnection", useAzureDatabase: false);
}
else
{
  connectionStringProvider = ConnectionStringProvider.Create(builder, logger);
}
builder.Services.AddSingleton<IConnectionStringProvider>(connectionStringProvider);

// Use it immediately
var budgetConnectionString = connectionStringProvider.BudgetConnectionString;
var identityConnectionString = connectionStringProvider.IdentityConnectionString;
```

**Budget.Web\Program.cs:**
```csharp
// Register connection string provider as a singleton
var connectionStringProvider = ConnectionStringProvider.Create(builder, logger);
builder.Services.AddSingleton<IConnectionStringProvider>(connectionStringProvider);
```

### 4. Usage in Services

**Budget.Web\Startup\ConfigureServices.cs:**
```csharp
public static void AddApplicationServices(WebApplicationBuilder builder)
{
  // Get connection string from the registered provider
  var serviceProvider = builder.Services.BuildServiceProvider();
  var connectionStringProvider = serviceProvider.GetService<IConnectionStringProvider>();
  var sqlConnection = connectionStringProvider?.BudgetConnectionString 
    ?? builder.Configuration["LocalBudgetConnection"] 
    ?? builder.Configuration["BudgetConnection"];
  
  // Use for distributed cache, etc.
  builder.Services.AddDistributedSqlServerCache(options =>
  {
    options.ConnectionString = sqlConnection;
    // ...
  });
}
```

## Files Created

- `Budget.Shared\Services\IConnectionStringProvider.cs` - Interface
- `Budget.Shared\Services\ConnectionStringProvider.cs` - Implementation

## Files Modified

- `Budget.Api\Program.cs` - Registers and uses the service
- `Budget.Web\Program.cs` - Registers the service
- `Budget.Web\Startup\ConfigureServices.cs` - Uses the service for distributed cache

## Usage Examples

### In a Controller/Handler

```csharp
public class MyHandler(IConnectionStringProvider connectionStrings)
{
  public void DoSomething()
  {
    var connString = connectionStrings.BudgetConnectionString;
    // Use it...
  }
}
```

### In Tests

```csharp
[Fact]
public void Test_Something()
{
  var mockProvider = new Mock<IConnectionStringProvider>();
  mockProvider.Setup(x => x.BudgetConnectionString).Returns("TestConnection");
  
  var handler = new MyHandler(mockProvider.Object);
  // Test...
}
```

### For Configuration

```csharp
// DbContext registration
builder.Services.AddDbContext<BudgetContext>((serviceProvider, options) =>
{
  var connStrings = serviceProvider.GetRequiredService<IConnectionStringProvider>();
  options.UseSqlServer(connStrings.BudgetConnectionString);
});
```

## Testing Strategy

The service makes testing easier:

**Integration Tests:**
```csharp
builder.ConfigureServices(services =>
{
  services.AddSingleton<IConnectionStringProvider>(
    new ConnectionStringProvider("InMemoryDb", "InMemoryDb", false));
});
```

**Unit Tests:**
```csharp
var mock = new Mock<IConnectionStringProvider>();
mock.Setup(x => x.BudgetConnectionString).Returns("MockedConnection");
```

## Migration Path

### Old Code
```csharp
var connString = Misc.GetConnectionString(builder, Misc.ConnectionStringType.Budget, logger);
```

### New Code
```csharp
// At startup (once)
var provider = ConnectionStringProvider.Create(builder, logger);
builder.Services.AddSingleton<IConnectionStringProvider>(provider);

// In services (inject)
public class MyService(IConnectionStringProvider connectionStrings)
{
  var connString = connectionStrings.BudgetConnectionString;
}
```

## Best Practices

1. **Register early**: Register `IConnectionStringProvider` immediately after `SetupConfigurationSources`
2. **Use singleton**: Connection strings don't change during app lifetime
3. **Inject, don't build**: Use constructor injection, not `BuildServiceProvider()`
4. **Fallback support**: Service supports fallback to configuration for backwards compatibility
5. **Logging**: Factory method logs all resolved connection strings at startup

## Backwards Compatibility

The `Misc.GetConnectionString()` method is **still available** and working. The new service is **additive** and doesn't break existing code.

## Future Improvements

1. **Remove direct Misc calls**: Gradually migrate all `Misc.GetConnectionString()` calls to use the service
2. **Add validation**: Validate connection strings at startup
3. **Add health checks**: Monitor database connectivity
4. **Add metrics**: Track connection string usage

## Summary

This refactoring provides:
- ? **Single resolution point** for connection strings
- ? **Dependency injection friendly**
- ? **Testability** through interface
- ? **Performance** - resolved once, cached forever
- ? **Maintainability** - centralized logic
- ? **Backwards compatible** - existing code still works
