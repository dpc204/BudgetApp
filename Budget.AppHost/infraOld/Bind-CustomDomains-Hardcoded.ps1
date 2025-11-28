# Hardcoded domain binding script for Azure Container App
# Binds existing certificates to hostnames (and optionally creates managed certs if missing).
# Adjust $AllowCreateManaged = $false to prevent any creation attempts.

# Use Continue so benign stderr (warnings) don't become terminating NativeCommandError
$ErrorActionPreference = 'Continue'
$ProgressPreference = 'SilentlyContinue'

$ResourceGroup       = 'rg-BudgetApp2'
$AppName             = 'budget'
$Hosts               = @('www.fantum.dev','fantum.dev')
$AllowCreateManaged  = $false   # existing certs only

function Invoke-AzJsonSafe {
    param([Parameter(Mandatory)][string]$ArgsLine)
    $all = Invoke-Expression $ArgsLine 2>&1
    $exit = $LASTEXITCODE
    # Filter out known benign warnings
    $filtered = $all | Where-Object { $_ -notmatch '^WARNING:' -and $_ -notmatch 'cryptography.*32-bit Python' }
    if ($exit -ne 0) {
        Write-Error "Command failed (exit=$exit): $ArgsLine`n$($all -join "`n")"
        return $null
    }
    $json = ($filtered -join "`n").Trim()
    if (-not $json) { return $null }
    try { return $json | ConvertFrom-Json } catch { Write-Error "JSON parse failed for: $ArgsLine"; return $null }
}

Write-Host 'Fetching container app...' -ForegroundColor Cyan
$app = Invoke-AzJsonSafe "az containerapp show -g $ResourceGroup -n $AppName -o json"
if (-not $app) { Write-Error 'Unable to fetch container app.'; exit 1 }
$envId = $app.properties.managedEnvironmentId
if (-not $envId) { Write-Error 'Managed environment ID missing on app.'; exit 1 }
$EnvironmentName = ($envId -split '/')[-1]
Write-Host "Environment: $EnvironmentName" -ForegroundColor Green

# Ensure extension present
$extList = Invoke-AzJsonSafe "az extension list -o json"
$hasExt = $false
if ($extList) { $hasExt = $extList | Where-Object { $_.name -eq 'containerapp' } }
if (-not $hasExt) {
    Write-Host 'Adding containerapp extension...' -ForegroundColor Yellow
    az extension add --name containerapp 1>$null 2>$null
} else {
    Write-Host 'Updating containerapp extension (ignored if current)...' -ForegroundColor DarkGray
    az extension update --name containerapp 1>$null 2>$null
}

# Detect command flavor
$hasManaged = $false
az containerapp managed-certificate --help 1>$null 2>$null
if ($LASTEXITCODE -eq 0) { $hasManaged = $true }

function Get-Certificates {
    if ($hasManaged) {
        $managed  = Invoke-AzJsonSafe "az containerapp managed-certificate list -g $ResourceGroup --environment $EnvironmentName -o json"; if (-not $managed) { $managed=@() }
        $uploaded = Invoke-AzJsonSafe "az containerapp certificate list -g $ResourceGroup --environment $EnvironmentName -o json"; if (-not $uploaded){ $uploaded=@() }
        return @($managed + $uploaded)
    } else {
        $all = Invoke-AzJsonSafe "az containerapp env certificate list -g $ResourceGroup -n $EnvironmentName -o json"; if (-not $all) { $all=@() }
        return $all
    }
}

function Find-CertForHost([string]$hostName){
    $hn = $hostName.Trim().ToLower()
    return $script:AllCerts | Where-Object {
        ($_.properties.subjectName -eq $hn) -or
        ($_.subjectName -eq $hn) -or
        ($_.name -eq ($hn -replace '\.','-'))
    } | Select-Object -First 1
}

function Create-ManagedCert([string]$hostName){
    if (-not $AllowCreateManaged) { Write-Warning "Creation disabled for $hostName"; return $null }
    $certName = $hostName -replace '\.','-'
    if ($hasManaged) {
        az containerapp managed-certificate create -g $ResourceGroup --environment $EnvironmentName -n $certName --hostname $hostName 1>$null 2>$null
    } else {
        az containerapp env certificate create -g $ResourceGroup -n $EnvironmentName --name $certName --hostname $hostName 1>$null 2>$null
    }
    if ($LASTEXITCODE -ne 0) { Write-Error "Failed creating cert for $hostName"; return $null }
    $script:AllCerts = Get-Certificates
    return Find-CertForHost $hostName
}

Write-Host 'Loading certificates...' -ForegroundColor Cyan
$script:AllCerts = Get-Certificates
if (-not $AllCerts -or $AllCerts.Count -eq 0) { Write-Host 'No certificates currently detected in environment.' -ForegroundColor DarkGray }

foreach ($h in $Hosts) {
    $hostName = $h.Trim().ToLower()
    if ([string]::IsNullOrWhiteSpace($hostName)) { continue }
    Write-Host "Processing host: $hostName" -ForegroundColor Cyan
    $cert = Find-CertForHost $hostName
    if (-not $cert) {
        Write-Host "No existing cert matched $hostName" -ForegroundColor Yellow
        $cert = Create-ManagedCert $hostName
        if (-not $cert) { Write-Warning "Skipping bind for $hostName (no certificate)."; continue }
    } else { Write-Host "Found certificate resource: $($cert.name)" -ForegroundColor Green }
    Write-Host "Binding $hostName -> cert $($cert.name)" -ForegroundColor Yellow
    az containerapp hostname bind -g $ResourceGroup --environment $EnvironmentName -n $AppName --hostname $hostName --certificate $cert.name 1>$null 2>$null
    if ($LASTEXITCODE -eq 0) { Write-Host "Bound $hostName" -ForegroundColor Green } else { Write-Error "Failed binding $hostName" }
}

Write-Host 'Completed domain bindings.' -ForegroundColor Green
