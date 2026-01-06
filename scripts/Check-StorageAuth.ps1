# Check Storage Authentication Configuration
# Run this script to diagnose 403 storage errors

Write-Host "=== Storage Authentication Diagnostic Tool ===" -ForegroundColor Cyan
Write-Host ""

# Get resource group and subscription
$subscriptionId = "3dd42e45-62af-4345-82d2-bffd522065f5"
$resourceGroup = "rg-BudgetApp2"
$storageAccountName = "fantumbudgetstorage"

Write-Host "Checking configuration for:" -ForegroundColor Yellow
Write-Host "  Subscription: $subscriptionId"
Write-Host "  Resource Group: $resourceGroup"
Write-Host "  Storage Account: $storageAccountName"
Write-Host ""

# Step 1: Check if storage account exists
Write-Host "1. Checking if storage account exists..." -ForegroundColor Cyan
try {
    $storageAccount = az storage account show --name $storageAccountName --resource-group $resourceGroup 2>$null | ConvertFrom-Json
    if ($storageAccount) {
        Write-Host "   ? Storage account exists" -ForegroundColor Green
    }
} catch {
    Write-Host "   ? Storage account not found!" -ForegroundColor Red
    Write-Host "   Run: azd provision" -ForegroundColor Yellow
    exit 1
}

# Step 2: Check managed identities
Write-Host ""
Write-Host "2. Checking managed identities..." -ForegroundColor Cyan
$identities = az identity list --resource-group $resourceGroup | ConvertFrom-Json
if ($identities.Count -eq 0) {
    Write-Host "   ? No managed identities found!" -ForegroundColor Red
    exit 1
}

Write-Host "   Found $($identities.Count) managed identity(ies):" -ForegroundColor Green
foreach ($identity in $identities) {
    Write-Host "   - $($identity.name) (PrincipalId: $($identity.principalId))" -ForegroundColor Gray
}

$managedIdentity = $identities[0]
$principalId = $managedIdentity.principalId
Write-Host "   Using: $($managedIdentity.name)" -ForegroundColor Yellow
Write-Host ""

# Step 3: Check role assignments on storage account
Write-Host "3. Checking RBAC role assignments on storage account..." -ForegroundColor Cyan
$storageScope = "/subscriptions/$subscriptionId/resourceGroups/$resourceGroup/providers/Microsoft.Storage/storageAccounts/$storageAccountName"
$roleAssignments = az role assignment list --scope $storageScope | ConvertFrom-Json

$blobRole = $roleAssignments | Where-Object { $_.roleDefinitionName -eq "Storage Blob Data Contributor" -and $_.principalId -eq $principalId }
$tableRole = $roleAssignments | Where-Object { $_.roleDefinitionName -eq "Storage Table Data Contributor" -and $_.principalId -eq $principalId }

if ($blobRole) {
    Write-Host "   ? Storage Blob Data Contributor role assigned" -ForegroundColor Green
} else {
    Write-Host "   ? Storage Blob Data Contributor role MISSING!" -ForegroundColor Red
    Write-Host "   Fix: az role assignment create --role `"Storage Blob Data Contributor`" --assignee $principalId --scope $storageScope" -ForegroundColor Yellow
}

if ($tableRole) {
    Write-Host "   ? Storage Table Data Contributor role assigned" -ForegroundColor Green
} else {
    Write-Host "   ? Storage Table Data Contributor role MISSING!" -ForegroundColor Red
    Write-Host "   Fix: az role assignment create --role `"Storage Table Data Contributor`" --assignee $principalId --scope $storageScope" -ForegroundColor Yellow
}
Write-Host ""

# Step 4: Check container app
Write-Host "4. Checking container app configuration..." -ForegroundColor Cyan
$containerApps = az containerapp list --resource-group $resourceGroup | ConvertFrom-Json
$apiApp = $containerApps | Where-Object { $_.name -like "*api*" } | Select-Object -First 1

if (-not $apiApp) {
    Write-Host "   ? API container app not found!" -ForegroundColor Red
    exit 1
}

Write-Host "   Found API app: $($apiApp.name)" -ForegroundColor Green

# Check if managed identity is assigned
$appIdentity = $apiApp.identity
if ($appIdentity -and $appIdentity.type -like "*UserAssigned*") {
    Write-Host "   ? Managed identity assigned to container app" -ForegroundColor Green
} else {
    Write-Host "   ? Managed identity NOT assigned to container app!" -ForegroundColor Red
    Write-Host "   Fix: az containerapp identity assign --name $($apiApp.name) --resource-group $resourceGroup --user-assigned $($managedIdentity.id)" -ForegroundColor Yellow
}

# Check environment variables
$envVars = $apiApp.properties.template.containers[0].env
$blobEndpoint = $envVars | Where-Object { $_.name -eq "AZURE_STORAGE_BLOB_ENDPOINT" }
$tableEndpoint = $envVars | Where-Object { $_.name -eq "AZURE_STORAGE_TABLE_ENDPOINT" }

if ($blobEndpoint) {
    Write-Host "   ? AZURE_STORAGE_BLOB_ENDPOINT set: $($blobEndpoint.value)" -ForegroundColor Green
} else {
    Write-Host "   ? AZURE_STORAGE_BLOB_ENDPOINT not set!" -ForegroundColor Red
    Write-Host "   Fix: azd deploy" -ForegroundColor Yellow
}

if ($tableEndpoint) {
    Write-Host "   ? AZURE_STORAGE_TABLE_ENDPOINT set: $($tableEndpoint.value)" -ForegroundColor Green
} else {
    Write-Host "   ? AZURE_STORAGE_TABLE_ENDPOINT not set!" -ForegroundColor Red
    Write-Host "   Fix: azd deploy" -ForegroundColor Yellow
}
Write-Host ""

# Step 5: Summary
Write-Host "=== Summary ===" -ForegroundColor Cyan
$issues = 0

if (-not $blobRole) { $issues++ }
if (-not $tableRole) { $issues++ }
if (-not ($appIdentity -and $appIdentity.type -like "*UserAssigned*")) { $issues++ }
if (-not $blobEndpoint) { $issues++ }
if (-not $tableEndpoint) { $issues++ }

if ($issues -eq 0) {
    Write-Host "? All checks passed! Configuration looks correct." -ForegroundColor Green
    Write-Host ""
    Write-Host "If you're still getting 403 errors:" -ForegroundColor Yellow
    Write-Host "1. Wait 5-10 minutes for RBAC propagation" -ForegroundColor Gray
    Write-Host "2. Restart the container app:" -ForegroundColor Gray
    Write-Host "   az containerapp revision restart --name $($apiApp.name) --resource-group $resourceGroup" -ForegroundColor Gray
    Write-Host "3. Check logs:" -ForegroundColor Gray
    Write-Host "   az containerapp logs show --name $($apiApp.name) --resource-group $resourceGroup --tail 50" -ForegroundColor Gray
} else {
    Write-Host "? Found $issues issue(s). Fix the items marked above and try again." -ForegroundColor Red
    Write-Host ""
    Write-Host "Quick fix commands:" -ForegroundColor Yellow
    
    if (-not $blobRole) {
        Write-Host "az role assignment create --role `"Storage Blob Data Contributor`" --assignee $principalId --scope $storageScope" -ForegroundColor Gray
    }
    
    if (-not $tableRole) {
        Write-Host "az role assignment create --role `"Storage Table Data Contributor`" --assignee $principalId --scope $storageScope" -ForegroundColor Gray
    }
    
    if (-not $blobEndpoint -or -not $tableEndpoint) {
        Write-Host "azd deploy" -ForegroundColor Gray
    }
    
    if (-not ($appIdentity -and $appIdentity.type -like "*UserAssigned*")) {
        Write-Host "az containerapp identity assign --name $($apiApp.name) --resource-group $resourceGroup --user-assigned $($managedIdentity.id)" -ForegroundColor Gray
    }
}

Write-Host ""
