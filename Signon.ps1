param(
 [string]$TenantId = "d2b31d23-106e-4175-95dc-82ff027f9d9c",
 [string]$SubscriptionId = "3dd42e45-62af-4345-82d2-bffd522065f5",
 [string]$ResourceGroup = "rg-fantum",
 [string]$SqlServerName = "fantumsqlserver",
 [string]$FirewallRuleName = "AllowCurrentIP",
 [string[]]$StorageAccounts = @(), # optional; auto-discover if empty
 [string[]]$KeyVaults = @() # optional; auto-discover if empty
)

$ErrorActionPreference = "Stop"

function Get-PublicIPv4 {
 try { return (Invoke-RestMethod -Uri "https://api.ipify.org").Trim() } catch {}
 try { return (Invoke-RestMethod -Uri "https://ifconfig.me/ip").Trim() } catch {}
 throw "Unable to determine public IPv4 address."
}

function Ensure-AzCli() {
 if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
 throw "Azure CLI (az) is not installed or not on PATH. Install from https://aka.ms/azure-cli and retry."
 }
}

function Ensure-AzLogin([string]$TenantId,[string]$SubscriptionId) {
 Write-Host "Logging in to tenant $TenantId ..."
 az login --tenant $TenantId | Out-Null
 Write-Host "Setting subscription $SubscriptionId ..."
 az account set --subscription $SubscriptionId | Out-Null
}

function Throw-IfAzFailed($operation) {
 if ($LASTEXITCODE -ne0) { throw "Azure CLI failed during: $operation (exit $LASTEXITCODE)" }
}

Ensure-AzCli
Ensure-AzLogin -TenantId $TenantId -SubscriptionId $SubscriptionId

$ip = Get-PublicIPv4
Write-Host "Current public IPv4: $ip"

# --- Azure SQL Server firewall ---
# Check if the rule exists using list + filter to avoid error when not found
$existingName = az sql server firewall-rule list `
 --resource-group $ResourceGroup `
 --server $SqlServerName `
 --query "[?name=='$FirewallRuleName'] | [0].name" -o tsv
Throw-IfAzFailed "sql server firewall-rule list"
$ruleExists = -not [string]::IsNullOrWhiteSpace($existingName)

if ($ruleExists) {
 Write-Host "Updating SQL firewall rule '$FirewallRuleName' on server '$SqlServerName' to $ip ..."
 az sql server firewall-rule update `
 --resource-group $ResourceGroup `
 --server $SqlServerName `
 --name $FirewallRuleName `
 --start-ip-address $ip `
 --end-ip-address $ip | Out-Null
 if ($LASTEXITCODE -ne0) {
 Write-Warning "Update failed; attempting create instead ..."
 az sql server firewall-rule create `
 --resource-group $ResourceGroup `
 --server $SqlServerName `
 --name $FirewallRuleName `
 --start-ip-address $ip `
 --end-ip-address $ip | Out-Null
 Throw-IfAzFailed "sql server firewall-rule create (fallback)"
 }
} else {
 Write-Host "Creating SQL firewall rule '$FirewallRuleName' on server '$SqlServerName' for $ip ..."
 az sql server firewall-rule create `
 --resource-group $ResourceGroup `
 --server $SqlServerName `
 --name $FirewallRuleName `
 --start-ip-address $ip `
 --end-ip-address $ip | Out-Null
 Throw-IfAzFailed "sql server firewall-rule create"
}

# Verify the rule
try {
 Write-Host "Current firewall rule details:"
 az sql server firewall-rule show `
 --resource-group $ResourceGroup `
 --server $SqlServerName `
 --name $FirewallRuleName `
 --query "{name:name,start:startIpAddress,end:endIpAddress}" -o table
} catch {}

# --- Discover Storage Accounts/Key Vaults if not provided ---
if (-not $StorageAccounts -or $StorageAccounts.Count -eq0) {
 $saList = az storage account list --resource-group $ResourceGroup --query "[].name" -o tsv
 Throw-IfAzFailed "storage account list"
 $StorageAccounts = @()
 if ($saList) { $StorageAccounts = $saList -split "`r?`n" | Where-Object { $_ } }
}
if (-not $KeyVaults -or $KeyVaults.Count -eq0) {
 $kvList = az keyvault list --resource-group $ResourceGroup --query "[].name" -o tsv
 Throw-IfAzFailed "keyvault list"
 $KeyVaults = @()
 if ($kvList) { $KeyVaults = $kvList -split "`r?`n" | Where-Object { $_ } }
}

# --- Storage Accounts: add current IP ---
foreach ($sa in $StorageAccounts) {
 Write-Host "Adding IP $ip to Storage Account '$sa' network rules ..."
 az storage account network-rule add `
 --resource-group $ResourceGroup `
 --account-name $sa `
 --ip-address $ip | Out-Null
 Throw-IfAzFailed "storage account network-rule add ($sa)"
}

# --- Key Vaults: add current IP ---
foreach ($kv in $KeyVaults) {
 Write-Host "Adding IP $ip to Key Vault '$kv' network rules ..."
 az keyvault network-rule add `
 --name $kv `
 --resource-group $ResourceGroup `
 --ip-address $ip | Out-Null
 Throw-IfAzFailed "keyvault network-rule add ($kv)"
}

Write-Host "Signon complete. SQL, Storage, and Key Vault network rules updated. Some changes can take up to a minute to propagate." -ForegroundColor Green
