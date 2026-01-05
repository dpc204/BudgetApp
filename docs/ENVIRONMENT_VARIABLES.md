# Azure Environment Variables Configuration

This document explains how environment variables are configured for Azure deployment using Azure Developer CLI (azd).

## Overview

Environment variables are configured in two places:
1. **`.azure/<environment-name>/.env`** - Stores the environment variable values
2. **`Budget.AppHost/AppHost.cs`** - Reads from `.env` and applies to Container Apps

## Current Environment

Active environment: **BudgetApp2**
Location: `.azure/BudgetApp2/.env`

## Environment Variables

### Application Configuration

| Variable | Value | Description |
|----------|-------|-------------|
| `USE_AZURE_DB` | `true` | Enables Azure SQL Database mode (vs LocalDB) |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Sets ASP.NET Core environment |

### Azure AD (Entra ID) Configuration

**IMPORTANT:** Azure AD configuration (`ClientId`, `ClientSecret`, `TenantId`, `Domain`) should **NOT** be set in `.env` or `AppHost.cs`.

These values are:
- **Local Development:** Loaded from User Secrets
- **Azure Deployment:** Automatically loaded from **Azure Key Vault** by azd

Setting `AzureAd__*` as environment variables will **override** Key Vault values and cause authentication failures.

#### To update Azure AD secrets in Key Vault:

```powershell
# Update via azd (stores in Key Vault automatically)
azd env set AZURE_AD_CLIENT_SECRET "your-new-secret" --secret
```

Then ensure your Bicep/azd configuration references Key Vault for these secrets.

### Azure Storage Configuration

| Variable | Auto-populated by azd | Description |
|----------|----------------------|-------------|
| `AZURE_STORAGE_ACCOUNT_NAME` | `fantumbudgetstorage` | Storage account name |
| `AZURE_STORAGE_BLOB_ENDPOINT` | `https://fantumbudgetstorage.blob.core.windows.net/` | Blob storage endpoint |
| `AZURE_STORAGE_TABLE_ENDPOINT` | `https://fantumbudgetstorage.table.core.windows.net/` | Table storage endpoint |

### Managed Identity

| Variable | Auto-populated by azd | Description |
|----------|----------------------|-------------|
| `MANAGED_IDENTITY_CLIENT_ID` | `c5817686-acae-494b-a8e9-f5620f83b0d4` | Managed Identity Client ID |

## Adding Secrets

For sensitive values like connection strings and client secrets, use `azd env set` with the `--secret` flag:

```powershell
# Add Azure AD Client Secret
azd env set AZURE_AD_CLIENT_SECRET "your-client-secret-value" --secret

# Add SQL Connection String
azd env set BUDGET_CONNECTION_STRING "Server=...;Database=...;" --secret

# Add Identity Connection String
azd env set IDENTITY_CONNECTION_STRING "Server=...;Database=...;" --secret
```

These secrets are automatically stored in **Azure Key Vault** and injected at runtime.

### To reference secrets in AppHost.cs:

```csharp
var azureAdClientSecret = builder.Configuration["AZURE_AD_CLIENT_SECRET"] ?? "";

var budgetApi = builder.AddProject<Projects.Budget_Api>("budget-api")
    .WithEnvironment("AzureAd__ClientSecret", azureAdClientSecret)
    // ... other config
```

## How Environment Variables Flow to Container Apps

1. **azd reads** `.azure/BudgetApp2/.env`
2. **AppHost.cs reads** from `builder.Configuration` (which includes .env values)
3. **azd translates** AppHost configuration to Container App environment variables
4. **Container Apps receive** the environment variables at runtime

## Deployment

After modifying `.env` or `AppHost.cs`, deploy with:

```powershell
azd up
```

Or to redeploy only the code (without infrastructure changes):

```powershell
azd deploy
```

## Viewing Current Environment Variables

```powershell
# Show all environment variables for active environment
azd env get-values

# Show specific variable
azd env get-value USE_AZURE_DB
```

## Switching Environments

```powershell
# List available environments
azd env list

# Switch to different environment
azd env select budget

# Create new environment
azd env new my-new-env
```

## Local vs Azure Behavior

### Local Development (F5 in Visual Studio)
- Uses `appsettings.Development.json` and user secrets
- `UseAzureDB` typically `false` (uses LocalDB)
- Redis for token caching (docker-compose)

### Azure Deployment (azd up)
- Uses `.azure/<env>/.env` values via AppHost
- `UseAzureDB` = `true` (uses Azure SQL)
- SQL Server distributed cache for tokens (no Redis needed)

## Troubleshooting

### Variables Not Applied After `azd up`
1. Verify the correct environment is active: `azd env list`
2. Check `.env` file has the variables
3. Ensure `AppHost.cs` reads and applies the variables
4. Rebuild and redeploy: `azd deploy`

### Secrets Not Working
1. Verify secret is set: `azd env get-values` (secrets show as `***`)
2. Check Key Vault access in Azure Portal
3. Ensure Managed Identity has Key Vault permissions

## Related Files

- `.azure/BudgetApp2/.env` - Environment variable values
- `Budget.AppHost/AppHost.cs` - Aspire host configuration
- `infra/resources.bicep` - Azure infrastructure (storage, identity, etc.)
- `azure.yaml` - azd project manifest
