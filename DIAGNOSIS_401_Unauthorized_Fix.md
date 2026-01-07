# 401 Unauthorized Error - Diagnosis and Fix

## Problem Summary

The `GetBackupSetsAsync()` call was failing with a **401 Unauthorized** error when calling the Budget.Api endpoint `/utilities/backup-sets`.

### Root Cause

The error was caused by:

```
IDW10502: An MsalUiRequiredException was thrown due to a challenge for the user
```

This means:
1. The user needs to **consent** to the API scope (`api://{ClientId}/access_as_user`)
2. `ITokenAcquisition` requires UI interaction (consent dialog) to acquire the token
3. In **Blazor Server** (SignalR context), there's no way to show a consent dialog
4. The handler was proceeding **without a token**, causing the 401 error

## Changes Made

### 1. Enhanced ForwardAuthCookiesHandler (Budget.Web\Services\ForwardAuthCookiesHandler.cs)

**Before**: Generic exception handling that logged errors but still sent requests without tokens.

**After**: 
- Added specific exception handling for `MicrosoftIdentityWebChallengeUserException` and `MsalUiRequiredException`
- Now returns **401 Unauthorized** with descriptive error messages instead of proceeding without a token
- Clearer logging to identify consent issues

```csharp
catch (MicrosoftIdentityWebChallengeUserException ex)
{
  // Don't proceed without a token - return 401 with clear error
  var response = new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)
  {
    ReasonPhrase = "User consent required - please sign out and sign back in"
  };
  return response;
}
```

### 2. Proactive Token Acquisition (Budget.Web\Startup\ConfigureIdentity.cs)

Added an `OnTokenValidated` event handler to **force token acquisition during sign-in**:

```csharp
options.Events.OnTokenValidated = async context =>
{
  // Proactively acquire and cache the API token during sign-in
  var token = await tokenAcquisition.GetAccessTokenForUserAsync(
    new[] { apiScope },
    authenticationScheme: OpenIdConnectDefaults.AuthenticationScheme);
  
  logger.LogInformation("? Successfully acquired and cached API token during sign-in");
};
```

This ensures that:
- The token is acquired while the user is still in an HTTP context (can show consent)
- The token is cached in SQL Server distributed cache for later use
- Subsequent API calls can use the cached token

### 3. Improved Error Handling (Budget.Client\Components\Maintenance\BackupRestore\BackupRestoreIndex.razor.cs)

Added specific handling for 401 errors with user-friendly messages:

```csharp
catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
{
  Snackbar.Add("Authentication required. Please sign out and sign back in to grant API access.", Severity.Warning);
}
```

### 4. Enhanced AuthDebug Page (Budget.Web\Components\Pages\Debug\AuthDebug.razor)

Added **token acquisition testing** to the debug page:
- Tests if the API token can be acquired
- Shows the API scope being used
- Displays clear status (SUCCESS/CONSENT REQUIRED/ERROR)
- Provides actionable guidance if token acquisition fails

## How to Test

### Step 1: Sign Out and Sign In Again

1. Navigate to the application
2. Sign out completely
3. Sign in again with your admin account
4. During sign-in, you should see a **consent screen** asking to grant API access
5. Accept the consent

**Look for this log message:**
```
? Successfully acquired and cached API token during sign-in (length: XXXX)
```

### Step 2: Check the AuthDebug Page

1. Navigate to `/debug/auth`
2. Look at the **Token Acquisition Test** section
3. It should show:
   - ? SUCCESS: Token acquired (length: XXXX)

### Step 3: Test the BackupRestore Page

1. Navigate to `/maintenance`
2. Click the **Backup/Restore** tab
3. The backup sets should now load without a 401 error

### Step 4: Check the Logs

Look for these log messages:

**During Sign-In:**
```
Token validated - attempting to acquire API token for scope: api://XXXXXXXX/access_as_user
? Successfully acquired and cached API token during sign-in (length: XXXX)
```

**During API Calls:**
```
Attempting to acquire token for scope: api://XXXXXXXX/access_as_user
? Added Bearer token for https://localhost:7063/utilities/backup-sets (token length: XXXX)
```

## Verification Checklist

- [ ] Distributed cache (SessionCache table) contains token cache entries
- [ ] Sign-in logs show successful token acquisition
- [ ] AuthDebug page shows "? SUCCESS" for token acquisition
- [ ] BackupRestore page loads backup sets without 401 errors
- [ ] ForwardAuthCookiesHandler logs show "? Added Bearer token"

## If It Still Fails

### Consent Not Granted

If you see:
```
? CONSENT REQUIRED: IDW10502...
```

**Action**: Sign out and sign in again. Make sure you see and accept the consent screen.

### API Scope Not Configured

If the AuthDebug page shows "Not configured" for API Scope:

**Action**: Verify `AzureAd:ClientId` is set in configuration/user secrets.

### Distributed Cache Not Working

If tokens aren't persisting:

**Action**: Check that the `SessionCache` table exists in SQL Server and that the connection string is correct.

### Admin Role Not Assigned

If the endpoint still returns 401 even with a token:

**Action**: Verify your user has the "Admin" role in Azure Portal:
1. Go to Azure Portal
2. Navigate to Enterprise Applications ? FantumBudget
3. Users and groups ? Add user
4. Assign the "Admin" role
5. Sign out and sign in again

## Architecture Notes

### Why This Happens in Blazor Server

- **Blazor Server** runs components on the server via SignalR
- SignalR connections are **long-lived WebSocket connections**
- There's no traditional HTTP request/response for component interactions
- Token acquisition that requires consent needs an HTTP context
- Solution: Acquire tokens **during sign-in** (when HTTP context exists) and cache them

### Token Caching Flow

1. User signs in via OIDC (HTTP context exists)
2. `OnTokenValidated` event fires
3. `ITokenAcquisition` acquires API token (can show consent if needed)
4. Token is cached in SQL Server distributed cache
5. Later API calls retrieve cached token
6. No consent needed for subsequent calls

## Related Files

- `Budget.Web\Services\ForwardAuthCookiesHandler.cs` - Token forwarding handler
- `Budget.Web\Startup\ConfigureIdentity.cs` - Authentication configuration
- `Budget.Client\Components\Maintenance\BackupRestore\BackupRestoreIndex.razor.cs` - Component that calls the API
- `Budget.Web\Components\Pages\Debug\AuthDebug.razor` - Diagnostic page
- `Budget.Api\Features\Utilities\ImportExport\GetBackupSets.cs` - API endpoint requiring authentication

## Summary

The fix ensures that API tokens are **acquired proactively during sign-in** (when user consent can be obtained) rather than lazily during API calls (when consent prompts aren't possible in Blazor Server). The enhanced error handling provides clear guidance when token acquisition fails.
