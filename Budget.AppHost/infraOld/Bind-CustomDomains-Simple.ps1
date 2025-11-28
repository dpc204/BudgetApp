<#!
.SYNOPSIS
  Minimal script to (re)apply custom domains with managed certificates to an Azure Container App.
.DESCRIPTION
  For each host:
    1. Creates a managed certificate if it does not already exist.
    2. Binds the hostname to the container app using that certificate.
  Assumes DNS already points host -> container app default FQDN (or a valid chain) and ingress is enabled.
  No DNS validation, no ingress overwrite.
.PARAMETER ResourceGroup
  Resource group containing the Container App and its environment.
.PARAMETER AppName
  Name of the existing Container App.
.PARAMETER Hosts
  Hostnames to bind (default: www.fantum.dev,fantum.dev).
.PARAMETER EnvironmentName
  (Optional) Managed Environment name. If omitted it is derived from the app.
.PARAMETER ForceRecreateCert
  If specified, deletes existing managed certificate before creating a new one.
.EXAMPLE
  ./Bind-CustomDomains-Simple.ps1 -ResourceGroup rg-BudgetApp2 -AppName budget
.EXAMPLE
  ./Bind-CustomDomains-Simple.ps1 -ResourceGroup rg-BudgetApp2 -AppName budget -Hosts www.fantum.dev,fantum.dev
!#>
[CmdletBinding()] param(
  [Parameter(Mandatory)][string]$ResourceGroup,
  [Parameter(Mandatory)][string]$AppName,
  [string[]]$Hosts = @('www.fantum.dev','fantum.dev'),
  [string]$EnvironmentName,
  [switch]$ForceRecreateCert
)

function Fail($m){ Write-Error $m; exit 1 }

Write-Host "Fetching Container App..." -ForegroundColor Cyan
$appJson = az containerapp show -g $ResourceGroup -n $AppName -o json 2>$null
if(-not $appJson){ Fail "Container App '$AppName' not found in RG '$ResourceGroup'" }
$app = $appJson | ConvertFrom-Json

if(-not $EnvironmentName){
  $envId = $app.properties.managedEnvironmentId
  if(-not $envId){ Fail "Managed environment ID not found on container app." }
  $EnvironmentName = ($envId -split '/')[-1]
}

Write-Host "Environment: $EnvironmentName" -ForegroundColor Green

# Cache existing managed certs
$existingCerts = az containerapp managed-certificate list -g $ResourceGroup --environment $EnvironmentName -o json 2>$null | ConvertFrom-Json
if(-not $existingCerts){ $existingCerts = @() }

foreach($host in $Hosts){
  $trimHost = $host.Trim().ToLower()
  if([string]::IsNullOrWhiteSpace($trimHost)){ continue }
  $certName = $trimHost -replace '\.','-'
  $cert = $existingCerts | Where-Object { $_.name -eq $certName }

  if($cert -and $ForceRecreateCert){
    Write-Host "Deleting existing managed cert $certName (force)" -ForegroundColor Yellow
    az containerapp managed-certificate delete -g $ResourceGroup --environment $EnvironmentName -n $certName --yes 1>$null 2>$null
    $cert = $null
  }

  if(-not $cert){
    Write-Host "Creating managed certificate $certName for $trimHost" -ForegroundColor Cyan
    az containerapp managed-certificate create -g $ResourceGroup --environment $EnvironmentName -n $certName --hostname $trimHost 1>$null || Fail "Failed to create cert for $trimHost"
  } else {
    Write-Host "Cert $certName already exists" -ForegroundColor DarkGray
  }

  Write-Host "Binding hostname $trimHost" -ForegroundColor Green
  az containerapp hostname bind -g $ResourceGroup -n $AppName --hostname $trimHost --certificate $certName 1>$null || Fail "Failed to bind $trimHost"
}

Write-Host "All hostnames processed successfully." -ForegroundColor Green
