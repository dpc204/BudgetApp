<#
.SYNOPSIS
    Adds a redirect URI to an existing Microsoft Entra ID App Registration.

.DESCRIPTION
    This script adds a new redirect URI to the FantumBudget Entra ID app registration.
    Use this after deploying to Azure Container Apps to register the production URL.
    
    The script will:
    - Connect to Microsoft Graph
    - Find the FantumBudget app registration
    - Add the specified redirect URI (if not already present)
    - Optionally add the signout callback URI
    
.PARAMETER RedirectUri
    The redirect URI to add (e.g., "https://budget.delightfulbush-2a4d6a17.eastus.azurecontainerapps.io/signin-oidc")

.PARAMETER AppName
    The name of the app registration. Defaults to "FantumBudget".

.PARAMETER TenantId
    The Azure AD Tenant ID. If not provided, will use the default tenant.

.PARAMETER SkipBrowserAuth
    Skip opening browser for authentication (useful for automated scenarios).

.EXAMPLE
    .\Add-RedirectUri.ps1 -RedirectUri "https://budget.delightfulbush-2a4d6a17.eastus.azurecontainerapps.io/signin-oidc"
    Adds the specified redirect URI to the FantumBudget app registration.

.EXAMPLE
    .\Add-RedirectUri.ps1 -RedirectUri "https://myapp.azurecontainerapps.io/signin-oidc" -AppName "MyCustomApp"
    Adds the redirect URI to a custom named app registration.

.NOTES
    Author: FantumBudget Team
    Requires: Microsoft Graph PowerShell module with Application.ReadWrite.All permission
    
    After deployment to Azure Container Apps, get your app URL and run:
    .\Add-RedirectUri.ps1 -RedirectUri "https://your-app-url.azurecontainerapps.io/signin-oidc"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, HelpMessage = "The redirect URI to add (e.g., https://myapp.azurecontainerapps.io/signin-oidc)")]
    [ValidatePattern('^https?://.*')]
    [string]$RedirectUri,
    
    [Parameter(Mandatory = $false)]
    [string]$AppName = "FantumBudget",
    
    [Parameter(Mandatory = $false)]
    [string]$TenantId,
    
    [Parameter(Mandatory = $false)]
    [switch]$SkipBrowserAuth
)

$ErrorActionPreference = "Stop"

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
    Write-Host "`n═══════════════════════════════════════════════════════════════" -ForegroundColor Magenta
    Write-Host "  $Title" -ForegroundColor Magenta
    Write-Host "═══════════════════════════════════════════════════════════════`n" -ForegroundColor Magenta
}

# Validate redirect URI format
if ($RedirectUri -notmatch '/signin-oidc$') {
    Write-Status "Warning: Redirect URI should typically end with '/signin-oidc'" "Warning"
    $continue = Read-Host "Do you want to continue? (Y/N)"
    if ($continue -ne 'Y' -and $continue -ne 'y') {
        Write-Status "Operation cancelled by user" "Info"
        exit 0
    }
}

# Check prerequisites
Write-SectionHeader "Checking Prerequisites"

# Check if Microsoft Graph module is installed
Write-Status "Checking Microsoft Graph PowerShell module..." "Info"
$mgModule = Get-Module -ListAvailable -Name Microsoft.Graph.Applications

if (-not $mgModule) {
    Write-Status "Microsoft Graph Applications module not found. Installing..." "Warning"
    try {
        Install-Module Microsoft.Graph.Applications -Scope CurrentUser -Force -AllowClobber
        Write-Status "Successfully installed Microsoft Graph Applications module" "Success"
    }
    catch {
        Write-Status "Failed to install Microsoft Graph Applications module: $_" "Error"
        Write-Host "Please install manually: Install-Module Microsoft.Graph.Applications -Scope CurrentUser"
        exit 1
    }
}
else {
    Write-Status "Microsoft Graph Applications module is installed (version $($mgModule.Version))" "Success"
}

# Connect to Microsoft Graph
Write-SectionHeader "Connecting to Microsoft Graph"

try {
    Import-Module Microsoft.Graph.Applications
    
    $connectParams = @{
        Scopes = @("Application.ReadWrite.All")
    }
    
    if ($TenantId) {
        $connectParams.TenantId = $TenantId
    }
    
    if ($SkipBrowserAuth) {
        $connectParams.UseDeviceAuthentication = $true
    }
    
    Write-Status "Connecting to Microsoft Graph..." "Info"
    Connect-MgGraph @connectParams
    
    $context = Get-MgContext
    Write-Status "Successfully connected to Microsoft Graph" "Success"
    Write-Host "   Tenant: $($context.TenantId)" -ForegroundColor Gray
    Write-Host "   Account: $($context.Account)" -ForegroundColor Gray
}
catch {
    Write-Status "Failed to connect to Microsoft Graph: $_" "Error"
    exit 1
}

# Find the app registration
Write-SectionHeader "Finding App Registration"

Write-Status "Searching for app registration: $AppName" "Info"
try {
    $app = Get-MgApplication -Filter "displayName eq '$AppName'" -ErrorAction Stop
    
    if (-not $app) {
        Write-Status "App registration '$AppName' not found" "Error"
        Write-Host "Please ensure the app registration exists or specify the correct name with -AppName parameter"
        Disconnect-MgGraph | Out-Null
        exit 1
    }
    
    if ($app -is [array] -and $app.Count -gt 1) {
        Write-Status "Multiple app registrations found with name '$AppName'" "Error"
        Write-Host "Please ensure you have a unique app registration name"
        Disconnect-MgGraph | Out-Null
        exit 1
    }
    
    # Handle array result
    if ($app -is [array]) {
        $app = $app[0]
    }
    
    Write-Status "Found app registration: $AppName" "Success"
    Write-Host "   Application ID: $($app.AppId)" -ForegroundColor Gray
    Write-Host "   Object ID: $($app.Id)" -ForegroundColor Gray
}
catch {
    Write-Status "Failed to find app registration: $_" "Error"
    Disconnect-MgGraph | Out-Null
    exit 1
}

# Get current redirect URIs
Write-SectionHeader "Checking Current Redirect URIs"

$currentRedirectUris = @()
if ($app.Web -and $app.Web.RedirectUris) {
    $currentRedirectUris = $app.Web.RedirectUris
}

Write-Status "Current redirect URIs:" "Info"
if ($currentRedirectUris.Count -eq 0) {
    Write-Host "   (none)" -ForegroundColor Gray
}
else {
    foreach ($uri in $currentRedirectUris) {
        Write-Host "   $uri" -ForegroundColor Gray
    }
}

# Check if URI already exists
if ($currentRedirectUris -contains $RedirectUri) {
    Write-Status "Redirect URI already exists in app registration" "Warning"
    Write-Host "No changes needed."
    Disconnect-MgGraph | Out-Null
    exit 0
}

# Add the new redirect URI
Write-SectionHeader "Adding Redirect URI"

Write-Status "Adding redirect URI: $RedirectUri" "Info"

try {
    # Create new array with existing + new URI
    $newRedirectUris = [System.Collections.ArrayList]::new($currentRedirectUris)
    $newRedirectUris.Add($RedirectUri) | Out-Null
    
    # Calculate signout callback URI
    $baseUrl = $RedirectUri -replace '/signin-oidc$', ''
    $signoutUri = "$baseUrl/signout-callback-oidc"
    
    # Add signout URI to redirect URIs if not present
    if ($currentRedirectUris -notcontains $signoutUri) {
        Write-Status "Also adding signout callback URI: $signoutUri" "Info"
        $newRedirectUris.Add($signoutUri) | Out-Null
    }
    
    # Preserve existing Web configuration
    $webConfig = @{
        RedirectUris = $newRedirectUris.ToArray()
    }
    
    # Preserve ImplicitGrantSettings if they exist
    if ($app.Web.ImplicitGrantSettings) {
        $webConfig.ImplicitGrantSettings = @{
            EnableIdTokenIssuance = $app.Web.ImplicitGrantSettings.EnableIdTokenIssuance
            EnableAccessTokenIssuance = $app.Web.ImplicitGrantSettings.EnableAccessTokenIssuance
        }
    }
    
    # Update the app registration
    $updateParams = @{
        ApplicationId = $app.Id
        Web = $webConfig
    }
    
    Update-MgApplication @updateParams
    Write-Status "Successfully added redirect URI(s)" "Success"
}
catch {
    Write-Status "Failed to add redirect URI: $_" "Error"
    Disconnect-MgGraph | Out-Null
    exit 1
}

# Verify the update
Write-SectionHeader "Verification"

try {
    $updatedApp = Get-MgApplication -ApplicationId $app.Id
    
    Write-Status "Updated redirect URIs:" "Success"
    foreach ($uri in $updatedApp.Web.RedirectUris) {
        # Only mark as new if signout URI was actually calculated
        $isNewUri = $uri -eq $RedirectUri
        if ($RedirectUri -match '/signin-oidc$') {
            $isNewUri = $isNewUri -or ($uri -eq $signoutUri)
        }
        $marker = if ($isNewUri) { "[NEW] " } else { "      " }
        Write-Host "$marker$uri" -ForegroundColor $(if ($marker -eq "[NEW] ") { "Green" } else { "Gray" })
    }
}
catch {
    Write-Status "Failed to verify update: $_" "Warning"
}

# Disconnect
Disconnect-MgGraph | Out-Null

Write-SectionHeader "Summary"
Write-Status "Redirect URI successfully added to app registration!" "Success"
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. [DONE] Redirect URI has been added to the Entra ID app registration" -ForegroundColor Green
Write-Host "2. [NEXT] Users can now sign in at: $RedirectUri" -ForegroundColor Cyan
Write-Host "3. [WAIT] Allow a few minutes for the changes to propagate" -ForegroundColor Yellow
Write-Host "4. [TEST] Test the authentication flow in your application" -ForegroundColor Cyan
Write-Host ""
