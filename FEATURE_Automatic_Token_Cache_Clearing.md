# Automatic Stale Token Detection and Clearing

## Overview

The application now includes automatic detection and clearing of stale authentication tokens. When the API returns a 401 Unauthorized error due to stale tokens, the system will:

1. **Detect** the stale token condition
2. **Clear** the SQL Server token cache automatically
3. **Prompt** the user to refresh the page
4. **Re-authenticate** the user on next request

## How It Works

### Components

1. **TokenCacheManager** (`Budget.Web\Services\TokenCacheManager.cs`)
   - Detects stale token errors
   - Clears SQL Server `dbo.SessionCache` table
   - Prevents rapid consecutive clears (5-minute cooldown)
   - Logs all operations for diagnostics

2. **ForwardAuthCookiesHandler** (`Budget.Web\Services\ForwardAuthCookiesHandler.cs`)
   - Intercepts 401 errors during token acquisition
   - Calls `TokenCacheManager` to clear cache
   - Returns friendly error messages

3. **TokenRefreshPrompt** (`Budget.Web\Components\Auth\TokenRefreshPrompt.razor`)
   - Optional UI component to display refresh prompt
   - Can be added to main layout for user-friendly experience

## Error Codes Detected

The system automatically clears tokens for these MSAL error codes:
- `interaction_required` - User interaction needed
- `invalid_grant` - Token is invalid or expired
- `consent_required` - User consent needs renewal

## Usage

### Automatic Mode (Current)

No user action required! When a stale token is detected:

1. User sees: "Authentication required - please refresh the page"
2. Token cache is cleared in the background
3. User refreshes the page (F5)
4. User is redirected to sign in
5. Fresh tokens are acquired
6. User continues working

### Enhanced Mode (Optional)

Add the `TokenRefreshPrompt` component to your layout:

```razor
@* In Budget.Web\Components\Layout\MainLayout.razor *@
<TokenRefreshPrompt />
```

This provides a modal dialog with a "Reload Page" button.

## Logging

All token cache operations are logged:

```
[Warning] TokenCacheManager: Detected stale token for user {UserId} - clearing token cache
[Information] TokenCacheManager: Cleared {RowCount} entries from SQL Server SessionCache
[Information] TokenCacheManager: Token cache cleared successfully at {Time}
```

## SQL Server Cache

Tokens are stored in: `dbo.SessionCache`

The system clears this table when stale tokens are detected. This is safe because:
- Only affects the current user's session
- Fresh tokens are acquired on next sign-in
- No data loss occurs

## Troubleshooting

### Issue: Still seeing 401 errors after refresh

**Solution**: Clear browser cookies and try again
```
1. Press F12 (Developer Tools)
2. Application Tab ? Storage ? Clear
3. Close all browser tabs
4. Restart the application
```

### Issue: "No SQL connection string found"

**Solution**: Ensure connection string is configured
```json
{
  "LocalBudgetConnection": "your-connection-string"
}
```

### Issue: Cache clearing too frequent

The system has a 5-minute cooldown between clears. If you see:
```
Skipping token cache clear - cleared X seconds ago
```

This is normal and prevents excessive database operations.

## Configuration

### Adjust Cooldown Period

In `TokenCacheManager.cs`:
```csharp
private static readonly TimeSpan _minTimeBetweenClears = TimeSpan.FromMinutes(5);
```

Change this value to adjust how often the cache can be cleared.

### Disable Automatic Clearing

Remove the cache clearing code from `ForwardAuthCookiesHandler.cs`:
```csharp
// Comment out this block:
if (tokenCacheManager.ShouldClearCache(errorCode, "consent required"))
{
  // ...
}
```

## Benefits

1. **Better Developer Experience**: No manual database queries needed
2. **Automatic Recovery**: System self-heals from stale tokens
3. **User-Friendly**: Clear error messages and automatic remediation
4. **Diagnostic Friendly**: Comprehensive logging for troubleshooting
5. **Safe**: Rate-limited to prevent excessive operations

## When This Helps

- After deploying API changes that require new authorization
- After adding `.RequireAuthorization()` to new endpoints
- After changing authentication scopes
- After token cache corruption
- During development when switching between branches

## Previous Manual Process (No Longer Needed)

Before this feature, developers had to:
1. Open SQL Server Management Studio
2. Run `DELETE FROM dbo.SessionCache`
3. Clear browser cookies
4. Sign out and sign in

Now this happens automatically! ??
