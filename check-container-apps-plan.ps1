# Container Apps Plan Type Check and Modification Script
# Run this to check your current plan type and optionally change it

param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("Consumption", "D4", "D8", "D16", "D32", "E4", "E8", "E16", "E32")]
    [string]$NewPlanType,
    
    [Parameter(Mandatory=$false)]
    [int]$MinNodes = 1,
    
    [Parameter(Mandatory=$false)]
    [int]$MaxNodes = 3
)

Write-Host "=== Azure Container Apps Plan Type Manager ===" -ForegroundColor Cyan
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

# Get Container Apps Environment
Write-Host "Finding Container Apps Environment in resource group: $ResourceGroupName" -ForegroundColor Yellow
$envs = az containerapp env list --resource-group $ResourceGroupName --output json | ConvertFrom-Json

if ($envs.Count -eq 0) {
    Write-Host "ERROR: No Container Apps Environment found in resource group $ResourceGroupName" -ForegroundColor Red
    exit 1
}

$env = $envs[0]
$envName = $env.name

Write-Host "? Found Container Apps Environment: $envName" -ForegroundColor Green
Write-Host ""

# Show current workload profiles
Write-Host "Current Workload Profiles:" -ForegroundColor Cyan
$workloadProfiles = $env.properties.workloadProfiles

if ($workloadProfiles) {
    foreach ($profile in $workloadProfiles) {
        Write-Host "  - Name: $($profile.name)" -ForegroundColor White
        Write-Host "    Type: $($profile.workloadProfileType)" -ForegroundColor White
        if ($profile.minimumCount) {
            Write-Host "    Min Nodes: $($profile.minimumCount)" -ForegroundColor White
            Write-Host "    Max Nodes: $($profile.maximumCount)" -ForegroundColor White
        }
        Write-Host ""
    }
} else {
    Write-Host "  No workload profiles configured (using default Consumption)" -ForegroundColor Yellow
    Write-Host ""
}

# Show Container Apps and their assigned profiles
Write-Host "Container Apps in Environment:" -ForegroundColor Cyan
$containerApps = az containerapp list --resource-group $ResourceGroupName --output json | ConvertFrom-Json

foreach ($app in $containerApps) {
    $appProfile = $app.properties.workloadProfileName
    if (!$appProfile) { $appProfile = "Consumption (default)" }
    
    Write-Host "  - $($app.name): $appProfile" -ForegroundColor White
}
Write-Host ""

# If NewPlanType is specified, modify the environment
if ($NewPlanType) {
    Write-Host "=== Modifying Plan Type ===" -ForegroundColor Cyan
    Write-Host ""
    
    if ($NewPlanType -eq "Consumption") {
        Write-Host "Environment is already using Consumption plan (default)." -ForegroundColor Green
        Write-Host "No changes needed." -ForegroundColor Green
    } else {
        # Check if profile already exists
        $existingProfile = $workloadProfiles | Where-Object { $_.workloadProfileType -eq $NewPlanType }
        
        if ($existingProfile) {
            Write-Host "Workload profile '$NewPlanType' already exists." -ForegroundColor Yellow
            $profileName = $existingProfile.name
        } else {
            Write-Host "Adding new workload profile: $NewPlanType" -ForegroundColor Yellow
            $profileName = "dedicated-$($NewPlanType.ToLower())"
            
            az containerapp env workload-profile add `
                --name $envName `
                --resource-group $ResourceGroupName `
                --workload-profile-name $profileName `
                --workload-profile-type $NewPlanType `
                --min-nodes $MinNodes `
                --max-nodes $MaxNodes
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "? Workload profile added successfully" -ForegroundColor Green
            } else {
                Write-Host "? Failed to add workload profile" -ForegroundColor Red
                exit 1
            }
        }
        
        Write-Host ""
        Write-Host "Updating Container Apps to use profile: $profileName" -ForegroundColor Yellow
        
        foreach ($app in $containerApps) {
            Write-Host "  Updating $($app.name)..." -ForegroundColor White
            
            az containerapp update `
                --name $app.name `
                --resource-group $ResourceGroupName `
                --workload-profile-name $profileName `
                --output none
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "  ? $($app.name) updated" -ForegroundColor Green
            } else {
                Write-Host "  ? Failed to update $($app.name)" -ForegroundColor Red
            }
        }
        
        Write-Host ""
        Write-Host "=== Plan Type Change Complete ===" -ForegroundColor Green
        Write-Host ""
        Write-Host "?? Cost Impact:" -ForegroundColor Yellow
        
        switch ($NewPlanType) {
            "D4"  { Write-Host "  ~`$0.20/hour (~`$144/month) per node" -ForegroundColor White }
            "D8"  { Write-Host "  ~`$0.40/hour (~`$288/month) per node" -ForegroundColor White }
            "D16" { Write-Host "  ~`$0.80/hour (~`$576/month) per node" -ForegroundColor White }
            "D32" { Write-Host "  ~`$1.60/hour (~`$1,152/month) per node" -ForegroundColor White }
            "E4"  { Write-Host "  ~`$0.26/hour (~`$187/month) per node" -ForegroundColor White }
            "E8"  { Write-Host "  ~`$0.52/hour (~`$374/month) per node" -ForegroundColor White }
            "E16" { Write-Host "  ~`$1.04/hour (~`$749/month) per node" -ForegroundColor White }
            "E32" { Write-Host "  ~`$2.08/hour (~`$1,498/month) per node" -ForegroundColor White }
        }
        Write-Host "  With $MinNodes-$MaxNodes nodes configured" -ForegroundColor White
    }
}

Write-Host ""
Write-Host "=== Summary Complete ===" -ForegroundColor Cyan

# Usage examples
Write-Host ""
Write-Host "Examples:" -ForegroundColor Yellow
Write-Host "  # Check current plan:" -ForegroundColor Gray
Write-Host "  .\check-container-apps-plan.ps1 -ResourceGroupName rg-BudgetApp2" -ForegroundColor Gray
Write-Host ""
Write-Host "  # Switch to Dedicated D4 plan:" -ForegroundColor Gray
Write-Host "  .\check-container-apps-plan.ps1 -ResourceGroupName rg-BudgetApp2 -NewPlanType D4 -MinNodes 1 -MaxNodes 3" -ForegroundColor Gray
Write-Host ""
Write-Host "  # Revert to Consumption (just check - Consumption is always available):" -ForegroundColor Gray
Write-Host "  az containerapp update --name budget-api --resource-group rg-BudgetApp2 --workload-profile-name Consumption" -ForegroundColor Gray
