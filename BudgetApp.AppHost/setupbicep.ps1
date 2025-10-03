# AddDomaints.ps1 - Apply custom domains to existing Container App
# Usage: ./AddDomaints.ps1 -ResourceGroup rg-MyEnv -AppName myapp -Port 8080

param(
  [Parameter(Mandatory=$true)] [string]$ResourceGroup,
  [Parameter(Mandatory=$true)] [string]$AppName,
  [int]$Port = 8080
)

# EDIT THIS ARRAY to add/remove domains
$domainObjects = @(
  @{ host = "www.fantum.dev"; certificateType = "managed"; certificateName = "www-fantum-dev" },
  @{ host = "fantum.dev";      certificateType = "managed"; certificateName = "fantum-dev" }
)

# Convert to compressed JSON for passing as parameter
$domainsJson = $domainObjects | ConvertTo-Json -Compress
Write-Host "Domains JSON: $domainsJson" -ForegroundColor Cyan

# Run the deployment (group scope)
az deployment group create `
  -g $ResourceGroup `
  -f infra/customDomains-post.bicep `
  -p containerAppName=$AppName targetPort=$Port domains="$domainsJson" || exit $LASTEXITCODE

Write-Host "Custom domain deployment complete." -ForegroundColor Green