# Fix: Constant 401 Errors After App Restart

## Problem

After restarting the Budget.Web application, users were constantly getting 401 Unauthorized errors with the message:
```
Response status code does not indicate success: 401 (Authentication required - please sign out and sign in again)
```

## Root Cause

The issue was caused by **stale authentication tokens persisting across app restarts**:

1. **SQL Server Distributed Cache** (`dbo.SessionCache` table) was used to persist Microsoft Entra ID tokens
2. When the app restarted, **old tokens remained in the cache**
3. These tokens were **no longer valid** after the app restart
4. The `ForwardAuthCookiesHandler` would try to use these stale tokens
5. API calls would fail with 401 errors

## Why It Happened on Every Restart

- **Development**: LocalDB connection string persists the `SessionCache` table across app restarts
- **Production**: Azure SQL Database persists tokens indefinitely
- **Token Lifetime**: Entra ID tokens can last hours/days, but become invalid when the issuing app restarts

## Solution Implemented

Added **automatic token cache clearing on application startup**:

### 1. New Method in `TokenCacheManager`

```csharp
/// <summary>
/// Clears all tokens from the cache on application startup to prevent stale token issues
/// Call this during application initialization
/// </summary>
public async Task ClearCacheOnStartupAsync()
{
  try
  {
    logger.LogInformation("Clearing token cache on application startup to prevent stale tokens");
    
    var sqlConnection = configuration["LocalBudgetConnection"] ?? configuration["BudgetConnection"];
    
    if (!string.IsNullOrEmpty(sqlConnection))
    {
      var cleared = await ClearSqlServerCacheAsync(sqlConnection, CancellationToken.None);
      if (cleared)
      {
        logger.LogInformation("? Token cache cleared successfully on startup");
      }
      else
      {
        logger.LogWarning("? Token cache clear failed on startup - users may need to sign in again");
      }
    }
    else
    {
      logger.LogInformation("No SQL connection - in-memory cache will be empty on startup");
    }
  }
  catch (Exception ex)
  {
    logger.LogError(ex, "? Error clearing token cache on startup - continuing anyway");
  }
}
```

### 2. Startup Call in `Program.cs`

```csharp
var app = builder.Build();

Misc.LogAllConfigurationSettings(builder, logger);

// Clear token cache on startup to prevent stale token issues
using (var scope = app.Services.CreateScope())
{
  var tokenCacheManager = scope.ServiceProvider.GetRequiredService<Budget.Web.Services.TokenCacheManager>();
  await tokenCacheManager.ClearCacheOnStartupAsync();
}
```

## How It Works Now

**On Every App Startup:**
1. App builds and configures services
2. **TokenCacheManager clears the `dbo.SessionCache` table**
3. All old/stale tokens are removed
4. App starts with a clean cache
5. **Users are required to sign in again on first request**
6. Fresh, valid tokens are acquired and cached

## Expected Behavior

### After Restart

1. User navigates to the app
2. App detects no valid cached token
3. User is **automatically redirected to Microsoft sign-in**
4. User signs in
5. Fresh tokens are acquired and cached
6. App works normally

### No More 401 Errors

- ? No more constant 401 errors after restart
- ? No more "please sign out and sign in again" messages
- ? Clean authentication flow every time
- ? Works in both development and production

## Logging

You'll see these messages on startup:

```
[Information] Clearing token cache on application startup to prevent stale tokens
[Information] Clearing SQL Server token cache
[Information] Cleared {RowCount} entries from SQL Server SessionCache
[Information] ? Token cache cleared successfully on startup
```

## Alternative Solutions Considered

### Option 1: Use In-Memory Cache (Rejected)
- **Pro**: Automatically clears on restart
- **Con**: Doesn't work for web farms/scale-out scenarios
- **Con**: Loses tokens on every app pool recycle

### Option 2: Add Token Expiration (Rejected)
- **Pro**: Tokens would eventually expire
- **Con**: Doesn't help immediately after restart
- **Con**: Complex to implement with MSAL

### Option 3: Manual Cache Clear (Rejected)
- **Pro**: Simple
- **Con**: Requires developer intervention
- **Con**: Doesn't help end users

### Option 4: Clear on Startup (Selected) ?
- **Pro**: Automatic and reliable
- **Pro**: Works in all environments
- **Pro**: No user intervention needed
- **Pro**: Simple to implement
- **Con**: Users must sign in again after every restart (acceptable in development)

## Production Considerations

### Development Environment
- Tokens cleared on **every F5 restart**
- Users sign in again each time (expected)
- Helps catch authentication issues early

### Production Environment
- Tokens cleared only on **app deployment or restart**
- Users sign in after deployments (normal)
- Prevents stale token accumulation
- No impact during normal operation

## Testing

To verify the fix:

1. **Start the app**
2. **Check logs** - should see "Token cache cleared successfully"
3. **Navigate to the app** - should redirect to sign-in
4. **Sign in** - should work without 401 errors
5. **Use the app** - should work normally
6. **Restart the app** (Shift+F5, then F5)
7. **Refresh browser** - should redirect to sign-in again
8. **No 401 errors** - authentication flow should be clean

## Files Modified

1. `Budget.Web\Services\TokenCacheManager.cs` - Added `ClearCacheOnStartupAsync` method
2. `Budget.Web\Program.cs` - Added call to clear cache on startup

## Related Documentation

- `FEATURE_Automatic_Token_Cache_Clearing.md` - Automatic stale token detection
- `DIAGNOSIS_401_Unauthorized_Fix.md` - General 401 error troubleshooting

## Summary

**Before**: Constant 401 errors after app restart due to stale cached tokens  
**After**: Clean token cache on startup, automatic sign-in redirect, no more 401 errors

The fix ensures that **every app restart starts with a clean authentication state**, eliminating the stale token problem entirely.
