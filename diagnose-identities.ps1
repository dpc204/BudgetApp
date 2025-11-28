# Diagnostic script to check Container App identities
param(
    [Parameter(Mandatory=$false)]
    [string]$ResourceGroupName = "rg-BudgetApp2"
)

Write-Host "=== Container App Identity Diagnostic ===" -ForegroundColor Cyan
Write-Host ""

$containerApps = az containerapp list --resource-group $ResourceGroupName --output json | ConvertFrom-Json

foreach ($app in $containerApps) {
    Write-Host "App: $($app.name)" -ForegroundColor Yellow
    Write-Host "Identity Type: $($app.identity.type)" -ForegroundColor White
    Write-Host ""
    
    # Show full identity object
    Write-Host "Full Identity Object:" -ForegroundColor Cyan
    $app.identity | ConvertTo-Json -Depth 5 | Write-Host
    Write-Host ""
    
    # Try to get user-assigned identities
    if ($app.identity.userAssignedIdentities) {
        Write-Host "User-Assigned Identities:" -ForegroundColor Cyan
        $app.identity.userAssignedIdentities | ConvertTo-Json -Depth 5 | Write-Host
        Write-Host ""
    }
    
    Write-Host "---" -ForegroundColor Gray
    Write-Host ""
}
