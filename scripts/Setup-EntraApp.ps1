<#
.SYNOPSIS
    Automates the creation and configuration of Microsoft Entra ID (Azure AD) App Registration for FantumBudget.

.DESCRIPTION
    This script creates and configures an Entra App Registration with the following features:
    - App registration named "FantumBudget"
    - Redirect URIs for development and production
    - ID tokens and Access tokens enabled
    - Microsoft Graph API permissions (User.Read, email, openid, profile)
    - Client secret creation
    - Three app roles: Admin, PowerUser, and User
    
.PARAMETER TenantId
    The Azure AD Tenant ID. If not provided, will use the default tenant.

.PARAMETER EnvironmentName
    The Azure Container Apps environment name for production redirect URI (optional).

.PARAMETER SaveToKeyVault
    Switch to save the client secret to Azure Key Vault.

.PARAMETER KeyVaultName
    The name of the Key Vault to save the client secret to (required if SaveToKeyVault is specified).

.PARAMETER SkipBrowserAuth
    Skip opening browser for authentication (useful for automated scenarios).

.EXAMPLE
    .\Setup-EntraApp.ps1
    Creates the app registration with interactive authentication.

.EXAMPLE
    .\Setup-EntraApp.ps1 -EnvironmentName "fantumbudget-prod" -SaveToKeyVault -KeyVaultName "fantumbudget-kv"
    Creates the app registration and saves the secret to Key Vault.

.NOTES
    Author: FantumBudget Team
    Requires: Azure CLI and Microsoft Graph PowerShell modules
    Permissions: Application.ReadWrite.All, AppRoleAssignment.ReadWrite.All
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$TenantId,
    
    [Parameter(Mandatory = $false)]
    [string]$EnvironmentName,
    
    [Parameter(Mandatory = $false)]
    [switch]$SaveToKeyVault,
    
    [Parameter(Mandatory = $false)]
    [string]$KeyVaultName,
    
    [Parameter(Mandatory = $false)]
    [switch]$SkipBrowserAuth
)

# Script configuration
$ErrorActionPreference = "Stop"
$AppName = "FantumBudget"
$DevRedirectUri = "https://localhost:7141/signin-oidc"
$DevSpaCallbackUri = "https://localhost:7141/authentication/login-callback"

# Function to write colored output
function Write-Status {
    param(
        [string]$Message,
        [string]$Type = "Info"
    )
    
    switch ($Type) {
        "Success" { Write-Host "[SUCCESS] $Message" -ForegroundColor Green }
        "Error" { Write-Host "[ERROR] $Message" -ForegroundColor Red }
        "Warning" { Write-Host "[WARNING] $Message" -ForegroundColor Yellow }
        "Info" { Write-Host "[INFO] $Message" -ForegroundColor Cyan }
        default { Write-Host "$Message" }
    }
}

function Write-SectionHeader {
    param([string]$Title)
    Write-Host "`n================================================================" -ForegroundColor Magenta
    Write-Host "  $Title" -ForegroundColor Magenta
    Write-Host "================================================================`n" -ForegroundColor Magenta
}

# Check prerequisites
Write-SectionHeader "Checking Prerequisites"

# Check if Azure CLI is installed
Write-Status "Checking Azure CLI installation..." "Info"
try {
    $azVersion = az version --query '\"azure-cli\"' -o tsv 2>$null
    if ($azVersion) {
        Write-Status "Azure CLI version $azVersion is installed" "Success"
    }
}
catch {
    Write-Status "Azure CLI is not installed. Please install from https://aka.ms/installazurecli" "Error"
    exit 1
}

# Check if Microsoft Graph PowerShell module is installed
Write-Status "Checking Microsoft Graph PowerShell module..." "Info"
$mgGraphModule = Get-Module -ListAvailable -Name Microsoft.Graph.Applications
if (-not $mgGraphModule) {
    Write-Status "Microsoft Graph PowerShell module not found. Installing..." "Warning"
    try {
        Install-Module -Name Microsoft.Graph.Applications -Scope CurrentUser -Force -AllowClobber
        Write-Status "Microsoft Graph PowerShell module installed successfully" "Success"
    }
    catch {
        Write-Status "Failed to install Microsoft Graph PowerShell module: $_" "Error"
        exit 1
    }
}
else {
    Write-Status "Microsoft Graph PowerShell module is installed" "Success"
}

# Validate parameters
if ($SaveToKeyVault -and [string]::IsNullOrWhiteSpace($KeyVaultName)) {
    Write-Status "KeyVaultName is required when SaveToKeyVault is specified" "Error"
    exit 1
}

# Authenticate to Azure
Write-SectionHeader "Azure Authentication"

Write-Status "Authenticating to Azure..." "Info"
try {
    if ($SkipBrowserAuth) {
        $loginResult = az login --output none 2>&1
    }
    else {
        $loginResult = az login --output none --use-device-code 2>&1
    }
    
    if ($LASTEXITCODE -eq 0) {
        Write-Status "Successfully authenticated to Azure" "Success"
    }
    else {
        throw "Authentication failed"
    }
}
catch {
    Write-Status "Failed to authenticate to Azure: $_" "Error"
    exit 1
}

# Get current tenant
if ([string]::IsNullOrWhiteSpace($TenantId)) {
    $TenantId = az account show --query tenantId -o tsv
    Write-Status "Using tenant: $TenantId" "Info"
}

# Connect to Microsoft Graph
Write-Status "Connecting to Microsoft Graph..." "Info"
try {
    Connect-MgGraph -TenantId $TenantId -Scopes "Application.ReadWrite.All", "AppRoleAssignment.ReadWrite.All" -NoWelcome
    Write-Status "Successfully connected to Microsoft Graph" "Success"
}
catch {
    Write-Status "Failed to connect to Microsoft Graph: $_" "Error"
    exit 1
}

# Check for existing app registration
Write-SectionHeader "Checking for Existing App Registration"

$existingApp = $null
try {
    # Try to get applications with a reasonable limit first
    # This works with lower permissions than -All
    $allApps = Get-MgApplication -Top 500 -ErrorAction SilentlyContinue
    if ($allApps) {
        $existingApp = $allApps | Where-Object { $_.DisplayName -eq $AppName } | Select-Object -First 1
        
        # If not found and we got exactly 500, try getting all
        if (-not $existingApp -and $allApps.Count -eq 500) {
            $allApps = Get-MgApplication -All -ErrorAction SilentlyContinue
            $existingApp = $allApps | Where-Object { $_.DisplayName -eq $AppName } | Select-Object -First 1
        }
    }
    
    if ($existingApp) {
        Write-Status "Found existing app registration: $($existingApp.DisplayName) (App ID: $($existingApp.AppId))" "Warning"
        $response = Read-Host "Do you want to update the existing app? (y/n)"
        if ($response -ne 'y') {
            Write-Status "Operation cancelled by user" "Info"
            Disconnect-MgGraph | Out-Null
            exit 0
        }
    }
    else {
        Write-Status "No existing app registration found. Creating new..." "Info"
    }
}
catch {
    # For this script, we'll just warn and continue - creation will work even if we can't check
    Write-Status "Could not check for existing app (may require higher permissions). Continuing with creation..." "Warning"
}

# Generate GUIDs for app roles
$adminRoleId = [Guid]::NewGuid().ToString()
$powerUserRoleId = [Guid]::NewGuid().ToString()
$userRoleId = [Guid]::NewGuid().ToString()

Write-Status "Generated role IDs:" "Info"
Write-Host "   Admin Role ID: $adminRoleId" -ForegroundColor Gray
Write-Host "   PowerUser Role ID: $powerUserRoleId" -ForegroundColor Gray
Write-Host "   User Role ID: $userRoleId" -ForegroundColor Gray

# Define app roles
$appRoles = @(
    @{
        AllowedMemberTypes = @("User")
        Description        = "Administrator role with full access to all features"
        DisplayName        = "Admin"
        Id                 = $adminRoleId
        IsEnabled          = $true
        Value              = "Admin"
    },
    @{
        AllowedMemberTypes = @("User")
        Description        = "Power User role with elevated access to advanced features"
        DisplayName        = "PowerUser"
        Id                 = $powerUserRoleId
        IsEnabled          = $true
        Value              = "PowerUser"
    },
    @{
        AllowedMemberTypes = @("User")
        Description        = "Standard user role with basic access"
        DisplayName        = "User"
        Id                 = $userRoleId
        IsEnabled          = $true
        Value              = "User"
    }
)

# Configure redirect URIs
$redirectUris = @($DevRedirectUri)
$spaRedirectUris = @($DevSpaCallbackUri)

if (-not [string]::IsNullOrWhiteSpace($EnvironmentName)) {
    $prodRedirectUri = "https://$EnvironmentName.azurecontainerapps.io/signin-oidc"
    $redirectUris += $prodRedirectUri
    Write-Status "Added production redirect URI: $prodRedirectUri" "Info"
}

# Define required resource access (Microsoft Graph permissions)
$graphServicePrincipalId = "00000003-0000-0000-c000-000000000000" # Microsoft Graph

$requiredResourceAccess = @{
    ResourceAppId  = $graphServicePrincipalId
    ResourceAccess = @(
        @{
            Id   = "e1fe6dd8-ba31-4d61-89e7-88639da4683d" # User.Read
            Type = "Scope"
        },
        @{
            Id   = "64a6cdd6-aab1-4aaf-94b8-3cc8405e90d0" # email
            Type = "Scope"
        },
        @{
            Id   = "37f7f235-527c-4136-accd-4a02d197296e" # openid
            Type = "Scope"
        },
        @{
            Id   = "14dad69e-099b-42c9-810b-d002981feec1" # profile
            Type = "Scope"
        }
    )
}

# Create or update app registration
Write-SectionHeader "Creating/Updating App Registration"

try {
    if ($existingApp) {
        # Update existing app
        Write-Status "Updating app registration..." "Info"
        
        $updateParams = @{
            ApplicationId          = $existingApp.Id
            Web                    = @{
                RedirectUris = $redirectUris
                ImplicitGrantSettings = @{
                    EnableIdTokenIssuance     = $true
                    EnableAccessTokenIssuance = $true
                }
            }
            Spa                    = @{
                RedirectUris = $spaRedirectUris
            }
            AppRoles               = $appRoles
            RequiredResourceAccess = @($requiredResourceAccess)
        }
        
        Update-MgApplication @updateParams
        $app = Get-MgApplication -ApplicationId $existingApp.Id
        Write-Status "Successfully updated app registration" "Success"
    }
    else {
        # Create new app
        Write-Status "Creating new app registration..." "Info"
        
        $appParams = @{
            DisplayName            = $AppName
            SignInAudience         = "AzureADMyOrg"
            Web                    = @{
                RedirectUris = $redirectUris
                ImplicitGrantSettings = @{
                    EnableIdTokenIssuance     = $true
                    EnableAccessTokenIssuance = $true
                }
            }
            Spa                    = @{
                RedirectUris = $spaRedirectUris
            }
            AppRoles               = $appRoles
            RequiredResourceAccess = @($requiredResourceAccess)
        }
        
        $app = New-MgApplication @appParams
        Write-Status "Successfully created app registration" "Success"
    }
}
catch {
    Write-Status "Failed to create/update app registration: $_" "Error"
    Disconnect-MgGraph | Out-Null
    exit 1
}

# Create client secret
Write-SectionHeader "Creating Client Secret"

Write-Status "Creating client secret..." "Info"
try {
    $passwordCredential = @{
        DisplayName = "FantumBudget Secret (Created $(Get-Date -Format 'yyyy-MM-dd'))"
        EndDateTime = (Get-Date).AddYears(2)
    }
    
    $secret = Add-MgApplicationPassword -ApplicationId $app.Id -PasswordCredential $passwordCredential
    Write-Status "Successfully created client secret (expires: $($secret.EndDateTime.ToString('yyyy-MM-dd')))" "Success"
}
catch {
    Write-Status "Failed to create client secret: $_" "Error"
    Disconnect-MgGraph | Out-Null
    exit 1
}

# Save to Key Vault if requested
if ($SaveToKeyVault) {
    Write-SectionHeader "Saving to Azure Key Vault"
    
    Write-Status "Saving client secret to Key Vault: $KeyVaultName" "Info"
    try {
        az keyvault secret set --vault-name $KeyVaultName --name "EntraClientSecret" --value $secret.SecretText --output none
        Write-Status "Successfully saved client secret to Key Vault" "Success"
    }
    catch {
        Write-Status "Failed to save to Key Vault: $_" "Warning"
        Write-Status "You will need to manually store the client secret shown below" "Warning"
    }
}

# Output configuration
Write-SectionHeader "Configuration Summary"

Write-Host ""
Write-Host "================================================================================" -ForegroundColor Green
Write-Host "                    ENTRA APP REGISTRATION COMPLETE                           " -ForegroundColor Green
Write-Host "================================================================================" -ForegroundColor Green
Write-Host ""

Write-Host "Application Details:" -ForegroundColor Cyan
Write-Host "   App Name:      $AppName" -ForegroundColor White
Write-Host "   App ID:        $($app.AppId)" -ForegroundColor Yellow
Write-Host "   Object ID:     $($app.Id)" -ForegroundColor White
Write-Host "   Tenant ID:     $TenantId" -ForegroundColor Yellow
Write-Host ""

Write-Host "Client Secret:" -ForegroundColor Cyan
Write-Host "   Secret Value:  $($secret.SecretText)" -ForegroundColor Yellow
Write-Host "   Expires:       $($secret.EndDateTime.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor White
Write-Host ""
Write-Host "   [WARNING] Save this secret securely! It will not be shown again." -ForegroundColor Red
Write-Host ""

Write-Host "Redirect URIs:" -ForegroundColor Cyan
foreach ($uri in $redirectUris) {
    Write-Host "   Web: $uri" -ForegroundColor White
}
foreach ($uri in $spaRedirectUris) {
    Write-Host "   SPA: $uri" -ForegroundColor White
}
Write-Host ""

Write-Host "App Roles:" -ForegroundColor Cyan
Write-Host "   Admin      (ID: $adminRoleId)" -ForegroundColor White
Write-Host "   PowerUser  (ID: $powerUserRoleId)" -ForegroundColor White
Write-Host "   User       (ID: $userRoleId)" -ForegroundColor White
Write-Host ""

Write-Host "API Permissions:" -ForegroundColor Cyan
Write-Host "   Microsoft Graph:" -ForegroundColor White
Write-Host "   - User.Read" -ForegroundColor White
Write-Host "   - email" -ForegroundColor White
Write-Host "   - openid" -ForegroundColor White
Write-Host "   - profile" -ForegroundColor White
Write-Host ""

Write-Host "Configuration for appsettings.json:" -ForegroundColor Cyan
Write-Host ""
Write-Host @"
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "<your-domain>.onmicrosoft.com",
    "TenantId": "$TenantId",
    "ClientId": "$($app.AppId)",
    "ClientSecret": "$($secret.SecretText)",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  }
}
"@ -ForegroundColor Yellow

Write-Host ""
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "   1. Copy the configuration above to your appsettings.json files" -ForegroundColor White
Write-Host "   2. Store the client secret securely (Key Vault recommended)" -ForegroundColor White
Write-Host "   3. Assign users to roles in the Enterprise Application:" -ForegroundColor White
Write-Host "      https://portal.azure.com/#view/Microsoft_AAD_IAM/ManagedAppMenuBlade/~/Users/objectId/$($app.Id)/appId/$($app.AppId)" -ForegroundColor Gray
Write-Host "   4. Grant admin consent for API permissions:" -ForegroundColor White
Write-Host "      https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/~/CallAnAPI/appId/$($app.AppId)" -ForegroundColor Gray
Write-Host "   5. Review the Phase1-EntraID-Setup.md documentation for implementation details" -ForegroundColor White
Write-Host ""

# Disconnect from Graph
Disconnect-MgGraph | Out-Null
Write-Status "Script completed successfully!" "Success"
