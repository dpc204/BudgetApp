# Azure Storage with Managed Identity - Deployment Guide

## Overview
This deployment adds Azure Storage Account to your infrastructure with managed identity authentication, fixing the 403 Authorization error when running the ExportAll endpoint on Azure.

## Changes Made

### 1. Infrastructure (`infra/resources.bicep`)
- ? Added Storage Account resource (`st{resourceToken}`)
- ? Added RBAC role assignments:
  - Storage Blob Data Contributor
  - Storage Table Data Contributor
- ? Added outputs for storage endpoints

### 2. Infrastructure Outputs (`infra/main.bicep`)
- ? Added storage account name and endpoint outputs

### 3. API Configuration (`Budget.Api/Program.cs`)
- ? Added managed identity authentication for Azure
- ? Kept connection string authentication for local development
- ? Automatic detection of Azure environment

## How It Works

### On Azure (Container Apps)
```
BlobServiceClient/TableServiceClient
    ?
Uses: Managed Identity (DefaultAzureCredential)
    ?
Authenticates: Automatically via Azure AD
    ?
Authorization: RBAC roles (Storage Blob/Table Data Contributor)
```

### Locally
```
BlobServiceClient/TableServiceClient
    ?
Uses: Connection String (from user secrets)
    ?
Authenticates: Storage account key
```

## Deployment Steps

### Step 1: Provision Infrastructure
This creates the storage account and assigns permissions:

```bash
azd provision
```

**What this does:**
- Creates Storage Account with unique name
- Assigns managed identity permissions
- Outputs storage endpoints to environment variables

### Step 2: Deploy Application
This deploys the updated code:

```bash
azd deploy
```

**What this does:**
- Builds and deploys the updated API code
- Injects storage endpoint environment variables
- Configures managed identity for the container app

### Step 3: Verify Deployment

1. **Check Container App Environment Variables:**
```bash
az containerapp show --name {your-api-app-name} --resource-group {your-rg} --query properties.configuration.secrets
```

Should include:
- `AZURE_STORAGE_BLOB_ENDPOINT`
- `AZURE_STORAGE_TABLE_ENDPOINT`

2. **Test the ExportAll Endpoint:**
- Navigate to your app
- Trigger the backup/export operation
- Check logs for: "Azure Storage configured with Managed Identity"

3. **Check Azure Storage:**
```bash
az storage container list --account-name {storage-account-name} --auth-mode login
```

Should show the `backups` container after first export.

## Troubleshooting

### Issue: Still getting 403 errors

**Solution 1: Wait for RBAC propagation**
Role assignments can take up to 5 minutes to propagate:
```bash
# Check role assignments
az role assignment list --assignee {managed-identity-principal-id} --scope {storage-account-resource-id}
```

**Solution 2: Verify managed identity is assigned to Container App**
```bash
az containerapp show --name {api-app-name} --resource-group {rg-name} --query identity
```

Should show the managed identity.

### Issue: "Azure Storage not configured" warning

**Check environment variables:**
```bash
az containerapp show --name {api-app-name} --resource-group {rg-name} --query properties.template.containers[0].env
```

If missing, re-run:
```bash
azd deploy
```

### Issue: Local development not working

**Verify user secrets:**
```bash
dotnet user-secrets list --project Budget.Api
```

Should include `AzureStorage:ConnectionString`.

## Security Benefits

### Before (Connection String Only)
- ? Storage account key in configuration
- ? Key rotation requires config updates
- ? Less audit trail
- ? Same key for all access

### After (Managed Identity on Azure)
- ? No secrets in configuration
- ? Automatic credential management
- ? Full Azure AD audit trail
- ? Granular RBAC permissions
- ? Automatic key rotation

## Cost Impact

**Storage Account (Standard_LRS):**
- Storage: ~$0.02/GB/month
- Transactions: Minimal for backup operations
- Expected: <$5/month for typical usage

## Rollback

If needed, rollback to connection strings only:

1. In `Budget.Api/Program.cs`, comment out the managed identity section
2. Ensure `AzureStorage:ConnectionString` is set in Azure configuration
3. Run `azd deploy`

## Next Steps

After successful deployment:
1. ? Remove `AzureStorage:ConnectionString` from Azure configuration (if present)
2. ? Test backup/export operations
3. ? Monitor storage costs in Azure portal
4. ? Set up retention policies if needed

## Support

- Azure Storage pricing: https://azure.microsoft.com/pricing/details/storage/
- Managed identities: https://learn.microsoft.com/azure/active-directory/managed-identities-azure-resources/
- DefaultAzureCredential: https://learn.microsoft.com/dotnet/api/azure.identity.defaultazurecredential
