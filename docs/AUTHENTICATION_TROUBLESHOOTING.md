# Authentication Troubleshooting Guide

## Authentication Scheme Not Registered Error

### Symptom
When running in Azure Container Apps, you may encounter:
```
System.InvalidOperationException: No authentication handler is registered for the scheme 'EntraJwt'. 
The registered schemes are: LocalJwt. Did you forget to call AddAuthentication().Add[SomeAuthHandler]("EntraJwt",...)?
```

### Root Cause
The API supports dual authentication modes:
1. **EntraJwt** - Microsoft Entra ID (Azure AD) authentication
2. **LocalJwt** - Custom JWT authentication (for backward compatibility)

The Entra authentication scheme is only registered when `AzureAd:ClientId` is configured. If this configuration is missing (e.g., environment variables not set in Azure Container Apps), the scheme won't be registered.

### Solution
Authorization policies now use a **smart JWT policy scheme** that dynamically routes to the correct authentication handler based on the token's issuer, preventing authentication errors.

## IDX10205: Issuer Validation Failed Error

### Symptom
When running locally with both authentication schemes configured, you may see errors like:
```
IDX10205: Issuer validation failed. Issuer: 'https://sts.windows.net/{tenant-id}/'. 
Did not match: validationParameters.ValidIssuer: 'budget-api' or validationParameters.ValidIssuers: 'null'
```

### Root Cause
Previously, when both `EntraJwt` and `LocalJwt` schemes were configured, **both handlers attempted to validate every incoming JWT token**. This caused:
- Entra ID tokens with issuer `https://sts.windows.net/{tenant-id}/` to fail LocalJwt validation
- Noisy error logs (IDX10205) even though authentication ultimately succeeded

### Solution (Architectural Fix)
The authentication system now uses a **Policy Scheme** with intelligent forwarding:

1. **Smart JWT Selector** (`SmartJwt` scheme):
   - Inspects the incoming JWT token
   - Reads the `iss` (issuer) claim
   - Routes to `EntraJwt` if issuer contains `login.microsoftonline.com` or `sts.windows.net`
   - Routes to `LocalJwt` for all other tokens

2. **Benefits**:
   - ? Only ONE handler validates each token (no failed attempts)
   - ? No IDX10205 errors in logs
   - ? Cleaner architecture - separation of concerns
   - ? Better performance (no redundant validation)

### How It Works

```
Incoming Request with Bearer Token
         ?
  SmartJwt Policy Scheme
         ?
   Inspect issuer claim
         ?
    ???????????
    ?         ?
EntraJwt   LocalJwt
 Handler    Handler
    ?         ?
   Success  Success
```

Each token is only validated by its intended handler.

### Configuration Requirements

#### For Entra ID Authentication in Azure
Set these environment variables in Azure Container Apps:

```bash
AzureAd__Instance=https://login.microsoftonline.com/
AzureAd__Domain=yourtenant.onmicrosoft.com
AzureAd__TenantId=your-tenant-id
AzureAd__ClientId=your-app-registration-client-id
```

Note: Double underscore (`__`) is used for nested configuration in environment variables.

#### For Local JWT Only
If you only want to use custom JWT authentication (no Entra), simply leave the `AzureAd:ClientId` empty in your configuration.

### Verification
Check the logs at startup to see the authentication configuration:

```
Using smart JWT authentication with dynamic scheme selection
Microsoft Entra ID JWT Bearer authentication configured with scheme: EntraJwt
Smart JWT selector configured - will route to EntraJwt or LocalJwt based on token issuer
Configuring authorization with dynamic scheme: SmartJwt
```

### Code Reference
The smart authentication logic is in `Budget.Api/Program.cs`:
- Lines 140-150: Policy scheme registration (`SmartJwt`)
- Lines 201-241: Policy scheme forwarding logic (inspects issuer)
- Lines 244-275: Authorization policies using the policy scheme
