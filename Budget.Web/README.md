# Budget.Web - Microsoft Entra ID Authentication Setup

This guide explains how to configure Microsoft Entra ID (formerly Azure AD) authentication for the Budget.Web application.

## Prerequisites

Before configuring the application, you must complete **Phase 1: Entra ID Setup**. See [Documentation/Phase1-EntraID-Setup.md](../Documentation/Phase1-EntraID-Setup.md) for instructions on:
- Creating the "FantumBudget" app registration in Azure
- Configuring redirect URIs
- Setting up app roles (Admin, PowerUser, User)
- Generating client secrets
- Assigning users to roles

## Configuration

### Development Environment

For local development, use **User Secrets** to store sensitive configuration values. This keeps credentials out of source control.

#### Setting Up User Secrets

Run the following commands from the `Budget.Web` directory:

```bash
# Set the Azure AD Tenant ID
dotnet user-secrets set "AzureAd:TenantId" "your-tenant-id"

# Set the Azure AD Client ID (Application ID)
dotnet user-secrets set "AzureAd:ClientId" "your-client-id"

# Set the Azure AD Client Secret
dotnet user-secrets set "AzureAd:ClientSecret" "your-client-secret"

# Set your Azure AD Domain
dotnet user-secrets set "AzureAd:Domain" "yourtenant.onmicrosoft.com"
```

#### Finding Your Configuration Values

1. **Tenant ID**: 
   - Azure Portal → Azure Active Directory → Overview → Tenant ID
   - Or run: `az account show --query tenantId -o tsv`

2. **Client ID** (Application ID):
   - Azure Portal → App registrations → FantumBudget → Overview → Application (client) ID

3. **Client Secret**:
   - Generated during Phase 1 setup (from the PowerShell script output)
   - Or create a new one: Azure Portal → App registrations → FantumBudget → Certificates & secrets

4. **Domain**:
   - Your Azure AD tenant domain (e.g., `contoso.onmicrosoft.com`)
   - Azure Portal → Azure Active Directory → Overview → Primary domain

### Production Environment

For production deployments (Azure Container Apps, App Service, etc.), store secrets in **Azure Key Vault**.

#### Azure Key Vault Setup

1. **Create or identify your Key Vault**:
   ```bash
   az keyvault create --name fantumbudget-kv --resource-group rg-fantumbudget --location eastus
   ```

2. **Store the client secret**:
   ```bash
   az keyvault secret set --vault-name fantumbudget-kv --name "EntraClientSecret" --value "your-client-secret"
   ```

3. **Grant access to your application's managed identity**:
   ```bash
   # For Container Apps
   az keyvault set-policy --name fantumbudget-kv \
     --object-id $(az containerapp identity show --name fantumbudget-web --resource-group rg-fantumbudget --query principalId -o tsv) \
     --secret-permissions get list
   ```

4. **Update your production configuration** to reference Key Vault:
   - The application automatically loads secrets from Key Vault when configured
   - Non-secret values can remain in `appsettings.json`

### Configuration File Structure

**appsettings.json** (checked into source control):
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "",
    "TenantId": "",
    "ClientId": "",
    "ClientSecret": "",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  }
}
```

**appsettings.Development.json** (checked into source control with placeholders):
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "yourtenant.onmicrosoft.com",
    "TenantId": "your-tenant-id-here",
    "ClientId": "your-client-id-here",
    "ClientSecret": "your-client-secret-here",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  }
}
```

**User Secrets** (local development - NOT in source control):
- Actual values are stored securely on your development machine
- Override placeholder values in appsettings.Development.json

## Authentication Flow

1. **Login**:
   - User clicks "Sign in with Microsoft"
   - Redirected to Microsoft Entra ID login page
   - After successful authentication, redirected back to the application
   - User's roles are loaded from Entra ID claims

2. **Authorization**:
   - Three role-based policies are configured:
     - `AdminOnly`: Requires Admin role
     - `PowerUserOrAbove`: Requires PowerUser or Admin role
     - `AuthenticatedUser`: Requires User, PowerUser, or Admin role

3. **Logout**:
   - User clicks "Logout"
   - Signs out from both the application and Entra ID
   - Redirected to home page

## User Role Assignment

Users must be assigned to roles in Azure before they can access the application.

### Assigning Roles

1. Go to Azure Portal → Azure Active Directory → Enterprise applications
2. Find and select "FantumBudget"
3. Navigate to Users and groups
4. Click "Add user/group"
5. Select the user and assign one of the roles:
   - **Admin**: Full access to all features
   - **PowerUser**: Elevated access to advanced features
   - **User**: Standard access to core features

See [Phase 1 documentation](../Documentation/Phase1-EntraID-Setup.md#user-role-assignment) for detailed instructions.

## Troubleshooting

### "Reply URL mismatch" error

**Problem**: `AADSTS50011: The reply URL specified in the request does not match`

**Solution**:
1. Verify redirect URI in Azure Portal matches your application URL:
   - Development: `https://localhost:7141/signin-oidc`
   - Production: `https://your-app.azurecontainerapps.io/signin-oidc`
2. Ensure the URL is configured in: App registrations → FantumBudget → Authentication → Redirect URIs

### "Admin consent required" error

**Problem**: `AADSTS65001: The user or administrator has not consented`

**Solution**:
1. Go to Azure Portal → App registrations → FantumBudget → API permissions
2. Click "Grant admin consent for [Your Organization]"

### User doesn't have expected permissions

**Problem**: User can log in but can't access certain features

**Solution**:
1. Verify role assignment in Azure Portal → Enterprise applications → FantumBudget → Users and groups
2. Ensure user is assigned to appropriate role (Admin, PowerUser, or User)
3. User may need to sign out and sign back in for role changes to take effect

### Client secret expired

**Problem**: Authentication fails with token errors

**Solution**:
1. Create a new client secret in Azure Portal
2. Update the secret in User Secrets (development) or Key Vault (production)
3. Test authentication
4. Delete the old secret after confirming the new one works

## Security Best Practices

✅ **DO**:
- Store secrets in User Secrets (development) or Azure Key Vault (production)
- Use HTTPS for all environments
- Rotate client secrets before they expire
- Enable Conditional Access policies in production
- Monitor sign-in logs in Azure AD

❌ **DON'T**:
- Commit secrets to source control
- Share client secrets via email or chat
- Use the same secret across environments
- Disable HTTPS requirements

## Additional Resources

- [Phase 1 Setup Documentation](../Documentation/Phase1-EntraID-Setup.md)
- [Authentication Migration Plan](../Documentation/Authentication-Migration-Plan.md)
- [Microsoft Identity Web Documentation](https://learn.microsoft.com/entra/msal/dotnet/microsoft-identity-web/)
- [Azure Key Vault Documentation](https://learn.microsoft.com/azure/key-vault/)

## Support

For issues or questions:
- Check [Phase 1 troubleshooting](../Documentation/Phase1-EntraID-Setup.md#troubleshooting)
- Review Azure AD sign-in logs in Azure Portal
- Contact your Azure administrator for access or permission issues
