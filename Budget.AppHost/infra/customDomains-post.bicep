// customDomains-post.bicep
// Purpose: Apply (or re-apply) ONLY custom domain + certificate bindings to an EXISTING Azure Container App
// Run AFTER your normal deployment (azd up / Aspire publish) has created the container app.
// Idempotent: re-running overwrites the ingress.customDomains list.
//
// Domain object shape passed in "domains" param:
//   {
//     host: 'www.example.com'
//     certificateType: 'managed' | 'existing' | 'none'
//     certificateName: 'www-example-com'          // no dots
//     // certificateSecretRef (NOT USED in this API version; for existing, ensure a certificate resource exists)
//   }
// For existing certificates, you must have already created a certificate resource (Microsoft.App/certificates) named certificateName.
// Managed certificate requires DNS (CNAME/ALIAS) pointing host -> <containerapp default fqdn> BEFORE deployment.
// "none" will bind host without TLS (not recommended in production).

targetScope = 'resourceGroup'

@description('Name of the existing Container App to patch')
param containerAppName string

@description('Ingress targetPort (must match the original container port)')
param targetPort int = 8080

@description('Domain objects: host, certificateType, certificateName')
param domains array = []

@description('Tags applied to any managed certificate resources created')
param tags object = {}

// Existing container app reference
resource app 'Microsoft.App/containerApps@2024-03-01' existing = {
  name: containerAppName
}

// Managed certificates (only those requesting certificateType == managed)
resource managedCerts 'Microsoft.App/managedCertificates@2023-05-01' = [for d in domains: if (d.certificateType == 'managed') {
  name: string(d.certificateName)
  location: resourceGroup().location
  tags: union(tags, { 'module': 'customDomains-post' })
  properties: {
    subjectName: string(d.host)
  }
}]

// Build customDomains list. For managed cert use managedCertificates resourceId.
// For existing cert assume a Microsoft.App/certificates resource with same name already exists.
var customDomainsList = [for d in domains: union({
    name: d.host
    bindingType: 'SniEnabled'
  },
  d.certificateType == 'managed' ? {
    certificateId: resourceId('Microsoft.App/managedCertificates', string(d.certificateName))
  } : (d.certificateType == 'existing' ? {
    certificateId: resourceId('Microsoft.App/certificates', string(d.certificateName))
  } : {})
)]

// Patch container app ingress (OVERWRITES ingress.customDomains & basic ingress settings)
resource appPatch 'Microsoft.App/containerApps@2024-03-01' = {
  name: app.name
  location: resourceGroup().location
  properties: {
    configuration: {
      ingress: {
        external: true
        targetPort: targetPort
        customDomains: customDomainsList
      }
    }
  }
  dependsOn: [ managedCerts ]
}

output appliedDomains array = customDomainsList
