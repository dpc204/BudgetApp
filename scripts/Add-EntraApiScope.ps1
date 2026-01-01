#Requires -Version 7.0

<#
.SYNOPSIS
    Adds API scope to FantumBudget Entra ID app registration for Budget.Api authentication.

.DESCRIPTION
    This script:
    1. Adds an "access_as_user" API scope to the app registration
    2. Configures the app to expose this API
    3. Pre-authorizes the client app to use this scope

.PARAMETER AppName
    The display name of the app registration (default: "FantumBudget")

.EXAMPLE
    .\Add-EntraApiScope.ps1
    Adds API scope to FantumBudget app
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$AppName = "FantumBudget"
)

$ErrorActionPreference = "Stop"

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Entra ID API Scope Setup" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Check if Azure CLI is installed
try {
    $null = az version 2>$null
} catch {
    Write-Error "Azure CLI is not installed. Install from: https://aka.ms/installazurecli"
    exit 1
}

# Check if logged in
Write-Host "Checking Azure CLI authentication..." -ForegroundColor Yellow
$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Host "Not logged in. Running 'az login'..." -ForegroundColor Yellow
    az login
    $account = az account show | ConvertFrom-Json
}

Write-Host "? Logged in as: $($account.user.name)" -ForegroundColor Green
Write-Host ""

# Get app registration
Write-Host "Finding app registration: $AppName..." -ForegroundColor Yellow
$appJson = az ad app list --display-name $AppName | ConvertFrom-Json
if ($appJson.Count -eq 0) {
    Write-Error "App registration '$AppName' not found. Please create it first."
    exit 1
}

$app = $appJson[0]
Write-Host "? Found app: $($app.displayName) (AppId: $($app.appId))" -ForegroundColor Green
Write-Host ""

# Check if API scope already exists
$existingScopes = $app.api.oauth2PermissionScopes
$scopeValue = "access_as_user"
$existingScope = $existingScopes | Where-Object { $_.value -eq $scopeValue }

if ($existingScope) {
    Write-Host "? API scope '$scopeValue' already exists" -ForegroundColor Green
    Write-Host "  Scope ID: $($existingScope.id)" -ForegroundColor Gray
} else {
    Write-Host "Adding API scope '$scopeValue'..." -ForegroundColor Yellow
    
    # Create new scope
    $scopeId = [guid]::NewGuid().ToString()
    $newScope = @{
        id = $scopeId
        adminConsentDescription = "Allows the app to access Budget.Api as the signed-in user"
        adminConsentDisplayName = "Access Budget.Api"
        isEnabled = $true
        type = "User"
        userConsentDescription = "Allows the app to access Budget.Api on your behalf"
        userConsentDisplayName = "Access Budget.Api"
        value = $scopeValue
    }
    
    # Combine with existing scopes
    $allScopes = @()
    $allScopes += $existingScopes
    $allScopes += $newScope
    
    # Build API configuration
    $apiConfig = @{
        oauth2PermissionScopes = $allScopes
    }
    
    $apiJson = $apiConfig | ConvertTo-Json -Depth 10 -Compress
    
    # Update app registration
    az ad app update --id $app.appId --identifier-uris "api://$($app.appId)"
    az ad app update --id $app.appId --set "api=$apiJson"
    
    Write-Host "? API scope added successfully" -ForegroundColor Green
    Write-Host "  Scope: api://$($app.appId)/$scopeValue" -ForegroundColor Gray
}

Write-Host ""

# Pre-authorize the client app (itself)
Write-Host "Configuring pre-authorized client applications..." -ForegroundColor Yellow
$app = (az ad app show --id $app.appId | ConvertFrom-Json)
$scopeId = ($app.api.oauth2PermissionScopes | Where-Object { $_.value -eq $scopeValue }).id

$preAuthorizedApp = @{
    appId = $app.appId
    delegatedPermissionIds = @($scopeId)
}

$existingPreAuth = $app.api.preAuthorizedApplications | Where-Object { $_.appId -eq $app.appId }

if ($existingPreAuth) {
    Write-Host "? Client app already pre-authorized" -ForegroundColor Green
} else {
    $allPreAuth = @()
    $allPreAuth += $app.api.preAuthorizedApplications
    $allPreAuth += $preAuthorizedApp
    
    $apiConfig = @{
        preAuthorizedApplications = $allPreAuth
        oauth2PermissionScopes = $app.api.oauth2PermissionScopes
    }
    
    $apiJson = $apiConfig | ConvertTo-Json -Depth 10 -Compress
    az ad app update --id $app.appId --set "api=$apiJson"
    
    Write-Host "? Client app pre-authorized" -ForegroundColor Green
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Setup Complete!" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "API Scope Configuration:" -ForegroundColor Cyan
Write-Host "  App Name: $AppName" -ForegroundColor White
Write-Host "  App ID: $($app.appId)" -ForegroundColor White
Write-Host "  API Scope: api://$($app.appId)/$scopeValue" -ForegroundColor White
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "1. Restart your Budget.Web application" -ForegroundColor White
Write-Host "2. Log out and log back in" -ForegroundColor White
Write-Host "3. Test the /utilities/export-all endpoint" -ForegroundColor White
Write-Host ""
