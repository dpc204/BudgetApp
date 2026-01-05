# Storage 403 Error Troubleshooting Guide

## Enhanced Logging Added

The code now includes detailed logging at every step of the storage authentication process. Use this guide to diagnose 403 errors.

---

## Step 1: Deploy the Updated Code

```powershell
azd deploy
```

Wait for deployment to complete (2-3 minutes).

---

## Step 2: Trigger the ExportAll Endpoint

1. Navigate to your Azure-hosted app
2. Go to Maintenance ? Backup/Export
3. Click "Export All Tables"
4. Note the exact time you triggered it

---

## Step 3: Get Logs from Azure

### Option A: Using Azure Portal
1. Go to Azure Portal ? Your Container App
2. Click "Log stream" (left menu)
3. Look for logs around the time you triggered the export

### Option B: Using Azure CLI (Recommended)
```powershell
# Get your API container app name
$apiApp = az containerapp list --resource-group rg-BudgetApp2 --query "[?contains(name, 'api')].name" -o tsv

# Get recent logs (last 50 entries)
az containerapp logs show --name $apiApp --resource-group rg-BudgetApp2 --tail 50
```

### Option C: Get logs from specific time
```powershell
# Get logs from the last 5 minutes
az containerapp logs show --name $apiApp --resource-group rg-BudgetApp2 --follow --tail 100
```

---

## Step 4: Analyze the Logs

Look for these key log entries:

### A. Storage Configuration (Startup Logs)
```
=== Azure Storage Configuration ===
IsRunningOnAzure: True
StorageBlobEndpoint: https://fantumbudgetstorage.blob.core.windows.net/
StorageTableEndpoint: https://fantumbudgetstorage.table.core.windows.net/
Has ConnectionString: False
Creating BlobServiceClient with Managed Identity for: https://fantumbudgetstorage.blob.core.windows.net/
Creating TableServiceClient with Managed Identity for: https://fantumbudgetstorage.table.core.windows.net/
? Azure Storage configured with Managed Identity
```

**If you see connection string = True:** The app is using connection strings instead of managed identity.

**If endpoints are "(not set)":** Environment variables weren't injected - redeploy.

---

### B. Backup Execution Logs
```
=== Starting ExecuteBackupAsync ===
BackupId: xxx, PartitionKey: xxx
Got BlobContainerClient for container: backups
BlobServiceClient URI: https://fantumbudgetstorage.blob.core.windows.net/
Attempting to create container if not exists...
```

**If it stops here with 403:** The managed identity doesn't have blob permissions.

---

### C. Successful Authentication
```
? Container creation check complete
TableServiceClient URI: https://fantumbudgetstorage.table.core.windows.net/
Got TableClient for table: TableBackups
Attempting to create table if not exists...
? Table creation check complete
Found X tables to export: ...
```

**If you get here:** Authentication is working! The 403 is from something else.

---

### D. 403 Error Patterns

#### Pattern 1: Blob Container Creation Fails
```
? Failed to create/check blob container. Error: This request is not authorized to perform this operation.
```

**Cause:** Missing "Storage Blob Data Contributor" role.

**Fix:**
```powershell
# Get managed identity principal ID
$identity = az identity list --resource-group rg-BudgetApp2 --query "[0].principalId" -o tsv

# Grant Blob permissions
az role assignment create `
  --role "Storage Blob Data Contributor" `
  --assignee $identity `
  --scope "/subscriptions/3dd42e45-62af-4345-82d2-bffd522065f5/resourceGroups/rg-BudgetApp2/providers/Microsoft.Storage/storageAccounts/fantumbudgetstorage"
```

#### Pattern 2: Table Creation Fails
```
? Container creation check complete
? Failed to create/check table. Error: This request is not authorized to perform this operation.
```

**Cause:** Missing "Storage Table Data Contributor" role.

**Fix:**
```powershell
# Grant Table permissions
az role assignment create `
  --role "Storage Table Data Contributor" `
  --assignee $identity `
  --scope "/subscriptions/3dd42e45-62af-4345-82d2-bffd522065f5/resourceGroups/rg-BudgetApp2/providers/Microsoft.Storage/storageAccounts/fantumbudgetstorage"
```

#### Pattern 3: Wrong Storage Account
```
BlobServiceClient URI: https://DIFFERENT-ACCOUNT.blob.core.windows.net/
```

**Cause:** Environment variables pointing to wrong storage account.

**Fix:** Verify environment variables in container app match your Bicep outputs.

---

## Step 5: Verify Role Assignments

Check what roles are actually assigned:

```powershell
# List all role assignments on the storage account
az role assignment list `
  --scope "/subscriptions/3dd42e45-62af-4345-82d2-bffd522065f5/resourceGroups/rg-BudgetApp2/providers/Microsoft.Storage/storageAccounts/fantumbudgetstorage" `
  --query "[].{Principal:principalName, Role:roleDefinitionName, Scope:scope}" `
  -o table
```

**You should see:**
- Storage Blob Data Contributor (assigned to your managed identity)
- Storage Table Data Contributor (assigned to your managed identity)

---

## Step 6: Check Managed Identity Assignment

Verify the container app is using the managed identity:

```powershell
# Get container app identity
az containerapp show --name $apiApp --resource-group rg-BudgetApp2 --query "identity" -o json
```

**Expected output:**
```json
{
  "type": "UserAssigned",
  "userAssignedIdentities": {
    "/subscriptions/.../resourceGroups/rg-BudgetApp2/providers/Microsoft.ManagedIdentity/userAssignedIdentities/mi-xxx": {
      "clientId": "xxx",
      "principalId": "xxx"
    }
  }
}
```

**If empty or null:** Managed identity isn't assigned to the container app.

**Fix:**
```powershell
# Get managed identity resource ID
$identityId = az identity list --resource-group rg-BudgetApp2 --query "[0].id" -o tsv

# Assign to container app
az containerapp identity assign `
  --name $apiApp `
  --resource-group rg-BudgetApp2 `
  --user-assigned $identityId
```

---

## Step 7: Wait for RBAC Propagation

Role assignments can take **5-10 minutes** to propagate across Azure AD. If you just added roles:

1. Wait 5-10 minutes
2. Restart the container app:
   ```powershell
   az containerapp revision restart --name $apiApp --resource-group rg-BudgetApp2
   ```
3. Try the export again

---

## Common Issues and Solutions

### Issue: "DefaultAzureCredential failed to retrieve a token"

**Cause:** Managed identity not configured or not assigned.

**Fix:** Verify Steps 5 & 6 above.

---

### Issue: Logs show connection string usage on Azure

**Check:**
```powershell
az containerapp show --name $apiApp --resource-group rg-BudgetApp2 --query "properties.template.containers[0].env[?name=='AZURE_STORAGE_BLOB_ENDPOINT'].value" -o tsv
```

**If empty:** Environment variables not set. Run `azd deploy` again.

---

### Issue: Works locally but fails on Azure

**Local:** Uses connection string (with account key)  
**Azure:** Uses managed identity (with RBAC)

This confirms the issue is with RBAC role assignments, not the code.

---

## Step 8: Nuclear Option - Re-provision Everything

If all else fails, delete and recreate the storage account:

```powershell
# Delete storage account
az storage account delete --name fantumbudgetstorage --resource-group rg-BudgetApp2 --yes

# Re-provision (this recreates with role assignments)
azd provision

# Deploy code
azd deploy
```

**Warning:** This deletes all existing backups in the storage account.

---

## Getting Help

If you're still stuck, gather this information:

1. **Logs from Step 3**
2. **Role assignments from Step 5**
3. **Identity info from Step 6**
4. **Environment variables:**
   ```powershell
   az containerapp show --name $apiApp --resource-group rg-BudgetApp2 --query "properties.template.containers[0].env" -o table
   ```

Share these in your support request with the exact error message.
