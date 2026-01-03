# Update-EntraRedirectUris.ps1
# Updates Entra ID App Registration with current Azure Container Apps URLs

param(
    [Parameter(Mandatory=$true)]
    [string]$Environment = "rg-BudgetApp2",
    
    [string]$AppId = "36ca674b-1c79-49ad-98fb-b90f13d72887"
)

# Check if Microsoft.Graph.Applications module is installed
Write-Host "🔍 Checking for Microsoft Graph PowerShell module..." -ForegroundColor Cyan
$module = Get-Module -ListAvailable -Name Microsoft.Graph.Applications

if (-not $module) {
    Write-Host "⚠️  Microsoft Graph PowerShell module not found." -ForegroundColor Yellow
    Write-Host "Installing Microsoft.Graph.Applications module..." -ForegroundColor Cyan
    
    try {
        Install-Module -Name Microsoft.Graph.Applications -Scope CurrentUser -Force -AllowClobber
        Write-Host "✅ Module installed successfully!" -ForegroundColor Green
    }
    catch {
        Write-Error "Failed to install Microsoft.Graph.Applications module. Please run: Install-Module -Name Microsoft.Graph.Applications -Scope CurrentUser"
        exit 1
    }
}
else {
    Write-Host "✅ Microsoft Graph module found" -ForegroundColor Green
}

Write-Host "🔍 Getting Container App URL..." -ForegroundColor Cyan

# Get the resource group name
$resourceGroup = "rg-$Environment"

# Get the Container App FQDN
$fqdn = az containerapp show `
    --name budget `
    --resource-group $resourceGroup `
    --query "properties.configuration.ingress.fqdn" `
    --output tsv

if ([string]::IsNullOrEmpty($fqdn)) {
    Write-Error "Failed to get Container App FQDN. Is the app deployed?"
    exit 1
}

$containerAppUrl = "https://$fqdn"
Write-Host "✅ Found URL: $containerAppUrl" -ForegroundColor Green

# Connect to Microsoft Graph
Write-Host "🔐 Connecting to Microsoft Graph..." -ForegroundColor Cyan
Connect-MgGraph -Scopes "Application.ReadWrite.All" -NoWelcome

# Get the application
Write-Host "📱 Getting app registration..." -ForegroundColor Cyan
$app = Get-MgApplication -Filter "appId eq '$AppId'"

if ($null -eq $app) {
    Write-Error "Application with ID $AppId not found"
    Disconnect-MgGraph
    exit 1
}

# Get existing redirect URIs
$existingUris = $app.Web.RedirectUris

# Define new URIs
$newUris = @(
    "$containerAppUrl/signin-oidc",
    "$containerAppUrl/signout-callback-oidc"
)

# Check if URIs already exist
$alreadyExists = $true
foreach ($uri in $newUris) {
    if ($existingUris -notcontains $uri) {
        $alreadyExists = $false
        break
    }
}

if ($alreadyExists) {
    Write-Host "✅ Redirect URIs already configured!" -ForegroundColor Green
    Write-Host "No changes needed." -ForegroundColor Gray
    Disconnect-MgGraph
    exit 0
}

# Combine existing and new URIs (remove duplicates)
$allUris = ($existingUris + $newUris) | Select-Object -Unique

# Update the application
Write-Host "🔄 Updating app registration..." -ForegroundColor Cyan
Update-MgApplication -ApplicationId $app.Id -Web @{
    RedirectUris = $allUris
    LogoutUrl = "$containerAppUrl/signout-callback-oidc"
}

Write-Host "`n✅ Redirect URIs updated successfully!" -ForegroundColor Green
Write-Host "`nAdded the following URIs:" -ForegroundColor Cyan
$newUris | ForEach-Object { Write-Host "  • $_" -ForegroundColor White }

Write-Host "`nAll configured URIs:" -ForegroundColor Cyan
$allUris | ForEach-Object { Write-Host "  • $_" -ForegroundColor Gray }

# Disconnect
Disconnect-MgGraph

Write-Host "`n🎉 Done! You can now log in at: $containerAppUrl" -ForegroundColor Green
