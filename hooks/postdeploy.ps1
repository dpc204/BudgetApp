# Post-deployment hook to assign Managed Identity to Container Apps
# This runs after azd provisions resources and deploys containers

Write-Host "=== Post-Deploy Hook: Configuring Managed Identity for Container Apps ===" -ForegroundColor Cyan

# Get environment variables set by azd
$managedIdentityClientId = $env:MANAGED_IDENTITY_CLIENT_ID
$managedIdentityName = $env:MANAGED_IDENTITY_NAME
$resourceGroup = "rg-$env:AZURE_ENV_NAME"
$subscriptionId = $env:AZURE_SUBSCRIPTION_ID
$containerAppsBudget = "budget"
$containerAppsBudgetApi = "budget-api"

if ([string]::IsNullOrEmpty($managedIdentityClientId)) {
    Write-Host "ERROR: MANAGED_IDENTITY_CLIENT_ID not found in environment" -ForegroundColor Red
    exit 1
}

Write-Host "Managed Identity Client ID: $managedIdentityClientId" -ForegroundColor Green
Write-Host "Managed Identity Name: $managedIdentityName" -ForegroundColor Green
Write-Host "Resource Group: $resourceGroup" -ForegroundColor Green

$managedIdentityResourceId = "/subscriptions/$subscriptionId/resourceGroups/$resourceGroup/providers/Microsoft.ManagedIdentity/userAssignedIdentities/$managedIdentityName"

# Function to configure Container App with Managed Identity
function Configure-ContainerApp {
    param(
        [string]$AppName
    )
    
    Write-Host "`nConfiguring Container App: $AppName" -ForegroundColor Yellow
    
    # Step 1: Assign user-assigned Managed Identity
    Write-Host "  [1/2] Assigning Managed Identity..." -ForegroundColor Gray
    az containerapp identity assign `
        --name $AppName `
        --resource-group $resourceGroup `
        --user-assigned $managedIdentityResourceId `
        --output none
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ? Failed to assign Managed Identity" -ForegroundColor Red
        return $false
    }
    
    # Step 2: Set AZURE_CLIENT_ID environment variable (required by DefaultAzureCredential)
    Write-Host "  [2/2] Setting AZURE_CLIENT_ID environment variable..." -ForegroundColor Gray
    az containerapp update `
        --name $AppName `
        --resource-group $resourceGroup `
        --set-env-vars "AZURE_CLIENT_ID=$managedIdentityClientId" `
        --output none
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ? Failed to set environment variable" -ForegroundColor Red
        return $false
    }
    
    Write-Host "  ? Successfully configured $AppName" -ForegroundColor Green
    return $true
}

# Configure both Container Apps
$budgetSuccess = Configure-ContainerApp -AppName $containerAppsBudget
$apiSuccess = Configure-ContainerApp -AppName $containerAppsBudgetApi

if ($budgetSuccess -and $apiSuccess) {
    Write-Host "`n? All Container Apps configured successfully!" -ForegroundColor Green
    Write-Host "Note: Container Apps will restart automatically to apply changes." -ForegroundColor Cyan
} else {
    Write-Host "`n? Some Container Apps failed to configure" -ForegroundColor Red
    exit 1
}

Write-Host "`n=== Post-Deploy Hook Complete ===" -ForegroundColor Cyan
