# Azure Configuration Verification Script
# Run this in PowerShell to verify your Azure Container Apps configuration

param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory=$false)]
    [string]$BudgetWebAppName = "budget",
    
    [Parameter(Mandatory=$false)]
    [string]$BudgetApiAppName = "budget-api"
)

Write-Host "=== Azure Container Apps Configuration Verification ===" -ForegroundColor Cyan
Write-Host ""

# Check if Azure CLI is installed
if (!(Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: Azure CLI is not installed. Please install it first." -ForegroundColor Red
    exit 1
}

# Check if logged in
$account = az account show 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Not logged in to Azure. Please run 'az login' first." -ForegroundColor Red
    exit 1
}

Write-Host "? Logged in to Azure" -ForegroundColor Green
Write-Host ""

# Get Budget.Api URL
Write-Host "Checking Budget.Api container app..." -ForegroundColor Yellow
$apiUrl = az containerapp show `
    --name $BudgetApiAppName `
    --resource-group $ResourceGroupName `
    --query "properties.configuration.ingress.fqdn" `
    --output tsv 2>$null

if ($LASTEXITCODE -eq 0 -and $apiUrl) {
    $fullApiUrl = "https://$apiUrl"
    Write-Host "? Budget.Api URL: $fullApiUrl" -ForegroundColor Green
} else {
    Write-Host "? Could not find Budget.Api container app '$BudgetApiAppName'" -ForegroundColor Red
    $fullApiUrl = $null
}

Write-Host ""

# Get Budget.Web environment variables
Write-Host "Checking Budget.Web container app..." -ForegroundColor Yellow
$webEnvVars = az containerapp show `
    --name $BudgetWebAppName `
    --resource-group $ResourceGroupName `
    --query "properties.template.containers[0].env" `
    --output json 2>$null | ConvertFrom-Json

if ($LASTEXITCODE -eq 0 -and $webEnvVars) {
    Write-Host "? Budget.Web container app found" -ForegroundColor Green
    
    # Check for BUDGET_API_URL
    $budgetApiUrlEnv = $webEnvVars | Where-Object { $_.name -eq "BUDGET_API_URL" }
    
    Write-Host ""
    Write-Host "Environment Variables Check:" -ForegroundColor Cyan
    
    if ($budgetApiUrlEnv) {
        Write-Host "  ? BUDGET_API_URL is set to: $($budgetApiUrlEnv.value)" -ForegroundColor Green
        
        if ($fullApiUrl -and $budgetApiUrlEnv.value -ne $fullApiUrl) {
            Write-Host "  ? WARNING: BUDGET_API_URL does not match Budget.Api URL!" -ForegroundColor Yellow
            Write-Host "    Expected: $fullApiUrl" -ForegroundColor Yellow
            Write-Host "    Actual:   $($budgetApiUrlEnv.value)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "  ? BUDGET_API_URL is NOT set!" -ForegroundColor Red
        Write-Host "    This MUST be set for Azure deployments to work." -ForegroundColor Red
        
        if ($fullApiUrl) {
            Write-Host ""
            Write-Host "To fix this, run:" -ForegroundColor Yellow
            Write-Host "az containerapp update --name $BudgetWebAppName --resource-group $ResourceGroupName --set-env-vars BUDGET_API_URL=$fullApiUrl" -ForegroundColor White
        }
    }
} else {
    Write-Host "? Could not find Budget.Web container app '$BudgetWebAppName'" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Verification Complete ===" -ForegroundColor Cyan
