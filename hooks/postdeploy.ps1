# Post-deployment hook to assign Managed Identity to Container Apps
# and publish the BACPAC Azure Function App.
# This runs after azd provisions resources and deploys containers

Write-Host "=== Post-Deploy Hook: Configuring Managed Identity for Container Apps ===" -ForegroundColor Cyan

# Get environment variables set by azd
$managedIdentityClientId = $env:MANAGED_IDENTITY_CLIENT_ID
$managedIdentityName = $env:MANAGED_IDENTITY_NAME
$resourceGroup = "rg-$env:AZURE_ENV_NAME"
$subscriptionId = $env:AZURE_SUBSCRIPTION_ID
$containerAppsBudget = "budget"
$containerAppsBudgetApi = "budget-api"
$functionAppName = $env:BACPAC_FUNCTION_APP_NAME

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
    $assignOutput = az containerapp identity assign `
        --name $AppName `
        --resource-group $resourceGroup `
        --user-assigned $managedIdentityResourceId `
        2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ? Failed to assign Managed Identity" -ForegroundColor Red
        Write-Host "  Error: $assignOutput" -ForegroundColor Red
        return $false
    }
    Write-Host "  ? Identity assigned" -ForegroundColor Green
    
    # Step 2: Set AZURE_CLIENT_ID environment variable (required by DefaultAzureCredential)
    Write-Host "  [2/2] Setting AZURE_CLIENT_ID environment variable..." -ForegroundColor Gray
    $updateOutput = az containerapp update `
        --name $AppName `
        --resource-group $resourceGroup `
        --set-env-vars "AZURE_CLIENT_ID=$managedIdentityClientId" `
        2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ? Failed to set environment variable" -ForegroundColor Red
        Write-Host "  Error: $updateOutput" -ForegroundColor Red
        return $false
    }
    Write-Host "  ? Environment variable set" -ForegroundColor Green
    
    Write-Host "  ? Successfully configured $AppName" -ForegroundColor Green
    return $true
}

# Configure both Container Apps
$budgetSuccess = Configure-ContainerApp -AppName $containerAppsBudget
$apiSuccess = Configure-ContainerApp -AppName $containerAppsBudgetApi

if ($budgetSuccess -and $apiSuccess) {
    Write-Host "`n? All Container Apps configured successfully!" -ForegroundColor Green
    Write-Host "Note: Container Apps will restart automatically to apply changes." -ForegroundColor Cyan
    $exitCode = 0
} else {
    Write-Host "`n? Some Container Apps failed to configure" -ForegroundColor Red
    if (-not $budgetSuccess) {
        Write-Host "  - Failed: $containerAppsBudget" -ForegroundColor Red
    }
    if (-not $apiSuccess) {
        Write-Host "  - Failed: $containerAppsBudgetApi" -ForegroundColor Red
    }
    $exitCode = 1
}

Write-Host "`n=== Post-Deploy Hook Complete ===" -ForegroundColor Cyan

# ── Deploy BACPAC Azure Function ──────────────────────────────────────────────
if ([string]::IsNullOrEmpty($functionAppName)) {
    Write-Host "`nWARN: BACPAC_FUNCTION_APP_NAME not set – skipping Function App deployment." -ForegroundColor Yellow
} else {
    Write-Host "`n=== Deploying BACPAC Azure Function: $functionAppName ===" -ForegroundColor Cyan

    $scriptRoot = $PSScriptRoot
    $repoRoot = Split-Path $scriptRoot -Parent
    $functionProject = Join-Path $repoRoot "Budget.Functions" "Budget.Functions.csproj"
    $publishDir = Join-Path ([System.IO.Path]::GetTempPath()) "bacpac-func-publish"
    $zipPath = Join-Path ([System.IO.Path]::GetTempPath()) "bacpac-func.zip"

    Write-Host "  [1/3] Publishing project..." -ForegroundColor Gray
    $publishOutput = dotnet publish $functionProject `
        --configuration Release `
        --output $publishDir `
        2>&1

    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ? dotnet publish failed:" -ForegroundColor Red
        Write-Host $publishOutput -ForegroundColor Red
        exit 1
    }
    Write-Host "  ? Published to $publishDir" -ForegroundColor Green

    Write-Host "  [2/3] Creating deployment ZIP..." -ForegroundColor Gray
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -Force
    Write-Host "  ? ZIP created at $zipPath" -ForegroundColor Green

    Write-Host "  [3/3] Deploying to Function App $functionAppName..." -ForegroundColor Gray
    $deployOutput = az functionapp deploy `
        --resource-group $resourceGroup `
        --name $functionAppName `
        --src-path $zipPath `
        --type zip `
        2>&1

    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ? Deployment failed:" -ForegroundColor Red
        Write-Host $deployOutput -ForegroundColor Red
        exit 1
    }
    Write-Host "  ? Function App deployed successfully!" -ForegroundColor Green

    # Clean up temp files
    Remove-Item $publishDir -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $zipPath -Force -ErrorAction SilentlyContinue
}

Write-Host "`n=== All Post-Deploy Steps Complete ===" -ForegroundColor Cyan
Write-Host "`nPress any key to close this window..." -ForegroundColor Yellow
#$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

exit $exitCode
