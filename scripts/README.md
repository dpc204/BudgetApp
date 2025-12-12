# Authentication Scripts

This directory contains PowerShell scripts for managing Microsoft Entra ID (Azure AD) authentication for the FantumBudget application.

## Scripts Overview

### Setup-EntraApp.ps1

**Purpose**: Initial setup of the Entra ID app registration with all necessary configurations.

**Use this when**: Setting up authentication for the first time.

**What it does**:
- Creates or updates the "FantumBudget" app registration
- Configures redirect URIs for development (and optionally production)
- Sets up API permissions (Microsoft Graph)
- Creates app roles (Admin, PowerUser, User)
- Generates client secret
- Optionally saves secret to Azure Key Vault

**Basic Usage**:
```powershell
.\Setup-EntraApp.ps1
```

**Advanced Usage**:
```powershell
# Include production redirect URI during setup
.\Setup-EntraApp.ps1 -EnvironmentName "budget-prod"

# Save client secret to Key Vault
.\Setup-EntraApp.ps1 -SaveToKeyVault -KeyVaultName "budget-kv"

# Full setup with production environment and Key Vault
.\Setup-EntraApp.ps1 -EnvironmentName "budget-prod" -SaveToKeyVault -KeyVaultName "budget-kv"
```

**Parameters**:
- `-TenantId`: Azure AD Tenant ID (optional, uses default if not specified)
- `-EnvironmentName`: Azure Container Apps environment name for production redirect URI
- `-SaveToKeyVault`: Switch to save client secret to Key Vault
- `-KeyVaultName`: Name of the Key Vault (required if SaveToKeyVault is used)
- `-SkipBrowserAuth`: Use device code flow instead of browser (for automation)

### Add-RedirectUri.ps1

**Purpose**: Add a redirect URI to an existing app registration after deployment.

**Use this when**: 
- You've deployed to Azure Container Apps and need to add the production URL
- You get error `AADSTS50011: The redirect URI does not match`
- You need to add additional redirect URIs for new environments

**What it does**:
- Connects to Microsoft Graph
- Finds the FantumBudget app registration
- Adds the specified redirect URI
- Automatically adds the corresponding signout callback URI

**Basic Usage**:
```powershell
.\Add-RedirectUri.ps1 -RedirectUri "https://budget.delightfulbush-2a4d6a17.eastus.azurecontainerapps.io/signin-oidc"
```

**Advanced Usage**:
```powershell
# Add redirect URI to a custom named app
.\Add-RedirectUri.ps1 -RedirectUri "https://myapp.azurecontainerapps.io/signin-oidc" -AppName "MyCustomApp"

# Specify tenant ID
.\Add-RedirectUri.ps1 -RedirectUri "https://myapp.azurecontainerapps.io/signin-oidc" -TenantId "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
```

**Parameters**:
- `-RedirectUri` (required): The full redirect URI to add
- `-AppName`: Name of the app registration (defaults to "FantumBudget")
- `-TenantId`: Azure AD Tenant ID (optional)
- `-SkipBrowserAuth`: Use device code flow instead of browser

## Common Workflows

### First-Time Setup

1. **Create the app registration**:
   ```powershell
   .\Setup-EntraApp.ps1
   ```

2. **Save the output** (ClientId, TenantId, ClientSecret) to your `appsettings.Development.json`

3. **Test locally** to ensure authentication works

### Deploying to Azure

1. **Deploy your application** to Azure Container Apps:
   ```bash
   azd up
   ```

2. **Get your Container Apps URL** from the deployment output or Azure Portal

3. **Add the redirect URI**:
   ```powershell
   .\Add-RedirectUri.ps1 -RedirectUri "https://YOUR-APP-URL.azurecontainerapps.io/signin-oidc"
   ```

4. **Test authentication** in your deployed application

### Updating or Redeploying

If you redeploy and your Container Apps URL changes:

1. **Note the new URL** from Azure Portal
2. **Run Add-RedirectUri.ps1** with the new URL
3. The old URL remains in the app registration (safe to remove if no longer needed)

### Adding Multiple Environments

For staging, production, and other environments:

```powershell
# Add staging
.\Add-RedirectUri.ps1 -RedirectUri "https://budget-staging.azurecontainerapps.io/signin-oidc"

# Add production
.\Add-RedirectUri.ps1 -RedirectUri "https://budget-production.azurecontainerapps.io/signin-oidc"

# Add custom domain
.\Add-RedirectUri.ps1 -RedirectUri "https://budget.yourdomain.com/signin-oidc"
```

## Prerequisites

### Required Software

- **PowerShell 7.0+**
  - Install: https://aka.ms/powershell
  - Verify: `$PSVersionTable.PSVersion`

### Required Permissions

You need one of these Azure AD roles:
- Global Administrator (recommended for setup)
- Application Administrator
- Cloud Application Administrator

**AND** one of these Microsoft Graph API permissions:
- Application.Read.All (minimum - for reading app registrations)
- Application.ReadWrite.All (recommended - for modifying app registrations)

To check your role:
1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Active Directory** → **Roles and administrators**
3. Search for your user account

**Note**: If you get an "Insufficient privileges" or "Authorization_RequestDenied" error, you need to:
1. Request one of the roles above from your Azure AD administrator, OR
2. Ask an administrator to run the script for you

### PowerShell Modules

The scripts will automatically install required modules if missing:
- `Microsoft.Graph.Applications`

Or install manually:
```powershell
Install-Module Microsoft.Graph.Applications -Scope CurrentUser
```

## Troubleshooting

### "App registration not found"

**Problem**: Script can't find the FantumBudget app registration

**Solution**: 
- Verify the app registration exists in Azure Portal
- Check you're connected to the correct tenant
- Use `-AppName` parameter if your app has a different name

### "Insufficient privileges" or "Authorization_RequestDenied"

**Problem**: Your account lacks permission to read or modify app registrations

**Error Messages**:
- "Insufficient privileges to complete the operation"
- "Status: 403 (Forbidden)"
- "ErrorCode: Authorization_RequestDenied"

**Solution**:
1. **Request the required Azure AD role** from your administrator:
   - Global Administrator, Application Administrator, or Cloud Application Administrator
2. **Request Microsoft Graph API permissions**:
   - Application.Read.All (minimum for reading)
   - Application.ReadWrite.All (required for modifications)
3. **Alternative**: Have an administrator run the script for you
4. **For automation**: Use a service principal with Application.ReadWrite.All permission

**To verify your permissions**:
1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Active Directory** → **Roles and administrators**
3. Search for your account and check assigned roles

### "Redirect URI format is invalid"

**Problem**: The redirect URI doesn't match expected format

**Solution**:
- Ensure URI starts with `https://` (http only allowed for localhost)
- Verify URI ends with `/signin-oidc`
- Remove any trailing spaces or extra characters

### "Client secret expired"

**Problem**: Authentication fails due to expired client secret

**Solution**:
1. Run Setup-EntraApp.ps1 to generate a new secret
2. Update the secret in your application configuration
3. For production, update Azure Key Vault
4. Redeploy if necessary

## Security Best Practices

1. **Never commit secrets**: Keep client secrets out of source control
2. **Use Key Vault**: Store secrets in Azure Key Vault for production
3. **Rotate secrets**: Regenerate secrets before they expire (default: 2 years)
4. **Limit permissions**: Only grant the minimum required API permissions
5. **Monitor usage**: Enable audit logging for app registration changes

## Reference Links

- [Microsoft Entra ID Documentation](https://learn.microsoft.com/entra/identity/)
- [App Registration Documentation](https://learn.microsoft.com/entra/identity-platform/quickstart-register-app)
- [Redirect URI Guidelines](https://learn.microsoft.com/entra/identity-platform/reply-url)
- [Microsoft Graph PowerShell](https://learn.microsoft.com/powershell/microsoftgraph/)

## Getting Help

If you encounter issues:

1. Review the [Troubleshooting Guide](../Documentation/Troubleshooting-Azure-Authentication.md)
2. Check Azure AD sign-in logs in Azure Portal
3. Enable verbose logging: Add `-Verbose` to script parameters
4. Review the [Phase 1 Setup Guide](../Documentation/Phase1-EntraID-Setup.md)
