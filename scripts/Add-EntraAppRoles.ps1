#Requires -Version 7.0

<#
.SYNOPSIS
    Adds app roles to FantumBudget Entra ID app registration and assigns current user to Admin role.

.DESCRIPTION
    This script:
    1. Checks if app roles exist in the FantumBudget app registration
    2. Creates Admin, PowerUser, and User roles if they don't exist
    3. Assigns the current user to the Admin role
    4. Provides instructions for assigning other users

.PARAMETER AppName
    The display name of the app registration (default: "FantumBudget")

.EXAMPLE
    .\Add-EntraAppRoles.ps1
    Adds roles to FantumBudget app and assigns current user to Admin

.EXAMPLE
    .\Add-EntraAppRoles.ps1 -AppName "MyCustomApp"
    Adds roles to a custom app registration
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$AppName = "FantumBudget"
)

$ErrorActionPreference = "Stop"

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Entra ID App Roles Setup" -ForegroundColor Cyan
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

# Define app roles
$roles = @(
    @{
        displayName = "Admin"
        description = "Full access to all features and settings"
        value = "Admin"
        isEnabled = $true
        allowedMemberTypes = @("User")
    },
    @{
        displayName = "PowerUser"
        description = "Elevated access to advanced features"
        value = "PowerUser"
        isEnabled = $true
        allowedMemberTypes = @("User")
    },
    @{
        displayName = "User"
        description = "Standard user access"
        value = "User"
        isEnabled = $true
        allowedMemberTypes = @("User")
    }
)

# Check existing roles
$existingRoles = $app.appRoles
$rolesToAdd = @()

Write-Host "Checking existing app roles..." -ForegroundColor Yellow
foreach ($role in $roles) {
    $exists = $existingRoles | Where-Object { $_.value -eq $role.value }
    if ($exists) {
        Write-Host "  ? Role '$($role.displayName)' already exists" -ForegroundColor Green
    } else {
        Write-Host "  ? Role '$($role.displayName)' needs to be created" -ForegroundColor Yellow
        $role.id = [guid]::NewGuid().ToString()
        $rolesToAdd += $role
    }
}
Write-Host ""

# Add missing roles
if ($rolesToAdd.Count -gt 0) {
    Write-Host "Adding missing roles to app registration..." -ForegroundColor Yellow
    
    # Combine existing and new roles
    $allRoles = @()
    $allRoles += $existingRoles
    $allRoles += $rolesToAdd
    
    # Update app registration
    $rolesJson = $allRoles | ConvertTo-Json -Depth 10 -Compress
    az ad app update --id $app.appId --app-roles $rolesJson
    
    Write-Host "? App roles updated successfully" -ForegroundColor Green
    Write-Host ""
    
    # Wait for changes to propagate
    Write-Host "Waiting 10 seconds for changes to propagate..." -ForegroundColor Yellow
    Start-Sleep -Seconds 10
}

# Get service principal (enterprise app)
Write-Host "Finding enterprise application..." -ForegroundColor Yellow
$spJson = az ad sp list --filter "appId eq '$($app.appId)'" | ConvertFrom-Json
if ($spJson.Count -eq 0) {
    Write-Host "Enterprise application not found. Creating it..." -ForegroundColor Yellow
    az ad sp create --id $app.appId
    Start-Sleep -Seconds 5
    $spJson = az ad sp list --filter "appId eq '$($app.appId)'" | ConvertFrom-Json
}

$sp = $spJson[0]
Write-Host "? Found enterprise app (Service Principal Id: $($sp.id))" -ForegroundColor Green
Write-Host ""

# Get current user
Write-Host "Getting current user information..." -ForegroundColor Yellow
$currentUser = az ad signed-in-user show | ConvertFrom-Json
Write-Host "? Current user: $($currentUser.userPrincipalName)" -ForegroundColor Green
Write-Host ""

# Get Admin role ID from service principal
$adminRole = $sp.appRoles | Where-Object { $_.value -eq "Admin" }
if (-not $adminRole) {
    Write-Error "Admin role not found in service principal. Please try running the script again."
    exit 1
}

# Check if already assigned
Write-Host "Checking existing role assignments..." -ForegroundColor Yellow
$existingAssignments = az rest --method GET `
    --url "https://graph.microsoft.com/v1.0/servicePrincipals/$($sp.id)/appRoleAssignedTo" `
    | ConvertFrom-Json

$hasAdminRole = $existingAssignments.value | Where-Object { 
    $_.principalId -eq $currentUser.id -and $_.appRoleId -eq $adminRole.id 
}

if ($hasAdminRole) {
    Write-Host "? You already have the Admin role assigned" -ForegroundColor Green
} else {
    Write-Host "Assigning Admin role to you..." -ForegroundColor Yellow
    
    $body = @{
        principalId = $currentUser.id
        resourceId = $sp.id
        appRoleId = $adminRole.id
    } | ConvertTo-Json
    
    az rest --method POST `
        --url "https://graph.microsoft.com/v1.0/servicePrincipals/$($sp.id)/appRoleAssignedTo" `
        --body $body `
        --headers "Content-Type=application/json"
    
    Write-Host "? Admin role assigned successfully!" -ForegroundColor Green
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Setup Complete!" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "IMPORTANT: You must LOG OUT and LOG BACK IN to your application" -ForegroundColor Yellow
Write-Host "for the role changes to take effect in your authentication token." -ForegroundColor Yellow
Write-Host ""
Write-Host "To assign roles to other users:" -ForegroundColor Cyan
Write-Host "1. Go to Azure Portal: https://portal.azure.com" -ForegroundColor White
Write-Host "2. Navigate to: Enterprise applications ? $AppName ? Users and groups" -ForegroundColor White
Write-Host "3. Click 'Add user/group' and select users and their roles" -ForegroundColor White
Write-Host ""
Write-Host "App Details:" -ForegroundColor Cyan
Write-Host "  App Name: $AppName" -ForegroundColor White
Write-Host "  App ID: $($app.appId)" -ForegroundColor White
Write-Host "  Service Principal ID: $($sp.id)" -ForegroundColor White
Write-Host "  Your Email: $($currentUser.userPrincipalName)" -ForegroundColor White
Write-Host "  Your Role: Admin" -ForegroundColor White
Write-Host ""
