param(
 [string]$TenantId = "d2b31d23-106e-4175-95dc-82ff027f9d9c",
 [string]$SubscriptionId = "3dd42e45-62af-4345-82d2-bffd522065f5",
 [string]$ResourceGroup = "rg-fantum",
 [string]$SqlServerName = "fantumsqlserver",
 [string]$FirewallRuleName = "AllowCurrentIP",
 [string[]]$StorageAccounts = @(), # optional; auto-discover if empty
 [string[]]$KeyVaults = @(), # optional; auto-discover if empty
 [switch]$KeepSqlRule
)

$ErrorActionPreference = "Stop"

function Ensure-AzCli() {
 if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
 throw "Azure CLI (az) is not installed or not on PATH. Install from https://aka.ms/azure-cli and retry."
 }
}

function Ensure-AzLogin([string]$TenantId,[string]$SubscriptionId) {
 Write-Host "Logging in to tenant $TenantId ..."
 az login --tenant $TenantId | Out-Null
 az account set --subscription $SubscriptionId | Out-Null
}

function Throw-IfAzFailed($operation) {
 if ($LASTEXITCODE -ne0) { throw "Azure CLI failed during: $operation (exit $LASTEXITCODE)" }
}

Ensure-AzCli
Ensure-AzLogin -TenantId $TenantId -SubscriptionId $SubscriptionId

# Remove SQL firewall rule (unless KeepSqlRule)
if (-not $KeepSqlRule) {
 Write-Host "Removing SQL firewall rule '$FirewallRuleName' from server '$SqlServerName' ..."
 az sql server firewall-rule delete `
 --resource-group $ResourceGroup `
 --server $SqlServerName `
 --name $FirewallRuleName | Out-Null
}

# Determine current public IP (for storage/kv removal)
function Get-PublicIPv4 {
 try { return (Invoke-RestMethod -Uri "https://api.ipify.org").Trim() } catch {}
 try { return (Invoke-RestMethod -Uri "https://ifconfig.me/ip").Trim() } catch {}
 throw "Unable to determine public IPv4 address."
}
$ip = Get-PublicIPv4

# Auto-discover if not provided
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

# Remove IP from Storage Accounts
foreach ($sa in $StorageAccounts) {
 Write-Host "Removing IP $ip from Storage Account '$sa' network rules ..."
 az storage account network-rule remove `
 --resource-group $ResourceGroup `
 --account-name $sa `
 --ip-address $ip | Out-Null
}

# Remove IP from Key Vaults
foreach ($kv in $KeyVaults) {
 Write-Host "Removing IP $ip from Key Vault '$kv' network rules ..."
 az keyvault network-rule remove `
 --name $kv `
 --resource-group $ResourceGroup `
 --ip-address $ip | Out-Null
}

Write-Host "Signoff complete. SQL, Storage, and Key Vault network rules cleaned up." -ForegroundColor Green
