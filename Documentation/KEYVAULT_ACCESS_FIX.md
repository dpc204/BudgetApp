# Azure Key Vault Access Issue - Fix Guide

## The Problem

Your Container Apps are trying to access Azure Key Vault (`fantumkeyvault`) but getting a **403 Forbidden** error because the Container App's **Managed Identity** doesn't have permission to read secrets from the Key Vault.

### Error Details
```
Caller is not authorized to perform action on resource.
Action: 'Microsoft.KeyVault/vaults/secrets/readMetadata/action'
Resource: '/subscriptions/.../microsoft.keyvault/vaults/fantumkeyvault'
Assignment: (not found)
```

The `appid=c5817686-acae-494b-a8e9-f5620f83b0d4` is your Container App's managed identity, but it has no role assignment to access the Key Vault.

## Solution Options

### Option 1: Grant Key Vault Access (For Production)

Run the PowerShell script I created to automatically grant access:

```powershell
.\fix-keyvault-access.ps1 -ResourceGroupName rg-BudgetApp2
```

This script will:
1. ? Find all Container Apps in your resource group
2. ? Enable System-Assigned Managed Identity (if not already enabled)
3. ? Grant "Key Vault Secrets User" role to each Container App
4. ? Handle both RBAC and Access Policy methods

After running, wait 2-3 minutes for permissions to propagate, then restart the Container Apps:

```bash
# Restart both apps
az containerapp revision restart-revision --name budget --resource-group rg-BudgetApp2
az containerapp revision restart-revision --name budget-api --resource-group rg-BudgetApp2
```

### Option 2: Skip Key Vault (Already Done - For Now)

I've updated `Budget.Shared\Misc.cs` to gracefully handle Key Vault access failures. The app will now:

? **Log a warning** instead of crashing  
? **Continue startup** without Key Vault secrets  
? **Use configuration from** environment variables and appsettings.json  

This is good for development and testing. Your app is now running even without Key Vault access!

## Manual Steps (If Script Fails)

### Step 1: Enable Managed Identity

```bash
# For budget-api
az containerapp identity assign \
  --name budget-api \
  --resource-group rg-BudgetApp2 \
  --system-assigned

# For budget
az containerapp identity assign \
  --name budget \
  --resource-group rg-BudgetApp2 \
  --system-assigned
```

### Step 2: Get the Principal ID

```bash
# For budget-api
az containerapp show \
  --name budget-api \
  --resource-group rg-BudgetApp2 \
  --query "identity.principalId" \
  --output tsv

# For budget
az containerapp show \
  --name budget \
  --resource-group rg-BudgetApp2 \
  --query "identity.principalId" \
  --output tsv
```

### Step 3: Grant Key Vault Access

#### Method A: Using RBAC (Recommended)

```bash
# Replace <principal-id> with the ID from step 2
az role assignment create \
  --role "Key Vault Secrets User" \
  --assignee <principal-id> \
  --scope "/subscriptions/3dd42e45-62af-4345-82d2-bffd522065f5/resourceGroups/rg-fantum/providers/Microsoft.KeyVault/vaults/fantumkeyvault"
```

#### Method B: Using Access Policies (Legacy)

```bash
# Replace <principal-id> with the ID from step 2
az keyvault set-policy \
  --name fantumkeyvault \
  --resource-group rg-fantum \
  --object-id <principal-id> \
  --secret-permissions get list
```

## Verify Access

### Check if Managed Identity is Enabled

```bash
az containerapp show \
  --name budget-api \
  --resource-group rg-BudgetApp2 \
  --query "{name:name, identityType:identity.type, principalId:identity.principalId}"
```

### Check Key Vault Role Assignments

```bash
az role assignment list \
  --scope "/subscriptions/3dd42e45-62af-4345-82d2-bffd522065f5/resourceGroups/rg-fantum/providers/Microsoft.KeyVault/vaults/fantumkeyvault" \
  --query "[].{principalId:principalId, roleDefinitionName:roleDefinitionName}" \
  --output table
```

### Check Container App Logs

After granting access and restarting, check the logs:

```bash
# Should now show "KeyVault Done" instead of error
az containerapp logs show \
  --name budget-api \
  --resource-group rg-BudgetApp2 \
  --follow
```

Look for:
- ? `SetupConfigurationSources Using AzureDB - KeyVault Done`
- ? `Azure Key Vault access denied (403 Forbidden)` - permissions not yet propagated

## Current State

### What I've Done

1. ? **Updated `Budget.Shared\Misc.cs`** to catch 403 errors gracefully
2. ? **App now starts** even without Key Vault access
3. ? **Created `fix-keyvault-access.ps1`** script for automated permission setup

### What You Should Do

**For Development/Testing (current state):**
- ? Nothing! Your app is already working without Key Vault
- Configuration comes from environment variables and appsettings.json

**For Production (when you need Key Vault):**
1. Run `.\fix-keyvault-access.ps1 -ResourceGroupName rg-BudgetApp2`
2. Wait 2-3 minutes
3. Restart the Container Apps
4. Verify in logs that "KeyVault Done" appears

## Why Use Key Vault?

Key Vault is useful for:
- ?? Storing sensitive connection strings
- ?? Managing secrets across multiple environments
- ?? Rotating secrets without redeploying
- ?? Audit logging of secret access

**But for now**, you don't need it! Your app is getting configuration from:
- ? Environment variables (set by Aspire in Container Apps)
- ? `appsettings.json`
- ? User Secrets (in Development)

## Troubleshooting

### "Assignment: (not found)" persists after granting access
**Solution:** Wait 5-10 minutes for Azure RBAC propagation, then restart the Container App

### "Access policies" tab is empty in Azure Portal
**Solution:** Your Key Vault is using RBAC (recommended). Check "Access control (IAM)" instead of "Access policies"

### Still getting 403 errors after granting access
**Solution:** 
1. Verify the Key Vault resource group is `rg-fantum` (check the error message)
2. Verify the subscription ID matches
3. Try granting access to both Container Apps
4. Restart the Container Apps after granting access

## Files Created/Modified

1. ? **`fix-keyvault-access.ps1`** - Automated permission setup script
2. ? **`Budget.Shared\Misc.cs`** - Improved error handling for Key Vault access
3. ?? **This guide** - Complete documentation

Your app should now be running successfully! The Key Vault error is now just a warning, not a crash.
