// App Service deployment for Blazor Server hosting API in-proc (Linux)
// Scope: resource group

targetScope = 'resourceGroup'

@description('Location')
param location string

@description('Environment name used in resource naming (rg-<env>, asp-<env>, as-<env>)')
param environmentName string

@description('App Service plan SKU (e.g., B1, S1)')
@allowed([
  'B1'
  'B2'
  'B3'
  'S1'
  'S2'
  'S3'
])
param appServiceSku string = 'B1'

var nameSuffix = toLower(replace(environmentName, ' ', ''))
var planName = 'asp-${nameSuffix}'
var siteName = 'as-${nameSuffix}'

var planTier = startsWith(appServiceSku, 'S') ? 'Standard' : 'Basic'
var enableAlwaysOn = true // Basic and above support AlwaysOn

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  sku: {
    name: appServiceSku
    tier: planTier
    capacity: 1
  }
  properties: {
    reserved: true // Linux plan
  }
  tags: {
    'azd-env-name': environmentName
    // Tagging the plan is optional for azd, but helps with traceability
  }
}

resource site 'Microsoft.Web/sites@2023-12-01' = {
  name: siteName
  location: location
  kind: 'app,linux'
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      ftpsState: 'Disabled'
      alwaysOn: enableAlwaysOn
      http20Enabled: true
    }
  }
  tags: {
    // Required by azd to locate the deploy target for service "web"
    'azd-service-name': 'web'
    'azd-env-name': environmentName
  }
}

// Configure application settings after site exists to allow referencing its hostname
resource appSettings 'Microsoft.Web/sites/config@2023-12-01' = {
  name: '${site.name}/appsettings'
  properties: {
    ASPNETCORE_ENVIRONMENT: 'Production'
    BUDGET_API_BASE_URL: 'https://${site.properties.defaultHostName}'
    // Force Zip Deploy to run from package and skip build on server
    WEBSITE_RUN_FROM_PACKAGE: '1'
    SCM_DO_BUILD_DURING_DEPLOYMENT: '0'
  }
}

output siteName string = site.name
output siteUrl string = 'https://${site.properties.defaultHostName}'
output planName string = plan.name
