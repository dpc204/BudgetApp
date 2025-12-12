# QUICK FIX: Azure Redirect URI Error

## Problem
You're seeing this error when trying to sign in:
```
AADSTS50011: The redirect URI 'https://budget.delightfulbush-2a4d6a17.eastus.azurecontainerapps.io/signin-oidc' 
specified in the request does not match the redirect URIs configured for the application
```

## Solution (Choose One)

### Option 1: Automated Fix with PowerShell Script (Recommended)

1. **Open PowerShell** and navigate to the scripts directory:
   ```powershell
   cd scripts
   ```

2. **Run the Add-RedirectUri script**:
   ```powershell
   .\Add-RedirectUri.ps1 -RedirectUri "https://budget.delightfulbush-2a4d6a17.eastus.azurecontainerapps.io/signin-oidc"
   ```

3. **Sign in** when prompted with an account that has Application Administrator or Global Administrator role

4. **Wait 1-2 minutes** for changes to propagate

5. **Test** by trying to sign in again at your application URL

### Option 2: Manual Fix via Azure Portal

1. Go to [Azure Portal](https://portal.azure.com)

2. Navigate to **Azure Active Directory** → **App registrations**

3. Find and click on **FantumBudget** app registration

4. Click **Authentication** in the left menu

5. Under **Web** → **Redirect URIs**, click **Add URI**

6. Add these two URIs:
   - `https://budget.delightfulbush-2a4d6a17.eastus.azurecontainerapps.io/signin-oidc`
   - `https://budget.delightfulbush-2a4d6a17.eastus.azurecontainerapps.io/signout-callback-oidc`

7. Click **Save** at the bottom

8. Wait 1-2 minutes for changes to propagate

9. Try signing in again

## Verification

After adding the redirect URIs, your app registration should show:

**Redirect URIs (Web):**
- ✅ `https://localhost:7141/signin-oidc` (development)
- ✅ `https://budget.delightfulbush-2a4d6a17.eastus.azurecontainerapps.io/signin-oidc` (production)
- ✅ `https://budget.delightfulbush-2a4d6a17.eastus.azurecontainerapps.io/signout-callback-oidc` (signout)

## Need More Help?

- [Full Troubleshooting Guide](Documentation/Troubleshooting-Azure-Authentication.md)
- [Scripts Documentation](scripts/README.md)
- [Entra ID Setup Guide](Documentation/Phase1-EntraID-Setup.md)
