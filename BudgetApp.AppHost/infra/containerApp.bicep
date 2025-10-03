// containerApp.bicep
// Deploy a single Azure Container App in the current resource group.
// Assumes managed environment already exists (pass its resource ID).
// Uses SystemAssigned identity; adjust if you need user-assigned.

targetScope = 'resourceGroup'

@description('Container App name')
param name string

@description('Managed Environment resource ID')
param managedEnvironmentId string

@description('Container image (registry/name:tag)')
param image string

@description('Target port for ingress')
param port int = 8080

@description('Min replicas')
param minReplicas int = 0

@description('Max replicas')
param maxReplicas int = 2

@description('vCPU cores (integer). Use 1 for 1.0, 2 for 2.0, etc. If you need fractional (e.g. 0.25) adjust after deployment or update to a newer Bicep supporting decimals.')
param cpuCores int = 1

@description('Memory size string (e.g. "0.5Gi", "1Gi")')
param memorySize string = '0.5Gi'

@description('Tags applied to the Container App')
param tags object = {}

resource app 'Microsoft.App/containerApps@2024-03-01' = {
  name: name
  location: resourceGroup().location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: managedEnvironmentId
    configuration: {
      ingress: {
        external: true
        targetPort: port
      }
    }
    template: {
      containers: [
        {
          name: 'app'
          image: image
          resources: {
            cpu: cpuCores
            memory: memorySize
          }
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
      }
    }
  }
  tags: tags
}

output containerAppName string = app.name
output containerAppFqdn string = app.properties.configuration.ingress.fqdn







