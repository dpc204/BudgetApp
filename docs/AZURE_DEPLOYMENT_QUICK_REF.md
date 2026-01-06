# Azure Deployment Quick Reference

## Pre-Deployment Checklist

### 1. Ensure SessionCache Table Exists

```bash
dotnet sql-cache create "Data Source=fantumsqlserver.database.windows.net;Initial Catalog=BudgetDB;User ID=dpc;Password=Fred1$HugoMarisaConnelly;Encrypt=True" dbo SessionCache
```

**Expected output:** `Table and index were created successfully.`

### 2. Verify Configuration

Check that `BudgetConnection` is set in your Azure environment:

```bash
azd env get-values | findstr BudgetConnection
```

If not set, configure it:

```bash
azd env set BudgetConnection "Server=tcp:fantumsqlserver.database.windows.net,1433;Database=BudgetDB;User ID=dpc;Password=Fred1$HugoMarisaConnelly;Encrypt=True;TrustServerCertificate=False"
```

## Deployment

### Full Deployment (First Time)

```bash
azd up
```

This will:
1. Build Docker containers
2. Push to Azure Container Registry
3. Provision Container Apps
4. Deploy applications

**Time:** ~10-15 minutes

### Code-Only Update

```bash
azd deploy
```

This only redeploys code (no infrastructure changes).

**Time:** ~3-5 minutes

## Post-Deployment Verification

### 1. Update Entra ID Redirect URIs

**Important:** After deployment, you must update your Entra ID App Registration with the new Container App URL.

**Quick method:**
```powershell
# Get your deployed URL
az containerapp show --name budget --resource-group rg-{environment} --query "properties.configuration.ingress.fqdn" --output tsv

# Use the automated script
.\scripts\Update-EntraRedirectUris.ps1 -Environment {your-environment}
```

**Manual method:**
1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **Entra ID** ? **App Registrations** ? Your App
3. Click **Authentication**
4. Add redirect URI: `https://{your-container-app-url}/signin-oidc`
5. Add logout URL: `https://{your-container-app-url}/signout-callback-oidc`
6. Click **Save**

### 3. Check Apps Are Running

```bash
az containerapp list --resource-group rg-{your-environment} --output table
```

### 4. Verify Token Cache

```bash
sqlcmd -S fantumsqlserver.database.windows.net -d BudgetDB -U dpc -P "Fred1$HugoMarisaConnelly" -Q "SELECT COUNT(*) as CachedTokens FROM dbo.SessionCache"
```

### 5. Test Application

1. Open the deployed URL (shown in `azd up` output)
2. **If you get AADSTS50011 error**, run: `.\scripts\Update-EntraRedirectUris.ps1 -Environment {env}`
3. Log in
4. Navigate to Maintenance ? Backup All Tables
5. Should work without 401 errors ?

## Troubleshooting

### Error: AADSTS50011 - Redirect URI Mismatch

**Full Error:**
```
AADSTS50011: The redirect URI specified in the request does not match the redirect URIs configured for the application
```

**Solution:**

1. **Get your Container App URL:**
```powershell
az containerapp show --name budget --resource-group rg-{environment} --query "properties.configuration.ingress.fqdn" --output tsv
```

2. **Update Entra ID automatically:**
```powershell
.\scripts\Update-EntraRedirectUris.ps1 -Environment {environment}
```

3. **Or update manually in Azure Portal:**
   - Entra ID ? App Registrations ? Your App ? Authentication
   - Add redirect URI: `https://{container-app-url}/signin-oidc`
   - Add logout URL: `https://{container-app-url}/signout-callback-oidc`

**Why this happens:**
- Container Apps generate dynamic URLs with revision numbers (e.g., `--0000029`)
- Each deployment creates a new revision with a new number
- Entra ID redirect URIs must match exactly

**Long-term solution:** Set up a custom domain that doesn't change.

### Error: "parameter redis not found"

**Solution:** You're using an old version of the code. Pull the latest changes where Redis was removed from AppHost.

**Quick Fix:**
```bash
git pull origin main
azd deploy
```

### Error: "Cannot open database IdentityDB"

**Solution:** Wrong database name in connection string.

**Fix:** Update connection string to use `BudgetDB`:
```bash
azd env set BudgetConnection "Server=tcp:fantumsqlserver.database.windows.net,1433;Database=BudgetDB;..."
azd deploy
```

### 401 Unauthorized Errors

**Causes:**
1. SessionCache table doesn't exist
2. SQL connection string not set
3. Entra ID redirect URIs not configured

**Solutions:**

1. **Create SessionCache table:**
   ```bash
   dotnet sql-cache create "your-connection-string" dbo SessionCache
   ```

2. **Verify connection string:**
   ```bash
   azd env get-values | findstr BudgetConnection
   ```

3. **Add redirect URIs in Azure Portal:**
   - Go to Entra ID ? App Registrations ? Your App
   - Add redirect URI: `https://your-app.azurewebsites.net/signin-oidc`
   - Add logout URI: `https://your-app.azurewebsites.net/signout-callback-oidc`

## Cost Monitoring

### View Current Month Costs

```bash
az consumption usage list --start-date 2026-01-01 --end-date 2026-01-31 --query "[].{Service:instanceName,Cost:pretaxCost}" --output table
```

### Expected Monthly Costs

- Container Apps: $25-50 (consumption-based)
- Container Registry: $5
- Log Analytics: $10-20
- **Total: ~$40-75/month**

## Cleanup

### Delete All Resources

```bash
azd down --purge
```

**Warning:** This deletes everything except your SQL Server database.

### Delete Just the Apps (Keep Infrastructure)

```bash
azd down
```

This keeps the resource group but removes the apps.

## Environment Variables

### View All Settings

```bash
azd env get-values
```

### Set a Variable

```bash
azd env set VARIABLE_NAME "value"
azd deploy
```

### Required Variables

| Variable | Description |
|----------|-------------|
| `BudgetConnection` | SQL Server connection string |
| `AzureAd:ClientId` | Entra ID App Registration Client ID |
| `AzureAd:ClientSecret` | Entra ID App Secret |
| `AzureAd:TenantId` | Your Azure AD Tenant ID |

## Logs

### Stream Live Logs

```bash
az containerapp logs show --name budget --resource-group rg-{environment} --follow
```

### View Last 100 Lines

```bash
az containerapp logs show --name budget --resource-group rg-{environment} --tail 100
```

### Search Logs for Errors

```bash
az containerapp logs show --name budget --resource-group rg-{environment} --query "[?contains(message, 'error')]"
```

## Scaling

### View Current Scale Settings

```bash
az containerapp show --name budget --resource-group rg-{environment} --query "{minReplicas:properties.template.scale.minReplicas,maxReplicas:properties.template.scale.maxReplicas}" --output table
```

### Update Scale Settings

```bash
az containerapp update --name budget --resource-group rg-{environment} --min-replicas 1 --max-replicas 5
```

## Useful Commands

### Get App URLs

```bash
az containerapp show --name budget --resource-group rg-{environment} --query "properties.configuration.ingress.fqdn" --output tsv
```

### Restart App

```bash
az containerapp revision restart --name budget --resource-group rg-{environment}
```

### View Resource Group

```bash
az group show --name rg-{environment}
```

## Support

- [Azure Container Apps Documentation](https://learn.microsoft.com/azure/container-apps/)
- [Azure Developer CLI (azd) Documentation](https://learn.microsoft.com/azure/developer/azure-developer-cli/)
- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
