# Azure AD Configuration Troubleshooting

## Error: "The 'ClientId' option must be provided"

### Symptom
```
ArgumentNullException: IDW10106: The 'ClientId' option must be provided.
Microsoft.Identity.Web.MergedOptionsValidation.Validate(MergedOptions options)
```

### Root Cause
Azure AD configuration (`ClientId`, `ClientSecret`, `TenantId`) is not being loaded correctly in Azure.

### Common Causes

#### 1. Environment Variables Overriding Key Vault ?

**Problem:** Setting `AzureAd__ClientId`, `AzureAd__TenantId`, or `AzureAd__ClientSecret` as **environment variables** in Container Apps overrides Key Vault configuration.

**Solution:** Remove Azure AD settings from:
- `Budget.AppHost/AppHost.cs` (`.WithEnvironment("AzureAd__*", ...)`)
- `.azure/<env>/.env` file (`AZURE_AD_*` variables)

Azure AD secrets should ONLY come from:
- **Local:** User Secrets
- **Azure:** Key Vault (configured by azd automatically)

#### 2. Key Vault Not Configured ?

**Problem:** azd hasn't been configured to store Azure AD secrets in Key Vault.

**Solution:** Use `azd env set` with `--secret` flag:

```powershell
azd env set AZURE_AD_CLIENT_ID "36ca674b-1c79-49ad-98fb-b90f13d72887" --secret
azd env set AZURE_AD_CLIENT_SECRET "your-client-secret" --secret
azd env set AZURE_AD_TENANT_ID "d2b31d23-106e-4175-95dc-82ff027f9d9c" --secret
azd env set AZURE_AD_DOMAIN "YourTenant.onmicrosoft.com" --secret
```

Then redeploy:
```powershell
azd up
```

#### 3. Managed Identity Missing Key Vault Access ?

**Problem:** The Container App's Managed Identity doesn't have permission to read from Key Vault.

**Solution:** Check your Bicep configuration includes Key Vault access policy or RBAC role:

```bicep
// Option A: Access Policy
resource keyVault 'Microsoft.KeyVault/vaults@2023-02-01' existing = {
  name: 'your-keyvault-name'
}

resource keyVaultAccessPolicy 'Microsoft.KeyVault/vaults/accessPolicies@2023-02-01' = {
  parent: keyVault
  name: 'add'
  properties: {
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: managedIdentity.properties.principalId
        permissions: {
          secrets: ['get', 'list']
        }
      }
    ]
  }
}

// Option B: RBAC (Recommended)
resource keyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, managedIdentity.id, 'Key Vault Secrets User')
  scope: keyVault
  properties: {
    principalId: managedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6') // Key Vault Secrets User
  }
}
```

## Configuration Hierarchy (Order of Precedence)

ASP.NET Core loads configuration in this order (later sources override earlier ones):

1. **appsettings.json** (lowest priority)
2. **appsettings.{Environment}.json**
3. **User Secrets** (local dev only)
4. **Environment Variables** ?? **OVERRIDES Key Vault!**
5. **Command Line Arguments** (highest priority)

**Key Vault** is loaded via `AddAzureKeyVault()` in the configuration builder, but environment variables still override it!

## Best Practice: Where to Store Azure AD Config

| Environment | Storage Location | Set Via |
|-------------|-----------------|---------|
| **Local Development** | User Secrets | `dotnet user-secrets set "AzureAd:ClientId" "..."` |
| **Azure (Container Apps)** | Azure Key Vault | `azd env set AZURE_AD_CLIENT_ID "..." --secret` |
| **DO NOT USE** | `.env` file or `AppHost.WithEnvironment()` | ? Overrides Key Vault |

## Verification Steps

### 1. Check Local Configuration
```powershell
# View user secrets
dotnet user-secrets list --project Budget.Web
```

Should show:
```
AzureAd:ClientId = 36ca674b-1c79-49ad-98fb-b90f13d72887
AzureAd:ClientSecret = cV18Q~UJuO...
AzureAd:TenantId = d2b31d23-106e-4175-95dc-82ff027f9d9c
AzureAd:Domain = SherwinWilliams854.onmicrosoft.com
```

### 2. Check Azure Environment Variables
```powershell
azd env get-values
```

Should **NOT** show:
```
AZURE_AD_CLIENT_ID="..."  ? BAD - Remove this
AZURE_AD_TENANT_ID="..."  ? BAD - Remove this
```

### 3. Check Key Vault Secrets
```powershell
# List Key Vault name
azd env get-values | Select-String -Pattern "KEY_VAULT"

# List secrets in Key Vault
az keyvault secret list --vault-name <vault-name> --query "[].name"
```

Should include:
- `AZURE-AD-CLIENT-ID`
- `AZURE-AD-CLIENT-SECRET`
- `AZURE-AD-TENANT-ID`

### 4. Check Container App Environment Variables

In Azure Portal:
1. Navigate to your Container App
2. Go to **Revision Management** > **Active Revision**
3. Check **Environment Variables** tab

**Azure AD settings should NOT appear here!** They should be referenced from Key Vault as secrets.

## Quick Fix Summary

1. **Remove** `AzureAd__*` from `AppHost.cs`:
```csharp
// ? REMOVE THIS:
.WithEnvironment("AzureAd__ClientId", azureAdClientId)
.WithEnvironment("AzureAd__TenantId", azureAdTenantId)
```

2. **Remove** `AZURE_AD_*` from `.azure/<env>/.env`:
```bash
# ? DELETE THESE LINES:
AZURE_AD_CLIENT_ID="..."
AZURE_AD_TENANT_ID="..."
```

3. **Set secrets in Key Vault** via azd:
```powershell
azd env set AZURE_AD_CLIENT_ID "36ca674b-1c79-49ad-98fb-b90f13d72887" --secret
azd env set AZURE_AD_CLIENT_SECRET "your-secret" --secret
azd env set AZURE_AD_TENANT_ID "d2b31d23-106e-4175-95dc-82ff027f9d9c" --secret
```

4. **Redeploy:**
```powershell
azd up
```

## Related Documentation

- [Environment Variables Configuration](./ENVIRONMENT_VARIABLES.md)
- [Authentication Troubleshooting](./AUTHENTICATION_TROUBLESHOOTING.md)
- [Azure Deployment Guide](./AZURE_DEPLOYMENT.md)
