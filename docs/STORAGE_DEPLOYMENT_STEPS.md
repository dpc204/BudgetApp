# Storage Account Deployment - Quick Steps

## What Changed
- **New storage account**: `fantumbudgetstorage` will be created in your resource group
- **Managed identity**: Automatically granted permissions to access the storage account
- **Old storage account**: `fantumstorage` in `rg-fantum` can be deleted after migration

---

## Deployment Steps

### 1. Deploy Infrastructure and Code
```powershell
# This will create the new storage account and deploy your app
azd up
```

**What happens:**
- ? Creates `fantumbudgetstorage` in your resource group
- ? Grants managed identity permissions (Blob + Table Data Contributor)
- ? Deploys updated API code with managed identity support
- ? Sets environment variables (`AZURE_STORAGE_BLOB_ENDPOINT`, `AZURE_STORAGE_TABLE_ENDPOINT`)

---

### 2. Verify Deployment

**Check storage account was created:**
```powershell
az storage account show --name fantumbudgetstorage --resource-group rg-BudgetApp2
```

**Check role assignments:**
```powershell
az role assignment list --scope "/subscriptions/3dd42e45-62af-4345-82d2-bffd522065f5/resourceGroups/rg-BudgetApp2/providers/Microsoft.Storage/storageAccounts/fantumbudgetstorage" --query "[].{Principal:principalName, Role:roleDefinitionName}" -o table
```

Should show:
- Storage Blob Data Contributor
- Storage Table Data Contributor

**Check API environment variables:**
```powershell
# Get your API container app name
az containerapp list --resource-group rg-BudgetApp2 --query "[?contains(name, 'api')].name" -o tsv

# Check environment variables (replace <api-app-name>)
az containerapp show --name <api-app-name> --resource-group rg-BudgetApp2 --query "properties.template.containers[0].env" -o table
```

Should include:
- `AZURE_STORAGE_BLOB_ENDPOINT`: https://fantumbudgetstorage.blob.core.windows.net/
- `AZURE_STORAGE_TABLE_ENDPOINT`: https://fantumbudgetstorage.table.core.windows.net/

---

### 3. Test ExportAll Endpoint

1. Navigate to your Azure-hosted app
2. Go to Maintenance ? Backup/Export
3. Click "Export All Tables"
4. Should succeed without 403 errors

**Check logs:**
```powershell
# Get recent logs
az containerapp logs show --name <api-app-name> --resource-group rg-BudgetApp2 --tail 50
```

Should see:
```
Azure Storage configured with Managed Identity (Blob: https://fantumbudgetstorage.blob.core.windows.net/, Table: https://fantumbudgetstorage.table.core.windows.net/)
```

---

### 4. Verify Data in Storage

**List containers:**
```powershell
az storage container list --account-name fantumbudgetstorage --auth-mode login -o table
```

Should show:
- `backups` container (created after first export)

**List blobs in backup:**
```powershell
az storage blob list --account-name fantumbudgetstorage --container-name backups --auth-mode login --query "[].name" -o table
```

---

### 5. Update Local Development (Optional)

If you want to use the new storage account locally:

**Get connection string:**
```powershell
az storage account show-connection-string --name fantumbudgetstorage --resource-group rg-BudgetApp2 --query connectionString -o tsv
```

**Update user secrets:**
```powershell
# For Budget.Api
dotnet user-secrets set "AzureStorage:ConnectionString" "<connection-string>" --project Budget.Api

# For Budget.Web (if needed)
dotnet user-secrets set "AzureStorage:ConnectionString" "<connection-string>" --project Budget.Web
```

---

### 6. Clean Up Old Storage Account (After Testing)

Once you've verified the new storage works:

```powershell
# Delete old storage account in rg-fantum
az storage account delete --name fantumstorage --resource-group rg-fantum --yes
```

?? **Warning**: This permanently deletes all data in `fantumstorage`. Make sure you've migrated any important backups first!

---

## Troubleshooting

### Issue: 403 errors persist

**Check RBAC propagation:**
Role assignments can take 5-10 minutes to propagate. Wait and retry.

**Verify managed identity:**
```powershell
az containerapp show --name <api-app-name> --resource-group rg-BudgetApp2 --query "identity.type" -o tsv
```

Should show: `SystemAssigned,UserAssigned` or `UserAssigned`

### Issue: "Azure Storage not configured" warning

**Re-deploy to inject environment variables:**
```powershell
azd deploy
```

### Issue: Storage account already exists

If `fantumbudgetstorage` name is taken globally:

1. Edit `infra/resources.bicep`
2. Change storage account name to something unique:
   ```bicep
   name: 'fantumbudget2025'  // Or any unique name
   ```
3. Run `azd provision`

---

## Architecture

### Before (Local + Azure with connection strings)
```
Budget.Api ? Connection String ? fantumstorage (rg-fantum)
```

### After (Local + Azure with managed identity)
```
Local:
Budget.Api ? Connection String ? fantumbudgetstorage (rg-BudgetApp2)

Azure:
Budget.Api ? Managed Identity ? fantumbudgetstorage (rg-BudgetApp2)
           ? (Automatic Azure AD auth)
           ? Storage Blob Data Contributor role
           ? Storage Table Data Contributor role
```

---

## Summary

? **New storage account**: `fantumbudgetstorage` in your app's resource group  
? **Managed identity**: Automatic authentication, no connection strings on Azure  
? **RBAC permissions**: Least-privilege access via role assignments  
? **Local development**: Still uses connection strings from user secrets  
? **Infrastructure as code**: Fully defined in Bicep templates  

Ready to deploy with `azd up`! ??
