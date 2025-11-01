<#!
.SYNOPSIS
    Deploy custom domains (and managed or existing certificates) to an existing Azure Container App.
.DESCRIPTION
    Runs a group-scope Bicep deployment (customDomains-post.bicep) to patch ingress.customDomains.
    Accepts -Domain (repeatable JSON object) and/or -DomainsFile (JSON array). If none supplied, uses embedded defaults.
    Performs DNS validation unless -SkipDnsCheck.
!#>
[CmdletBinding()] param(
  [Parameter(Mandatory, Position=0)] [string] $ResourceGroup,
  [Parameter(Mandatory, Position=1)] [string] $AppName,
  [int]    $Port = 8080,
  [string[]] $Domain,
  [string] $DomainsFile,
  [string] $BicepPath = "infra/customDomains-post.bicep",
  [switch] $WhatIf,
  [switch] $Force,
  [switch] $SkipDnsCheck,
  [string] $ExpectedFqdnOverride,
  [int] $DnsRetrySeconds = 2,
  [int] $DnsMaxAttempts  = 5
)

function Fail($msg) { Write-Error $msg; exit 1 }
if (-not (Test-Path $BicepPath)) { Fail "Bicep file not found: $BicepPath" }

$domainObjs = @()

# Load from file
if ($DomainsFile) {
  if (-not (Test-Path $DomainsFile)) { Fail "Domains file not found: $DomainsFile" }
  try {
    $fileContent = Get-Content -Raw -Path $DomainsFile | ConvertFrom-Json
    if ($fileContent -is [System.Collections.IEnumerable]) { $domainObjs += $fileContent } else { Fail "Domains file must contain a JSON array." }
  } catch { Fail "Failed to parse domains file JSON: $($_.Exception.Message)" }
}

# Load from -Domain args
if ($Domain) {
  foreach ($d in $Domain) {
    if ([string]::IsNullOrWhiteSpace($d)) { continue }
    try { $domainObjs += ($d | ConvertFrom-Json) } catch { Fail "Invalid domain JSON in -Domain: $d :: $($_.Exception.Message)" }
  }
}

# Defaults if still empty (use PSCustomObject literals for consistent property access)
if ($domainObjs.Count -eq 0) {
  Write-Host "No domains supplied: using embedded defaults (fantum.dev, www.fantum.dev)" -ForegroundColor Yellow
  $domainObjs = @(
    [pscustomobject]@{ host = "www.fantum.dev"; certificateType = "managed"; certificateName = "www-fantum-dev" },
    [pscustomobject]@{ host = "fantum.dev";      certificateType = "managed"; certificateName = "fantum-dev" }
  )
}

# Normalize: convert hashtables to PSCustomObject
$domainObjs = $domainObjs | ForEach-Object { if ($_ -is [hashtable]) { [pscustomobject]$_ } else { $_ } }

# Validation
$allowedTypes = 'managed','existing','none'
foreach ($d in $domainObjs) {
  foreach ($req in 'host','certificateType','certificateName') {
    if (-not ($d.PSObject.Properties.Name -contains $req)) { Fail "Domain missing required property '$req': $(ConvertTo-Json $d -Compress)" }
  }
  if ($allowedTypes -notcontains $d.certificateType) { Fail "Invalid certificateType '$($d.certificateType)'. Allowed: $allowedTypes" }
  if ($d.certificateName -match '\.') { Fail "certificateName must not contain dots: $($d.certificateName)" }
}

# DNS validation
$defaultFqdn = $ExpectedFqdnOverride
if (-not $SkipDnsCheck -and -not $defaultFqdn) {
  Write-Host "Fetching container app default FQDN..." -ForegroundColor Cyan
  $defaultFqdn = az containerapp show -g $ResourceGroup -n $AppName --query properties.configuration.ingress.fqdn -o tsv 2>$null
  if (-not $defaultFqdn) { Fail "Unable to retrieve container app FQDN. Use -SkipDnsCheck or -ExpectedFqdnOverride." }
  Write-Host "Default FQDN: $defaultFqdn" -ForegroundColor Green
}

function Test-Apex($h) { return ($h.Split('.').Count -eq 2) }
function Resolve-With-Retry($record,[string]$type) {
  for ($i=1; $i -le $DnsMaxAttempts; $i++) {
    try { $res = Resolve-DnsName -Name $record -Type $type -ErrorAction Stop; if ($res) { return $res } } catch { Start-Sleep -Seconds $DnsRetrySeconds }
  }
  return $null
}

if (-not $SkipDnsCheck) {
  Write-Host "Validating DNS for supplied domains..." -ForegroundColor Cyan
  $dnsFailures = @()
  foreach ($d in $domainObjs) {
    $domainName = ($d.host | Out-String).Trim().ToLowerInvariant()
    if (-not $domainName) { $dnsFailures += "Empty host value."; continue }
    $isApex = Test-Apex $domainName
    if ($d.certificateType -eq 'managed') {
      if ($isApex) {
        $aRec = Resolve-With-Retry $domainName 'A'
        if (-not $aRec) { $dnsFailures += "Apex host '$domainName' missing A/ALIAS record."; continue }
      } else {
        $cname = Resolve-With-Retry $domainName 'CNAME'
        if (-not $cname) { $dnsFailures += "Host '$domainName' missing CNAME record."; continue }
        $target = ($cname | Select-Object -First 1).CName
        if ($null -eq $target) { $dnsFailures += "Host '$domainName' CNAME query yielded no target."; continue }
        $target = $target.TrimEnd('.')
        if (-not $target.EndsWith($defaultFqdn, [System.StringComparison]::OrdinalIgnoreCase)) {
          $dnsFailures += "Host '$domainName' CNAME target '$target' does not end with '$defaultFqdn'" }
      }
    } else {
      try { Resolve-DnsName -Name $domainName -ErrorAction Stop | Out-Null } catch { $dnsFailures += "Host '$domainName' does not resolve (certificateType=$($d.certificateType))." }
    }
  }
  if ($dnsFailures.Count -gt 0) {
    Write-Host "DNS validation failed:" -ForegroundColor Red
    $dnsFailures | ForEach-Object { Write-Host " - $_" -ForegroundColor Red }
    Fail "Fix DNS or use -SkipDnsCheck."
  } else { Write-Host "DNS validation passed." -ForegroundColor Green }
}

$domainsJson = $domainObjs | ConvertTo-Json -Compress
Write-Host "Prepared domains JSON:" -ForegroundColor Cyan
Write-Host $domainsJson

if (-not $Force) {
  $confirm = Read-Host "Proceed with deploying these $( $domainObjs.Count ) domain(s) to app '$AppName' in RG '$ResourceGroup'? (y/N)"
  if ($confirm.ToLower() -ne 'y') { Write-Host 'Aborted.'; exit 0 }
}

$baseArgs = @('deployment','group','create','-g',$ResourceGroup,'-f',$BicepPath,'-p',"containerAppName=$AppName","targetPort=$Port","domains=$domainsJson")
if ($WhatIf) { $baseArgs += '--what-if' }

Write-Host "Running: az $($baseArgs -join ' ')" -ForegroundColor Yellow
az @baseArgs
if ($LASTEXITCODE -ne 0) { Fail "Deployment failed (exit $LASTEXITCODE)." }

Write-Host "Deployment complete." -ForegroundColor Green
