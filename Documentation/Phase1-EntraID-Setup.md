# Phase 1: Microsoft Entra ID Setup for FantumBudget

This guide provides comprehensive instructions for configuring Microsoft Entra ID (formerly Azure AD) authentication for the FantumBudget application.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Automated Setup (Recommended)](#automated-setup-recommended)
- [Manual Setup](#manual-setup)
- [User Role Assignment](#user-role-assignment)
- [Verification Steps](#verification-steps)
- [Troubleshooting](#troubleshooting)
- [Security Best Practices](#security-best-practices)
- [Environment Configuration](#environment-configuration)

## Prerequisites

Before starting, ensure you have:

### Required Software
- **Azure CLI** (version 2.50.0 or higher)
  - Install from: https://aka.ms/installazurecli
  - Verify: `az version`
- **PowerShell 7.0+** (for automated setup)
  - Install from: https://aka.ms/powershell
  - Verify: `$PSVersionTable.PSVersion`
- **Microsoft Graph PowerShell SDK**
  - The script will auto-install if missing
  - Manual install: `Install-Module Microsoft.Graph.Applications -Scope CurrentUser`

### Required Permissions
You need one of the following Azure AD roles:
- **Global Administrator** (recommended for setup)
- **Application Administrator**
- **Cloud Application Administrator**

To check your role:
1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Active Directory** → **Roles and administrators**
3. Search for your user account

### Access Requirements
- Access to the Azure subscription where the app will be deployed
- Permissions to create and configure app registrations
- (Optional) Access to Azure Key Vault if storing secrets there

## Automated Setup (Recommended)

The automated PowerShell script simplifies the entire setup process.

### Basic Usage

Run from the `scripts` directory:

```powershell
.\Setup-EntraApp.ps1
```

This will:
- ✅ Create the "FantumBudget" app registration
- ✅ Configure redirect URIs for development (https://localhost:7141)
- ✅ Enable ID and Access tokens
- ✅ Set up Microsoft Graph API permissions
- ✅ Create app roles (Admin, PowerUser, User)
- ✅ Generate a client secret
- ✅ Output all configuration values

### Advanced Usage

#### Include Production Environment

```powershell
.\Setup-EntraApp.ps1 -EnvironmentName "fantumbudget-prod"
```

This adds the production redirect URI: `https://fantumbudget-prod.azurecontainerapps.io/signin-oidc`

#### Save Secret to Key Vault

```powershell
.\Setup-EntraApp.ps1 -SaveToKeyVault -KeyVaultName "fantumbudget-kv"
```

Automatically stores the client secret in Azure Key Vault.

#### Update Existing App

If an app with the name "FantumBudget" already exists, the script will prompt you to update it.

#### Non-Interactive Mode

For automated pipelines:

```powershell
.\Setup-EntraApp.ps1 -SkipBrowserAuth -TenantId "your-tenant-id"
```

### Script Output

After successful execution, the script outputs:

1. **Application Details**
   - App ID (Client ID)
   - Tenant ID
   - Object ID

2. **Client Secret**
   - Secret value (store securely!)
   - Expiration date

3. **Configuration Template**
   - Ready-to-use JSON for appsettings.json

4. **Next Steps**
   - Links to Azure Portal pages
   - Role assignment instructions

## Manual Setup

If you prefer or need to configure manually:

### Step 1: Create App Registration

1. Navigate to [Azure Portal](https://portal.azure.com)
2. Go to **Azure Active Directory** → **App registrations**
3. Click **New registration**
4. Enter the following details:
   - **Name**: `FantumBudget`
   - **Supported account types**: Accounts in this organizational directory only (Single tenant)
   - **Redirect URI**: 
     - Platform: `Web`
     - URI: `https://localhost:7141/signin-oidc`
5. Click **Register**

### Step 2: Configure Authentication

1. In your app registration, navigate to **Authentication**
2. Under **Web** redirect URIs, add:
   - `https://localhost:7141/signin-oidc` (if not already added)
3. Under **Single-page application**, add:
   - `https://localhost:7141/authentication/login-callback`
4. Under **Implicit grant and hybrid flows**, enable:
   - ✅ ID tokens
   - ✅ Access tokens
5. Click **Save**

### Step 3: Configure API Permissions

1. Navigate to **API permissions**
2. Click **Add a permission**
3. Select **Microsoft Graph** → **Delegated permissions**
4. Add the following permissions:
   - `User.Read`
   - `email`
   - `openid`
   - `profile`
5. Click **Add permissions**
6. Click **Grant admin consent for [Your Organization]**
7. Confirm by clicking **Yes**

### Step 4: Create Client Secret

1. Navigate to **Certificates & secrets**
2. Click **New client secret**
3. Enter:
   - **Description**: `FantumBudget Secret`
   - **Expires**: 24 months (recommended)
4. Click **Add**
5. **IMPORTANT**: Copy the secret value immediately and store it securely

### Step 5: Define App Roles

1. Navigate to **App roles**
2. Click **Create app role**

**Admin Role:**
- Display name: `Admin`
- Allowed member types: `Users/Groups`
- Value: `Admin`
- Description: `Administrator role with full access to all features`
- Enable this app role: ✅

**PowerUser Role:**
- Display name: `PowerUser`
- Allowed member types: `Users/Groups`
- Value: `PowerUser`
- Description: `Power User role with elevated access to advanced features`
- Enable this app role: ✅

**User Role:**
- Display name: `User`
- Allowed member types: `Users/Groups`
- Value: `User`
- Description: `Standard user role with basic access`
- Enable this app role: ✅

3. Click **Apply** for each role

### Step 6: Note Configuration Values

From the **Overview** page, record:
- **Application (client) ID**
- **Directory (tenant) ID**
- **Client secret** (from Step 4)

## User Role Assignment

After creating the app registration, assign users to roles:

### Through Azure Portal

1. Navigate to [Azure Portal](https://portal.azure.com)
2. Go to **Azure Active Directory** → **Enterprise applications**
3. Find and select **FantumBudget**
4. Navigate to **Users and groups**
5. Click **Add user/group**
6. Select:
   - **Users**: Choose the user(s)
   - **Select a role**: Choose Admin, PowerUser, or User
7. Click **Assign**

### Verify Role Assignments

1. In **Enterprise applications** → **FantumBudget** → **Users and groups**
2. You should see each user with their assigned role

### Role Descriptions

| Role | Access Level | Typical Users |
|------|-------------|---------------|
| **Admin** | Full access to all features, user management, and settings | System administrators, IT managers |
| **PowerUser** | Elevated access to advanced features, reports, and analytics | Department managers, senior users |
| **User** | Standard access to core budgeting features | Regular users, team members |

## Verification Steps

### 1. Verify App Registration

```powershell
# Using Azure CLI
az ad app list --display-name "FantumBudget" --query "[].{Name:displayName, AppId:appId, Id:id}" -o table
```

Expected output:
```
Name           AppId                                 Id
-------------  ------------------------------------  ------------------------------------
FantumBudget   xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx  xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
```

### 2. Verify Redirect URIs

```powershell
az ad app show --id <app-id> --query "web.redirectUris" -o json
```

Should include:
```json
[
  "https://localhost:7141/signin-oidc"
]
```

### 3. Verify API Permissions

1. Go to Azure Portal → App registrations → FantumBudget → API permissions
2. Confirm the following are listed and have admin consent:
   - Microsoft Graph: User.Read
   - Microsoft Graph: email
   - Microsoft Graph: openid
   - Microsoft Graph: profile

### 4. Verify App Roles

```powershell
az ad app show --id <app-id> --query "appRoles[].{DisplayName:displayName, Value:value}" -o table
```

Expected output:
```
DisplayName  Value
-----------  ---------
Admin        Admin
PowerUser    PowerUser
User         User
```

### 5. Test Authentication (After Configuration)

After configuring your application with the Entra settings:

1. Run the application locally
2. Navigate to the login page
3. Click "Sign in with Microsoft"
4. Enter credentials for a user assigned to a role
5. Verify successful login and proper role assignment

## Troubleshooting

For comprehensive troubleshooting guidance, especially for Azure deployment issues, see:
- **[Troubleshooting Azure Authentication](Troubleshooting-Azure-Authentication.md)** - Complete guide for fixing authentication errors

### Common Issues

#### Issue: "AADSTS50011: The reply URL specified in the request does not match"

**Solution:**
- **For local development**: Verify the redirect URI in your appsettings.json matches exactly with the URI configured in Azure Portal (https://localhost:7141/signin-oidc)
- **For Azure deployment**: After deploying to Azure Container Apps, run:
  ```powershell
  cd scripts
  .\Add-RedirectUri.ps1 -RedirectUri "https://YOUR-APP-URL.azurecontainerapps.io/signin-oidc"
  ```
- Check for http vs https
- Ensure the port number is correct (7141 for development)
- Clear browser cache and cookies
- See the [full troubleshooting guide](Troubleshooting-Azure-Authentication.md#aadsts50011-redirect-uri-mismatch) for more details

#### Issue: "AADSTS65001: The user or administrator has not consented"

**Solution:**
- Ensure admin consent was granted for API permissions
- Go to Azure Portal → App registrations → FantumBudget → API permissions
- Click "Grant admin consent for [Organization]"

#### Issue: Client secret expired

**Solution:**
- Create a new client secret in Azure Portal
- Update appsettings.json or Key Vault with the new secret
- Delete the old secret after verifying the new one works

#### Issue: User doesn't have expected role

**Solution:**
- Verify role assignment in Azure Portal → Enterprise applications → FantumBudget → Users and groups
- Roles are not automatically granted; they must be explicitly assigned
- User may need to sign out and sign back in for role changes to take effect

#### Issue: PowerShell script fails with authentication error

**Solution:**
- Ensure you have sufficient permissions (Global Admin, Application Admin)
- Try running `az login` manually first
- Check that Microsoft Graph PowerShell module is installed
- Verify you're connected to the correct tenant

### Checking Logs

#### Azure AD Sign-in Logs

1. Azure Portal → Azure Active Directory → Sign-in logs
2. Filter by Application: FantumBudget
3. Review failed sign-ins for detailed error messages

#### Application Logs

Check your application logs for authentication errors:
- Look for AADSTS error codes
- Check redirect URI mismatches
- Verify token validation errors

### Getting Help

If issues persist:

1. **Azure AD Troubleshooting Tool**: https://aka.ms/aadtroubleshoot
2. **Microsoft Support**: https://azure.microsoft.com/support/
3. **Documentation**: https://learn.microsoft.com/entra/identity-platform/

## Security Best Practices

### Client Secret Management

❌ **DON'T:**
- Store client secrets in source code
- Commit secrets to version control
- Share secrets via email or chat
- Use the same secret across environments

✅ **DO:**
- Store secrets in Azure Key Vault
- Use managed identities when possible
- Rotate secrets regularly (before expiration)
- Use separate secrets for dev/staging/prod
- Enable secret expiration notifications

### Key Vault Integration

```powershell
# Store secret in Key Vault
az keyvault secret set `
  --vault-name "fantumbudget-kv" `
  --name "EntraClientSecret" `
  --value "your-client-secret"

# Grant app access to Key Vault
az keyvault set-policy `
  --name "fantumbudget-kv" `
  --object-id <app-object-id> `
  --secret-permissions get list
```

### Access Configuration via Key Vault

In appsettings.json:
```json
{
  "AzureAd": {
    "ClientSecret": "" // Leave empty, will be loaded from Key Vault
  },
  "KeyVault": {
    "VaultUri": "https://fantumbudget-kv.vault.azure.net/"
  }
}
```

### Conditional Access Policies

Consider implementing:
- Multi-factor authentication (MFA) requirements
- Trusted location restrictions
- Device compliance requirements
- Sign-in risk policies

### Monitoring and Auditing

Enable monitoring for:
- Failed sign-in attempts
- Unusual access patterns
- Role assignment changes
- Permission changes

Set up alerts for:
- Multiple failed logins
- Sign-ins from unexpected locations
- Administrative changes

## Environment Configuration

### Development Environment

Use `appsettings.Development.json`:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "your-domain.onmicrosoft.com",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientSecret": "your-dev-secret",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  }
}
```

### Production Environment (Azure Container Apps)

#### Update Redirect URIs

When deploying to production:

1. Get your Container Apps URL:
   ```bash
   az containerapp show --name fantumbudget-web --resource-group rg-fantumbudget --query properties.configuration.ingress.fqdn -o tsv
   ```

2. Add production redirect URI:
   ```powershell
   # Run the setup script with environment name
   .\Setup-EntraApp.ps1 -EnvironmentName "fantumbudget-prod"
   ```

   Or manually add in Azure Portal:
   - Navigate to App registrations → FantumBudget → Authentication
   - Add redirect URI: `https://[your-app-url].azurecontainerapps.io/signin-oidc`

#### Use Azure Key Vault for Secrets

In production, reference secrets from Key Vault:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "your-domain.onmicrosoft.com",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id"
    // ClientSecret loaded from Key Vault
  }
}
```

Configure in Container Apps:
```bash
az containerapp secret set \
  --name fantumbudget-web \
  --resource-group rg-fantumbudget \
  --secrets entra-secret=keyvaultref:https://fantumbudget-kv.vault.azure.net/secrets/EntraClientSecret,identityref:/subscriptions/{sub-id}/resourcegroups/{rg}/providers/Microsoft.ManagedIdentity/userAssignedIdentities/{identity}
```

### Multi-Environment Strategy

| Environment | Secret Storage | Redirect URI |
|-------------|----------------|--------------|
| Development | appsettings.Development.json (local) | https://localhost:7141/signin-oidc |
| Staging | Azure Key Vault | https://staging.azurecontainerapps.io/signin-oidc |
| Production | Azure Key Vault | https://prod.azurecontainerapps.io/signin-oidc |

## Next Steps

After completing this phase:

1. ✅ Update `appsettings.json` files with Entra configuration
2. ✅ Implement authentication middleware in Budget.Web
3. ✅ Configure authorization policies
4. ✅ Test authentication flow locally
5. ✅ Proceed to Phase 2: Code Migration (see [Authentication-Migration-Plan.md](Authentication-Migration-Plan.md))

## Resources

- [Microsoft Entra ID Documentation](https://learn.microsoft.com/entra/identity-platform/)
- [ASP.NET Core Authentication](https://learn.microsoft.com/aspnet/core/security/authentication/)
- [Azure Key Vault](https://learn.microsoft.com/azure/key-vault/)
- [App Roles](https://learn.microsoft.com/entra/identity-platform/howto-add-app-roles-in-apps)
- [PowerShell Script Documentation](../scripts/Setup-EntraApp.ps1)
