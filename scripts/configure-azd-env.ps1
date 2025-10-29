param(
 [string]$SubscriptionId = "3dd42e45-62af-4345-82d2-bffd522065f5",
 [string]$ResourceGroupName = "rg-BudgetApp2",
 [string]$ContainerAppName = "budget",
 [string]$EnvName = "budget"
)

$ErrorActionPreference = 'Stop'

if ($SubscriptionId) {
 az account set -s $SubscriptionId | Out-Null
}

Write-Host "Reading Container App '$ContainerAppName' in RG '$ResourceGroupName'..."
$app = az containerapp show -g $ResourceGroupName -n $ContainerAppName --query '{env:properties.managedEnvironmentId, location:location, registries:properties.configuration.registries}' -o json | ConvertFrom-Json

if (-not $app) { throw "Container App '$ContainerAppName' not found in '$ResourceGroupName'" }

$envId = $app.env
$location = $app.location
$regs = @($app.registries)
$reg = $null
if ($regs -and $regs.Length -gt 0) { $reg = $regs[0] }
$acrEndpoint = if ($reg) { $reg.server } else { $null }
$acrMiId = if ($reg) { $reg.identity } else { $null }

# Resolve the clientId for the user-assigned identity so DefaultAzureCredential can use it
$miClientId = $null
if ($acrMiId) {
 try {
 $miClientId = az identity show --ids $acrMiId --query clientId -o tsv
 } catch {}
}

Write-Host "Managed Environment: $envId"
Write-Host "Location: $location"
if ($acrEndpoint) { Write-Host "Registry: $acrEndpoint" }
if ($acrMiId) { Write-Host "Registry MI: $acrMiId" }
if ($miClientId) { Write-Host "MI ClientId: $miClientId" }

# Create/select azd env and set variables
azd env new $EnvName --subscription $SubscriptionId --location $location --no-prompt 2>$null

azd env set AZURE_SUBSCRIPTION_ID $SubscriptionId
azd env set AZURE_RESOURCE_GROUP $ResourceGroupName
azd env set AZURE_LOCATION $location
azd env set AZURE_CONTAINER_APPS_ENVIRONMENT_ID $envId

if ($acrEndpoint) { azd env set AZURE_CONTAINER_REGISTRY_ENDPOINT $acrEndpoint }
if ($acrMiId) { azd env set AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID $acrMiId }
if ($miClientId) { azd env set MANAGED_IDENTITY_CLIENT_ID $miClientId }

Write-Host "Environment '$EnvName' configured. You can now run: azd deploy"
