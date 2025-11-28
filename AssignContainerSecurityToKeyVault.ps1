# Quick fix using the known principal ID from the error message
# Your error shows: oid=08e01e4c-71e1-4eba-a3d2-35a56fa65c28

param(
    [Parameter(Mandatory=$false)]
    [string]$KeyVaultName = "fantumkeyvault",
    
    [Parameter(Mandatory=$false)]
    [string]$KeyVaultResourceGroup = "rg-fantum",
    
    [Parameter(Mandatory=$false)]
    [string]$SubscriptionId = "3dd42e45-62af-4345-82d2-bffd522065f5"
)

Write-Host "=== Quick Key Vault Access Fix ===" -ForegroundColor Cyan
Write-Host ""

# The principal ID from your error message
$principalId = "08e01e4c-71e1-4eba-a3d2-35a56fa65c28"

Write-Host "Using Principal ID from error: $principalId" -ForegroundColor Yellow
Write-Host ""

# Grant Key Vault Secrets User role
Write-Host "Granting 'Key Vault Secrets User' role..." -ForegroundColor Yellow

$scope = "/subscriptions/$SubscriptionId/resourceGroups/$KeyVaultResourceGroup/providers/Microsoft.KeyVault/vaults/$KeyVaultName"

az role assignment create `
    --role "Key Vault Secrets User" `
    --assignee $principalId `
    --scope $scope `
    --output json

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Key Vault access granted successfully!" -ForegroundColor Green
} else {
    Write-Host "? RBAC assignment failed, trying access policy method..." -ForegroundColor Yellow
    
    az keyvault set-policy `
        --name $KeyVaultName `
        --resource-group $KeyVaultResourceGroup `
        --object-id $principalId `
        --secret-permissions get list `
        --output json
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? Key Vault access granted via access policy!" -ForegroundColor Green
    } else {
        Write-Host "? Failed to grant access" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "=== Success! ===" -ForegroundColor Green
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Wait 2-3 minutes for permissions to propagate" -ForegroundColor White
Write-Host "2. Restart your Container Apps:" -ForegroundColor White
Write-Host ""
Write-Host "   # Trigger restart by updating an env var" -ForegroundColor Gray
Write-Host "   az containerapp update --name budget-api --resource-group rg-BudgetApp2 --set-env-vars RESTART_TIME=`$(Get-Date -Format 'yyyyMMddHHmmss')" -ForegroundColor White
Write-Host "   az containerapp update --name budget --resource-group rg-BudgetApp2 --set-env-vars RESTART_TIME=`$(Get-Date -Format 'yyyyMMddHHmmss')" -ForegroundColor White
Write-Host ""
Write-Host "3. Check logs:" -ForegroundColor White
Write-Host "   az containerapp logs show --name budget-api --resource-group rg-BudgetApp2 --follow" -ForegroundColor Gray
Write-Host ""
Write-Host "Look for: 'SetupConfigurationSources Using AzureDB - KeyVault Done'" -ForegroundColor White
