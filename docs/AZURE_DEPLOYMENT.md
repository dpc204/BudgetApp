# Azure Deployment Guide

This guide explains how to deploy the Budget App to Azure using .NET Aspire.

## Prerequisites

- Azure subscription
- Azure CLI installed ([Install](https://learn.microsoft.com/cli/azure/install-azure-cli))
- .NET 10 SDK
- Docker Desktop (for local development)

## What Gets Deployed

When you deploy this app to Azure, Aspire automatically provisions:

1. **Azure Container Apps** - For Budget.Web and Budget.Api
2. **Azure Container Registry** - For storing container images
3. **Azure Log Analytics** - For monitoring and diagnostics
4. **Virtual Network** - For secure communication between services

**Note:** This app uses **SQL Server distributed cache** for token persistence in Azure (leveraging your existing Azure SQL Database), so **no Redis Cache** is provisioned, keeping costs minimal.

## Deployment Steps

### 1. Login to Azure

```bash
az login
```

### 2. Set Your Subscription

```bash
az account set --subscription "Your-Subscription-Name"
```

### 3. Install Azure Developer CLI (azd)

If not already installed:

```powershell
# Windows
powershell -ex AllSigned -c "Invoke-RestMethod 'https://aka.ms/install-azd.ps1' | Invoke-Expression"
```

Or:

```bash
# macOS/Linux
curl -fsSL https://aka.ms/install-azd.sh | bash
```

### 4. Initialize Azure Deployment

From the repository root:

```bash
azd init
```

When prompted:
- **Environment name**: Choose a name like `budget-prod` or `budget-dev`
- **Subscription**: Select your Azure subscription
- **Location**: Choose a region (e.g., `eastus`, `westus2`)

### 5. Configure App Settings

Create a file `.azure/{environment-name}/.env` with your configuration:

```env
# Azure SQL Database
AZURE_SQL_SERVER=your-sql-server.database.windows.net
AZURE_SQL_DATABASE=BudgetDB
AZURE_SQL_ADMIN_USER=your-admin-user

# Entra ID (Azure AD) Configuration
AZUREAD_INSTANCE=https://login.microsoftonline.com/
AZUREAD_DOMAIN=yourtenant.onmicrosoft.com
AZUREAD_TENANTID=your-tenant-id
AZUREAD_CLIENTID=your-app-registration-client-id
AZUREAD_CLIENTSECRET=your-client-secret

# Azure Storage (for backups)
AZURE_STORAGE_CONNECTION_STRING=your-storage-connection-string
```

### 6. Deploy to Azure

```bash
azd up
```

This command will:
1. Build your Docker containers
2. Push images to Azure Container Registry
3. Provision Azure resources (Container Apps, Redis, etc.)
4. Deploy your applications
5. Output the URLs for your deployed apps

**Important:** Save the URL that's displayed - you'll need it for the next step!

### 7. Update Entra ID Redirect URIs

After deployment, you **must** update your Entra ID App Registration with the Container App URL:

**Automated Method (Recommended):**
```powershell
# Run from repository root
.\scripts\Update-EntraRedirectUris.ps1 -Environment {your-environment-name}
```

**Manual Method:**
1. Copy your Container App URL from step 6 output
2. Go to [Azure Portal](https://portal.azure.com)
3. Navigate to **Entra ID** ? **App Registrations** ? Find app `36ca674b-1c79-49ad-98fb-b90f13d72887`
4. Click **Authentication** tab
5. Under **Redirect URIs**, click **Add URI**
6. Add: `https://{your-container-app-url}/signin-oidc`
7. Under **Logout URL**, add: `https://{your-container-app-url}/signout-callback-oidc`
8. Click **Save**

**Why this is needed:** Container Apps generate dynamic URLs that change with each deployment. Entra ID requires exact matches for security.

### 8. Configure Database Connection

After deployment, set the SQL connection string as a secret:

```bash
azd env set BudgetConnection "Server=tcp:your-server.database.windows.net,1433;Database=BudgetDB;User ID=your-user;Password=your-password;Encrypt=True;TrustServerCertificate=False"
```

Then redeploy:

```bash
azd deploy
```

**Note:** After redeployment, you may need to run `Update-EntraRedirectUris.ps1` again if the Container App URL changed.

## Azure Resources Created

### Token Cache Configuration

**Local Development:** Uses Redis (Docker container) for best performance

**Azure Production:** Uses SQL Server distributed cache with your existing Azure SQL Database

The `SessionCache` table is automatically created in your BudgetDB database. This approach:
- ? **Zero additional cost** - Uses existing SQL Server
- ? **Automatic provisioning** - Table created on first deployment
- ? **Reliable** - SQL Server is highly available
- ? **Slightly slower** - SQL queries vs in-memory Redis (negligible for auth tokens)

To verify the cache table exists after deployment:

```bash
sqlcmd -S your-server.database.windows.net -d BudgetDB -U your-user -P your-password \
  -Q "SELECT COUNT(*) as CachedTokens FROM dbo.SessionCache"
```

### Container Apps

Two Container Apps are created:
- **budget-api** - The API backend
- **budget** - The Blazor web frontend

Both apps are configured with:
- Managed Identity enabled
- Application Insights integration
- Auto-scaling rules
- Health probes

## Monitoring

### View Application Logs

```bash
azd monitor --logs
```

### View Application Insights

```bash
azd monitor --overview
```

### Check Token Cache Metrics

```bash
# View number of cached tokens
sqlcmd -S your-server.database.windows.net -d BudgetDB -U your-user -P your-password \
  -Q "SELECT COUNT(*) as TokenCount, SUM(DATALENGTH([Value]))/1024.0 as SizeKB FROM dbo.SessionCache"
```

## Updating the Deployment

### Deploy Code Changes

```bash
azd deploy
```

### Update Environment Variables

```bash
azd env set VARIABLE_NAME "value"
azd deploy
```

### Update Infrastructure

Modify `Budget.AppHost/AppHost.cs` then:

```bash
azd provision
```

## Cost Optimization

This application is designed for **minimal Azure costs**:

### Current Architecture (Cost-Optimized)

- **Container Apps**: Consumption-based ($0 idle, ~$25-50/month with traffic)
- **SQL Server**: Uses your existing Azure SQL Database (no additional cost)
- **Container Registry**: Basic tier (~$5/month)
- **Log Analytics**: Pay-as-you-go (~$10-20/month)
- **Token Cache**: SQL Server table (no additional cost)

**Total: ~$40-75/month** (most costs are consumption-based)

### Optional: Redis for Better Performance

If you need better token cache performance and can afford it:

```csharp
// In AppHost.cs - only if you want to pay for Redis
var redis = builder.AddRedis("redis")
    .WithDataVolume()
    .AsAzureRedis(redis => redis
        .WithTier("Basic")
        .WithVmSize("C0")); // +$16/month
```

Then remove the SQL cache logic from `ConfigureServices.cs`.

## Troubleshooting

### Token Cache Issues

1. Check if SessionCache table exists:
   ```bash
   sqlcmd -S your-server.database.windows.net -d BudgetDB -U your-user -P your-password \
     -Q "SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SessionCache'"
   ```

2. If table doesn't exist, create it manually:
   ```bash
   dotnet sql-cache create "your-connection-string" dbo SessionCache
   ```

3. Check Container App logs for cache errors:
   ```bash
   az containerapp logs show --name budget \
     --resource-group rg-{environment-name} \
     --follow
   ```

### Authentication Issues

**Error: AADSTS50011 - Redirect URI Mismatch**

This is the most common error after deployment.

**Symptoms:**
- Cannot log in to deployed app
- Error message mentions redirect URI not matching

**Solution:**
```powershell
# Quick fix - updates Entra ID with current Container App URL
.\scripts\Update-EntraRedirectUris.ps1 -Environment {environment}
```

**Root Cause:** Container Apps use dynamic URLs that include revision numbers. These change with each deployment, but Entra ID redirect URIs must match exactly.

**Permanent Solution:** Configure a custom domain for your Container App that doesn't change between deployments.

---

**Other Authentication Issues:**

1. Verify Entra ID configuration in Azure Portal
2. Ensure Redirect URIs include your deployed app URL
3. Check that API scope exists: `api://{ClientId}/access_as_user`

### Database Connection Issues

1. Verify SQL Server firewall rules allow Azure services
2. Check connection string in environment variables
3. Ensure managed identity has SQL database permissions

## Scaling

### Manual Scaling

Scale Container Apps:

```bash
az containerapp update --name budget \
  --resource-group rg-{environment-name} \
  --min-replicas 2 \
  --max-replicas 10
```

### Auto-Scaling Rules

Container Apps auto-scale based on HTTP traffic by default. To customize:

```bash
az containerapp update --name budget \
  --resource-group rg-{environment-name} \
  --set "configuration.ingress.targetPort=8080" \
  --scale-rule-name http-rule \
  --scale-rule-type http \
  --scale-rule-http-concurrency 100
```

## Security

### Managed Identity

Both Container Apps use Managed Identity to access:
- Azure SQL Database (for app data and token cache)
- Azure Storage
- Application Insights

**No Redis connection strings needed!** All authentication uses Managed Identity where possible.

### Network Isolation

All services communicate through:
- Private virtual network
- No public internet exposure for SQL
- TLS encryption for all connections
- No Redis to configure!

### Secrets Management

Sensitive configuration is stored in:
- Azure Key Vault (automatic with Aspire)
- Container App secrets (encrypted at rest)

## Cost Estimation

**Monthly cost estimate** (minimized for small deployments):

- **Container Apps**: ~$25-50/month (consumption-based, scales to zero)
- **SQL Server**: Using existing database ($0 additional)
- **Token Cache**: SQL Server table ($0 additional)
- **Container Registry**: Basic tier (~$5/month)
- **Log Analytics**: ~$10-20/month (can be reduced with retention policies)

**Total: ~$40-75/month** with most costs being consumption-based

### Further Cost Reduction

1. **Use free Container Apps allocation**: First 180,000 vCore-seconds free/month
2. **Minimize Log Analytics retention**: Set to 30 days instead of 90
3. **Use existing resources**: SQL Server, Storage accounts you already have
4. **Scale to zero**: Container Apps can scale down to zero when not in use

## Clean Up Resources

To delete all Azure resources:

```bash
azd down --purge
```

This removes:
- All Container Apps
- Container Registry
- Log Analytics workspace
- Virtual Network
- Resource Group

**Note:** Your SQL Server database and SessionCache table are NOT deleted (they're in your existing SQL Server).

## Next Steps

- [Configure custom domains](https://learn.microsoft.com/azure/container-apps/custom-domains-managed-certificates)
- [Set up CI/CD with GitHub Actions](https://learn.microsoft.com/azure/developer/azure-developer-cli/configure-devops-pipeline)
- [Enable Application Insights](https://learn.microsoft.com/azure/container-apps/application-insights)
- [Optimize Container App costs](https://learn.microsoft.com/azure/container-apps/billing)

## Resources

- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [Azure Container Apps](https://learn.microsoft.com/azure/container-apps/)
- [SQL Server Distributed Cache](https://learn.microsoft.com/aspnet/core/performance/caching/distributed#distributed-sql-server-cache)
- [Azure Developer CLI (azd)](https://learn.microsoft.com/azure/developer/azure-developer-cli/)
