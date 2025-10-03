targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Environment name used in resource naming (resource group will be rg-<env>).')
param environmentName string

@minLength(1)
@description('Azure location for all resources.')
param location string

@description('Principal Id (optional).')
param principalId string = ''

@description('Container App name (leave empty to skip app & domain deployment).')
param containerAppName string = ''

@description('Container image (registry/name:tag). If empty, sample image used.')
param containerImage string = ''

@description('Container port for ingress.')
param containerPort int = 8080

@description('Min replicas.')
param containerMinReplicas int = 0

@description('Max replicas.')
param containerMaxReplicas int = 2

@description('CPU cores (integer).')
param containerCpuCores int = 1

@description('Memory size (e.g. 0.5Gi, 1Gi).')
param containerMemory string = '0.5Gi'

@description('Custom domains (host, certificateType, certificateName, optional certificateSecretRef).')
param customDomainBindings array = []

var tags = {
  'azd-env-name': environmentName
}

// Resource Group
resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}

// Core infra (managed identity, ACR, Log Analytics, Container Apps Environment)
module core 'resources.bicep' = {
  scope: rg
  name: 'core'
  params: {
    location: location
    tags: tags
    principalId: principalId
  }
}

// Container App (optional)
module app 'containerApp.bicep' = if (!empty(containerAppName)) {
  scope: rg
  name: 'app'
  params: {
    name: containerAppName
    managedEnvironmentId: core.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID
    image: empty(containerImage) ? 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest' : containerImage
    port: containerPort
    minReplicas: containerMinReplicas
    maxReplicas: containerMaxReplicas
    cpuCores: containerCpuCores
    memorySize: containerMemory
    tags: tags
  }
}

// Custom Domains (optional – requires container app name & non-empty bindings)
module domainModule 'customDomains.bicep' = if (!empty(containerAppName) && length(customDomainBindings) > 0) {
  scope: rg
  name: 'domainModule'
  dependsOn: [ app ]
  params: {
    location: location
    containerAppName: containerAppName
    resourceGroupName: rg.name
    ingressTargetPort: containerPort
    customDomains: customDomainBindings
    tags: tags
  }
}

// Outputs
output MANAGED_IDENTITY_CLIENT_ID string = core.outputs.MANAGED_IDENTITY_CLIENT_ID
output MANAGED_IDENTITY_NAME string = core.outputs.MANAGED_IDENTITY_NAME
output AZURE_LOG_ANALYTICS_WORKSPACE_NAME string = core.outputs.AZURE_LOG_ANALYTICS_WORKSPACE_NAME
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = core.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = core.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID
output AZURE_CONTAINER_REGISTRY_NAME string = core.outputs.AZURE_CONTAINER_REGISTRY_NAME
output AZURE_CONTAINER_APPS_ENVIRONMENT_NAME string = core.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_NAME
output AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = core.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID
output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = core.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN
output CONTAINER_APP_FQDN string = !empty(containerAppName) ? app.outputs.containerAppFqdn : ''
output APPLIED_CUSTOM_DOMAINS array = (!empty(containerAppName) && length(customDomainBindings) > 0) ? customDomainBindings : []
