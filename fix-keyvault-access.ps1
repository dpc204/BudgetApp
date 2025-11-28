# Fix Azure Key Vault Access for Container Apps
# This script grants your Container Apps access to the Key Vault

param(
    [Parameter(Mandatory=$false)]
    [string]$ResourceGroupName = "rg-BudgetApp2",
    
    [Parameter(Mandatory=$false)]
    [string]$KeyVaultName = "fantumkeyvault",
    
    [Parameter(Mandatory=$false)]
    [string]$KeyVaultResourceGroup = "rg-fantum"
)

Write-Host "=== Azure Key Vault Access Setup ===" -ForegroundColor Cyan
Write-Host ""

# Check if logged in
$account = az account show 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Not logged in to Azure. Please run 'az login' first." -ForegroundColor Red
    exit 1
}

# Get Container Apps
Write-Host "Finding Container Apps in resource group: $ResourceGroupName" -ForegroundColor Yellow
$containerApps = az containerapp list --resource-group $ResourceGroupName --output json | ConvertFrom-Json

if ($containerApps.Count -eq 0) {
    Write-Host "ERROR: No Container Apps found in resource group $ResourceGroupName" -ForegroundColor Red
    exit 1
}

Write-Host "? Found $($containerApps.Count) Container Apps" -ForegroundColor Green
Write-Host ""

# For each Container App, get its managed identity and grant Key Vault access
foreach ($app in $containerApps) {
    Write-Host "Processing: $($app.name)" -ForegroundColor Cyan
    
    # Check identity type
    $identityType = $app.identity.type
    
    if (!$identityType -or $identityType -eq "None") {
        Write-Host "  ? No managed identity enabled - enabling system-assigned identity..." -ForegroundColor Yellow
        
        az containerapp identity assign `
            --name $app.name `
            --resource-group $ResourceGroupName `
            --system-assigned `
            --output none
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ? System-assigned identity enabled" -ForegroundColor Green
            
            # Refresh app data to get the new identity
            $app = az containerapp show --name $app.name --resource-group $ResourceGroupName --output json | ConvertFrom-Json
            $principalId = $app.identity.principalId
        } else {
            Write-Host "  ? Failed to enable managed identity" -ForegroundColor Red
            continue
        }
    }
    else {
        Write-Host "  ? Managed identity type: $identityType" -ForegroundColor Green
        
        # Handle different identity types
        if ($identityType -eq "SystemAssigned" -or $identityType -eq "SystemAssigned, UserAssigned") {
            $principalId = $app.identity.principalId
            Write-Host "  ? Using System-Assigned Identity" -ForegroundColor Green
        }
        elseif ($identityType -eq "UserAssigned" -or $identityType -eq "SystemAssigned, UserAssigned") {
            # User-assigned identity - get the first one
            $userAssignedIdentities = $app.identity.userAssignedIdentities | Get-Member -MemberType NoteProperty | Select-Object -ExpandProperty Name
            
            if ($userAssignedIdentities) {
                $userIdentityId = $userAssignedIdentities[0]
                Write-Host "  ? Using User-Assigned Identity: $userIdentityId" -ForegroundColor Green
                
                # Get principal ID from the user-assigned identity
                $userIdentity = az identity show --ids $userIdentityId --output json | ConvertFrom-Json
                $principalId = $userIdentity.principalId
            }
            else {
                Write-Host "  ? No user-assigned identities found" -ForegroundColor Red
                continue
            }
        }
    }
    
    if (!$principalId) {
        Write-Host "  ? Could not get principal ID" -ForegroundColor Red
        continue
    }
    
    Write-Host "  Principal ID: $principalId" -ForegroundColor White
    
    # Grant Key Vault Secrets User role
    Write-Host "  Granting 'Key Vault Secrets User' role..." -ForegroundColor Yellow
    
    az role assignment create `
        --role "Key Vault Secrets User" `
        --assignee $principalId `
        --scope "/subscriptions/$(az account show --query id -o tsv)/resourceGroups/$KeyVaultResourceGroup/providers/Microsoft.KeyVault/vaults/$KeyVaultName" `
        --output none 2>$null
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ? Key Vault access granted" -ForegroundColor Green
    } else {
        # Try the older Access Policies method as fallback
        Write-Host "  ? RBAC failed, trying access policies..." -ForegroundColor Yellow
        
        az keyvault set-policy `
            --name $KeyVaultName `
            --resource-group $KeyVaultResourceGroup `
            --object-id $principalId `
            --secret-permissions get list `
            --output none
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ? Key Vault access granted via access policy" -ForegroundColor Green
        } else {
            Write-Host "  ? Failed to grant Key Vault access" -ForegroundColor Red
        }
    }
    
    Write-Host ""
}

Write-Host "=== Setup Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Wait 2-3 minutes for role assignments to propagate" -ForegroundColor White
Write-Host "2. Restart your Container Apps:" -ForegroundColor White
Write-Host "   az containerapp revision restart --revision <latest-revision-name> --name budget --resource-group $ResourceGroupName" -ForegroundColor Gray
Write-Host "   az containerapp revision restart --revision <latest-revision-name> --name budget-api --resource-group $ResourceGroupName" -ForegroundColor Gray
Write-Host "   Or simply: az containerapp update --name budget --resource-group $ResourceGroupName --set-env-vars RESTART=1" -ForegroundColor Gray
Write-Host "3. Check the logs to verify Key Vault access works" -ForegroundColor White
Write-Host ""
Write-Host "Verify with:" -ForegroundColor Yellow
Write-Host "  az containerapp logs show --name budget-api --resource-group $ResourceGroupName --follow" -ForegroundColor Gray
