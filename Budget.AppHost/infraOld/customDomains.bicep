// Module: customDomains.bicep
// Purpose: Manage custom domain bindings (and optional managed certificates) for an Azure Container App.
// ----------------------------------------------------------------------------------
// HOW TO USE / ADAPT:
// 1. Set `containerAppName` to the existing Container App resource name you want to attach domains to.
// 2. Provide a list of `customDomains` objects. Each object requires:
//      - host:            The FQDN you want to bind (e.g. "app.example.com")
//      - certificateType: 'managed' | 'existing' | 'none'
//      - certificateName: (required for managed or existing) A unique name for the cert resource or reference.
//      - certificateSecretRef: (only if certificateType == 'existing') Name of a secret already added to the Container App that holds the cert (PFX) or certificate chain.
// 3. For managed certificates, you must have a CNAME or A record pointing to the Container App default domain before deployment so issuance can succeed.
// 4. If you use an existing certificate, you must have previously added the secret (e.g. via CLI or another bicep module) to the Container App.
// 5. Set `ingressTargetPort` to match the exposed port of your running container image.
// 6. This module DOES NOT create the Container App itself; it assumes it already exists.
// 7. Add this module in main.bicep AFTER the Container App deployment module.
// 8. Redeployment is idempotent.

@description('Location (should match the Container App resource location).')
param location string

@description('Name of existing Container App to attach domains to.')
param containerAppName string

@description('Resource group name (informational).')
param resourceGroupName string

@description('Target container port for ingress.')
param ingressTargetPort int = 8080

@description('List of custom domains to bind.')
param customDomains array = [
  // Example objects (remove or replace):
  // {
  //   host: 'app.example.com'
  //   certificateType: 'managed'
  //   certificateName: 'app-example-com'
  // }
]

@description('Tags propagated to any created managed certificates.')
param tags object = {}

// Existing app reference
resource containerApp 'Microsoft.App/containerApps@2024-03-01' existing = {
  name: containerAppName
}

// Managed certificates
resource managedCerts 'Microsoft.App/managedCertificates@2023-05-01' = [for d in customDomains: if (d.certificateType == 'managed') {
  name: string(d.certificateName)
  location: location
  tags: union(tags, { 'bicep-module': 'customDomains' })
  properties: {
    subjectName: string(d.host)
  }
}]

// Map ingress customDomains structure
var mappedDomains = [for d in customDomains: {
  name: d.host
  bindingType: 'SniEnabled'
  certificateId: d.certificateType == 'managed' ? resourceId('Microsoft.App/managedCertificates', string(d.certificateName)) : null
  certificateRef: d.certificateType == 'existing' ? string(d.certificateSecretRef) : null
}]

resource containerAppUpdate 'Microsoft.App/containerApps@2024-03-01' = {
  name: containerApp.name
  location: location
  properties: {
    configuration: {
      ingress: {
        external: true
        targetPort: ingressTargetPort
        customDomains: mappedDomains
      }
    }
  }
  dependsOn: [ managedCerts ]
}

output appliedDomains array = mappedDomains
