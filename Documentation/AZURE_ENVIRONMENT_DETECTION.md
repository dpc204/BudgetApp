# Azure Environment Detection Guide

## Problem

The original `AzureEnvironment.IsRunningOnAzure` only checked for `WEBSITE_INSTANCE_ID`, which is specific to Azure App Service. When deploying with Aspire to Azure Container Apps, this check returns `false` because Container Apps use different environment variables.

## Solution

Updated `AzureEnvironment` class to detect multiple Azure hosting environments:
- ? **Azure Container Apps** (Aspire deployments)
- ? **Azure App Service** (Web Apps)
- ? **Azure Virtual Machines**

## Environment Variables by Azure Service

### Azure Container Apps (Your Current Deployment)

Aspire automatically sets these environment variables:

| Variable | Example Value | Purpose |
|----------|---------------|---------|
| `CONTAINER_APP_NAME` | `budget-api` | Name of the container app |
| `CONTAINER_APP_REVISION` | `budget-api--abc123` | Revision identifier |
| `CONTAINER_APP_REPLICA_NAME` | `budget-api--abc123-xyz456` | Replica identifier |
| `CONTAINER_APP_ENV_DNS_SUFFIX` | `delightfulsea-xxx.eastus.azurecontainerapps.io` | DNS suffix |
| `AZURE_CLIENT_ID` | `guid` | Managed identity client ID |

### Azure App Service

| Variable | Example Value | Purpose |
|----------|---------------|---------|
| `WEBSITE_INSTANCE_ID` | `guid` | Unique instance identifier |
| `WEBSITE_SITE_NAME` | `myapp` | App Service name |
| `WEBSITE_RESOURCE_GROUP` | `rg-myapp` | Resource group name |
| `WEBSITE_OWNER_NAME` | `subscription-id+...` | Subscription info |

### Azure Virtual Machines

| Variable | Example Value | Purpose |
|----------|---------------|---------|
| `AZURE_RESOURCE_GROUP` | `rg-vm` | Resource group name |
| `AZURE_SUBSCRIPTION_ID` | `guid` | Subscription ID |

### Common Across All Azure Services (When Using Managed Identity)

| Variable | Example Value | Purpose |
|----------|---------------|---------|
| `AZURE_CLIENT_ID` | `guid` | Managed Identity Client ID |
| `AZURE_TENANT_ID` | `guid` | Azure AD Tenant ID |

## Updated AzureEnvironment Class Features

### Properties

```csharp
// Main detection - works for all Azure services
AzureEnvironment.IsRunningOnAzure  // true if ANY Azure service

// Specific service detection
AzureEnvironment.IsRunningOnContainerApps  // Azure Container Apps
AzureEnvironment.IsRunningOnAppService     // Azure App Service
AzureEnvironment.IsRunningOnAzureVirtualMachine  // Azure VMs

// Instance information
AzureEnvironment.InstanceId        // Unique instance/replica ID
AzureEnvironment.AppName           // Application name
AzureEnvironment.HostingEnvironment // "Azure Container Apps", "Azure App Service", etc.
```

### Usage Examples

#### Example 1: Check if Running on Azure (Any Service)

```csharp
if (AzureEnvironment.IsRunningOnAzure)
{
    // Use Azure services (Key Vault, Managed Identity, etc.)
    logger.LogInformation("Running on Azure: {Environment}", AzureEnvironment.HostingEnvironment);
}
else
{
    // Use local development settings
    logger.LogInformation("Running locally");
}
```

#### Example 2: Container Apps Specific Logic

```csharp
if (AzureEnvironment.IsRunningOnContainerApps)
{
    logger.LogInformation("Container App: {AppName}, Instance: {InstanceId}", 
        AzureEnvironment.AppName, 
        AzureEnvironment.InstanceId);
}
```

#### Example 3: Your Current Use Case (Connection Strings)

In `Misc.cs`, the `UseAzureDB` property now works correctly:

```csharp
public static bool UseAzureDB
{
    get
    {
        Console.WriteLine($"Checking UseAzureDB");
        
        // This now correctly detects Container Apps!
        if (AzureEnvironment.IsRunningOnAzure)
        {
            return true;
        }
        
        // Check appsettings.json for local override
        if (UseAzureDb is null)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
            var sValue = config["UseAzureDB"];
            UseAzureDb = bool.TryParse(sValue, out var bValue) ? bValue : false;
        }
        
        return (bool)UseAzureDb;
    }
}
```

## Testing Locally

You can simulate running on Azure by setting environment variables:

```powershell
# Simulate Container Apps
$env:CONTAINER_APP_NAME = "budget-api"
$env:CONTAINER_APP_REVISION = "budget-api--test"

# Run your app
dotnet run

# Clean up
Remove-Item Env:\CONTAINER_APP_NAME
Remove-Item Env:\CONTAINER_APP_REVISION
```

## Verification

### Check in Local Development

Your app should detect as **NOT** running on Azure:
```
IsRunningOnAzure: false
HostingEnvironment: Local/Unknown
```

### Check in Azure Container Apps

After deploying with Aspire, your app should detect:
```
IsRunningOnAzure: true
IsRunningOnContainerApps: true
HostingEnvironment: Azure Container Apps
AppName: budget-api
```

### Verify with Logs

Add logging to `Program.cs`:

```csharp
logger.LogInformation("Azure Detection: IsRunningOnAzure={IsAzure}, Environment={Env}, AppName={AppName}", 
    AzureEnvironment.IsRunningOnAzure,
    AzureEnvironment.HostingEnvironment,
    AzureEnvironment.AppName ?? "N/A");
```

Then check Azure Application Insights or container logs to verify detection is working.

## Alternative: Use ASP.NET Core Environment

If you only need to distinguish between Development and Production, use the built-in environment detection:

```csharp
// In Program.cs or Startup
if (builder.Environment.IsProduction())
{
    // Use Azure resources
}
else if (builder.Environment.IsDevelopment())
{
    // Use local resources
}
```

This is simpler but less specific - it won't tell you *which* Azure service you're on.

## Recommendation for Your App

Since you're using Aspire and Container Apps, the updated `AzureEnvironment.IsRunningOnAzure` will now work correctly. Your existing code in `Misc.cs` requires no changes - it will automatically detect Container Apps deployment!

### Before (Broken)
```
Local: IsRunningOnAzure = false ?
Container Apps: IsRunningOnAzure = false ? (Wrong!)
```

### After (Fixed)
```
Local: IsRunningOnAzure = false ?
Container Apps: IsRunningOnAzure = true ? (Fixed!)
```
