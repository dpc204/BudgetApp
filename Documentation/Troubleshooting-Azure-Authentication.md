# Troubleshooting Azure Authentication Issues

This guide helps you resolve common authentication issues when deploying the FantumBudget application to Azure Container Apps.

## Table of Contents

- [AADSTS50011: Redirect URI Mismatch](#aadsts50011-redirect-uri-mismatch)
- [Other Common Authentication Errors](#other-common-authentication-errors)
- [Prevention: Setting Up for Deployment](#prevention-setting-up-for-deployment)

## AADSTS50011: Redirect URI Mismatch

### Error Message

```
AADSTS50011: The redirect URI 'https://your-app.azurecontainerapps.io/signin-oidc' 
specified in the request does not match the redirect URIs configured for the 
application 'xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx'. Make sure the redirect URI 
sent in the request matches one added to your application in the Azure portal.
```

### Root Cause

When you deploy to Azure Container Apps, your application gets a unique URL (e.g., `https://budget.delightfulbush-2a4d6a17.eastus.azurecontainerapps.io`). Azure AD requires this exact URL to be registered as an allowed redirect URI in your Entra ID app registration **before** users can sign in.

During initial setup, only the localhost development URL is typically registered. After deployment, you must add the production URL.

### Solution: Quick Fix (Recommended)

Use the provided PowerShell script to add your deployed URL to the app registration:

1. **Get your deployed application URL** from Azure Portal or deployment output:
   - Go to [Azure Portal](https://portal.azure.com)
   - Navigate to **Container Apps** → Select your app → **Overview**
   - Copy the **Application Url** (e.g., `https://budget.delightfulbush-2a4d6a17.eastus.azurecontainerapps.io`)

2. **Run the Add-RedirectUri script** from the `scripts` directory:

   ```powershell
   cd scripts
   .\Add-RedirectUri.ps1 -RedirectUri "https://YOUR-APP-URL.azurecontainerapps.io/signin-oidc"
   ```

   Replace `YOUR-APP-URL` with your actual Container Apps URL.

3. **Example**:

   ```powershell
   .\Add-RedirectUri.ps1 -RedirectUri "https://budget.delightfulbush-2a4d6a17.eastus.azurecontainerapps.io/signin-oidc"
   ```

4. **Wait and test**:
   - Allow 1-2 minutes for changes to propagate
   - Try signing in to your application again

### Solution: Manual Fix (Alternative)

If you prefer to add the redirect URI manually through the Azure Portal:

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Active Directory** → **App registrations**
3. Find and select your app registration (typically named "FantumBudget")
4. In the left menu, select **Authentication**
5. Under **Platform configurations** → **Web**, click **Add URI**
6. Add your redirect URI: `https://YOUR-APP-URL.azurecontainerapps.io/signin-oidc`
7. Also add the signout URI: `https://YOUR-APP-URL.azurecontainerapps.io/signout-callback-oidc`
8. Click **Save** at the bottom of the page
9. Wait 1-2 minutes for changes to propagate
10. Try signing in again

### Verification

After adding the redirect URI, verify it's configured correctly:

1. In Azure Portal → **App registrations** → Your app → **Authentication**
2. Under **Web** → **Redirect URIs**, you should see:
   - `https://localhost:7141/signin-oidc` (development)
   - `https://YOUR-APP-URL.azurecontainerapps.io/signin-oidc` (production)
   - `https://YOUR-APP-URL.azurecontainerapps.io/signout-callback-oidc` (signout)

### Understanding the CallbackPath

The `/signin-oidc` path comes from the `CallbackPath` setting in your `appsettings.json`:

```json
{
  "AzureAd": {
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  }
}
```

The complete redirect URI is: `https://YOUR-DOMAIN` + `/signin-oidc`

## Other Common Authentication Errors

### AADSTS65001: Consent Required

**Error**: "The user or administrator has not consented to use the application"

**Solution**:
1. Go to Azure Portal → **App registrations** → Your app → **API permissions**
2. Click **Grant admin consent for [Your Organization]**
3. Confirm the consent

### AADSTS700016: Application Not Found

**Error**: "Application with identifier 'xxxxxxxx' was not found in the directory"

**Solution**:
1. Verify the `ClientId` in your `appsettings.json` matches the Application ID in Azure Portal
2. Ensure you're using the correct Azure AD tenant
3. Check that the app registration exists and hasn't been deleted

### AADSTS7000215: Invalid Client Secret

**Error**: "Invalid client secret is provided"

**Solution**:
1. The client secret may be incorrect or expired
2. Generate a new secret in Azure Portal:
   - **App registrations** → Your app → **Certificates & secrets**
   - Create a new client secret
3. Update the secret in your Azure Key Vault or app configuration
4. Redeploy if necessary

### User Has No Roles / Authorization Failed

**Error**: User can sign in but has no access to features

**Solution**:
1. Assign the user to a role in Azure Portal:
   - **Enterprise applications** → Search for your app name
   - Select your app → **Users and groups** → **Add user/group**
   - Select the user and assign them to a role (Admin, PowerUser, or User)
2. Have the user sign out and sign in again

## Prevention: Setting Up for Deployment

To avoid authentication issues when deploying to Azure, follow these steps:

### Option 1: Add Redirect URI During Deployment (Recommended)

After your first deployment:

1. Note your Container Apps URL from the deployment output or Azure Portal
2. Run the `Add-RedirectUri.ps1` script immediately:
   ```powershell
   .\scripts\Add-RedirectUri.ps1 -RedirectUri "https://YOUR-APP-URL.azurecontainerapps.io/signin-oidc"
   ```

### Option 2: Pre-configure Redirect URIs

If you know your Container Apps environment name before deployment:

1. Use the Setup-EntraApp.ps1 script with the environment name:
   ```powershell
   .\scripts\Setup-EntraApp.ps1 -EnvironmentName "your-environment-name"
   ```
   This adds `https://your-environment-name.azurecontainerapps.io/signin-oidc` during setup.

### Option 3: Use Custom Domains

For production deployments, consider using custom domains:

1. Configure a custom domain for your Container App
2. Add the custom domain redirect URI to your app registration:
   ```
   https://budget.yourdomain.com/signin-oidc
   ```
3. This remains stable across redeployments

## Best Practices

1. **Document Your URLs**: Keep a record of your redirect URIs for each environment (dev, staging, production)

2. **Use Separate App Registrations**: Consider separate app registrations for different environments to avoid confusion

3. **Automate Where Possible**: Use the provided scripts to automate redirect URI management

4. **Monitor Authentication Logs**: Enable diagnostic logging in Azure AD to track authentication issues

5. **Test After Deployment**: Always test the authentication flow immediately after deploying to a new environment

6. **Keep Secrets Secure**: Never commit client secrets to source control; always use Azure Key Vault for production

## Getting Help

If you continue to experience authentication issues:

1. Check the application logs in Azure Portal → Container Apps → Log stream
2. Review the authentication logs in Azure AD → Sign-ins
3. Verify your appsettings.json configuration matches your Azure AD app registration
4. Ensure all required environment variables are set in Container Apps configuration

## Related Documentation

- [Phase 1: Entra ID Setup](Phase1-EntraID-Setup.md)
- [Entra Configuration Template](entra-config-template.json)
- [Microsoft Entra ID Redirect URI Documentation](https://learn.microsoft.com/entra/identity-platform/reply-url)
