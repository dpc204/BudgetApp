# How to Fix: Return to Pure Aspire Deployment

## What Went Wrong

Someone created a custom `Budget.Web\infra\main.bicep` file that:
1. Deploys Budget.Web as an Azure App Service (not Container Apps)
2. Sets `BUDGET_API_BASE_URL` to Budget.Web's OWN URL (wrong!)
3. Breaks Aspire's automatic service discovery

This is why you lost the "it just works" experience you had before.

## The Fix: Remove Custom Infrastructure

### Step 1: Remove the Custom Bicep Files

```powershell
# Backup the custom infrastructure (in case you need it)
Move-Item "Budget.Web\infra" "Budget.Web\infra.BACKUP" -Force
```

### Step 2: Clean Up Existing Azure Resources

```powershell
cd Budget.AppHost

# Remove all existing resources
azd down --force --purge
```

### Step 3: Deploy with Pure Aspire

```powershell
# Deploy using Aspire's automatic Container Apps deployment
azd up
```

That's it! Aspire will now:
- ? Deploy both Budget.Web and Budget.Api as Container Apps
- ? Automatically configure service discovery
- ? Set up internal networking between services
- ? Configure CORS automatically for internal communication
- ? Handle all environment variables

## What Was Reverted in Code

I've reverted the following unnecessary changes:

### `Budget.Web\Startup\ConfigureServices.cs`
- ? Removed `BUDGET_API_URL` fallback logic
- ? Back to pure `https+http://budget-api` service discovery URL

### `Budget.Web\Startup\AddTelemetry.cs`
- ? Removed conditional service discovery logic
- ? Back to always using `.AddServiceDiscovery()`

### `Budget.Api\Program.cs`
- ?? Kept the improved CORS configuration (it's actually helpful and doesn't hurt)
- It allows `*.azurecontainerapps.io` domains automatically

## Why This Works

With Aspire's automatic deployment:

1. **Service Discovery**: Budget.Web uses `https+http://budget-api` and Aspire automatically resolves it to the correct Container App URL
2. **Internal Networking**: Container Apps in the same environment communicate through internal networking (fast and secure)
3. **No Manual Config**: No need to set `BUDGET_API_URL`, `ALLOWED_ORIGINS`, or any other environment variables
4. **Zero Complexity**: No custom Bicep files, no manual URL configuration

## The Budget.Web\infra Folder

The `Budget.Web\infra\main.bicep` file was deploying Budget.Web as an Azure App Service (different from Container Apps) and setting:

```bicep
BUDGET_API_BASE_URL: 'https://${site.properties.defaultHostName}'
```

This sets the API URL to Budget.Web's own URL, which is completely wrong! It should either:
- Not be set (let Aspire handle it), OR
- Point to the Budget.Api URL

## Verification

After `azd up` completes:

```powershell
# Check that service discovery is working
az containerapp show --name budget --resource-group rg-<your-env> --query "properties.template.containers[0].env" --output table

# You should NOT see BUDGET_API_URL or it should be set correctly by Aspire
```

## Going Forward

- **Don't** create custom `infra` folders in your project directories
- **Do** let Aspire handle all infrastructure through AppHost.cs
- **Do** use `.WithReference()` in AppHost.cs for service dependencies
- **Don't** manually set service URLs in environment variables

If you need custom infrastructure (like custom domains, specific SKUs, etc.), modify the AppHost.cs file or use Aspire's extension methods, not custom Bicep files in project folders.
