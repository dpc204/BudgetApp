# Update-EntraRedirectUris-AzCli.ps1
# Updates Entra ID App Registration with current Azure Container Apps URLs using Azure CLI
# This version uses Azure CLI instead of Microsoft Graph PowerShell

param(
    [Parameter(Mandatory=$true)]
    [string]$Environment,
    
    [string]$AppId = "36ca674b-1c79-49ad-98fb-b90f13d72887"
)

Write-Host "?? Getting Container App URL..." -ForegroundColor Cyan

# Get the resource group name
$resourceGroup = "rg-$Environment"

# Get the Container App configuration including latest revision name
$containerAppJson = az containerapp show `
    --name budget `
    --resource-group $resourceGroup `
    --output json 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to get Container App information. Is the app deployed?"
    exit 1
}

$containerApp = $containerAppJson | ConvertFrom-Json

# Get the latest active revision FQDN (this includes the revision number)
$latestRevisionName = $containerApp.properties.latestRevisionName
$revisionFqdn = $containerApp.properties.configuration.ingress.fqdn

# The FQDN from ingress.fqdn is the base domain
# We need to construct the full URL with the revision
$baseDomain = $revisionFqdn -replace '^[^.]+\.', ''  # Remove first part before first dot
$revisionPrefix = $latestRevisionName -replace '^budget--', ''  # Extract just the revision number

# Construct the full revision-specific URL
$revisionUrl = "https://budget--$revisionPrefix.$baseDomain"
$baseUrl = "https://$revisionFqdn"

Write-Host "? Found revision: $latestRevisionName" -ForegroundColor Green
Write-Host "   Base URL: $baseUrl" -ForegroundColor Gray
Write-Host "   Revision URL: $revisionUrl" -ForegroundColor Gray

# Get the app registration
Write-Host "?? Getting app registration..." -ForegroundColor Cyan
$appJson = az ad app show --id $AppId 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to get app registration. Are you logged in? Run: az login"
    exit 1
}

$app = $appJson | ConvertFrom-Json

# Get existing redirect URIs
$existingUris = $app.web.redirectUris

# Define new URIs (both base and revision-specific)
$newUris = @(
    "$baseUrl/signin-oidc",
    "$baseUrl/signout-callback-oidc",
    "$revisionUrl/signin-oidc",
    "$revisionUrl/signout-callback-oidc"
)

# Check which URIs need to be added
$urisToAdd = @()
foreach ($uri in $newUris) {
    if ($existingUris -notcontains $uri) {
        $urisToAdd += $uri
    }
}

if ($urisToAdd.Count -eq 0) {
    Write-Host "? All redirect URIs already configured!" -ForegroundColor Green
    Write-Host "No changes needed." -ForegroundColor Gray
    exit 0
}

# Combine existing and new URIs (remove duplicates)
$updatedUris = ($existingUris + $newUris) | Select-Object -Unique

Write-Host "?? Updating app registration..." -ForegroundColor Cyan

# Update the app using the correct parameter format
az ad app update --id $AppId --web-redirect-uris $updatedUris 2>&1 | Out-Null

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to update redirect URIs"
    exit 1
}

Write-Host "`n? Redirect URIs updated successfully!" -ForegroundColor Green

if ($urisToAdd.Count -gt 0) {
    Write-Host "`nAdded URIs:" -ForegroundColor Cyan
    $urisToAdd | ForEach-Object { Write-Host "  • $_" -ForegroundColor White }
}

Write-Host "`nAll configured redirect URIs:" -ForegroundColor Cyan
$updatedUris | ForEach-Object { Write-Host "  • $_" -ForegroundColor Gray }

Write-Host "`n?? Done! You can now log in at:" -ForegroundColor Green
Write-Host "   Base URL: $baseUrl" -ForegroundColor White
Write-Host "   Revision URL: $revisionUrl" -ForegroundColor White
Write-Host "`n?? Tip: If you still can't log in, wait 1-2 minutes for changes to propagate." -ForegroundColor Yellow
