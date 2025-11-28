# How to Control Azure Container Apps Plan Type with Aspire

## Overview

Azure Container Apps has two pricing models:
1. **Consumption** - Pay only for what you use (includes free tier)
2. **Dedicated** (Workload Profiles) - Pre-allocated resources with predictable pricing

By default, Aspire deploys to the **Consumption plan**.

## Methods to Control Plan Type

### Option 1: Using Azure Developer CLI (azd) Parameters

When deploying with `azd up`, Aspire generates infrastructure as code. You can customize the Container Apps Environment plan by adding parameters.

#### Step 1: Create an `infra` folder in Budget.AppHost

```powershell
mkdir Budget.AppHost\infra
```

#### Step 2: Create `main.parameters.json`

Create `Budget.AppHost\infra\main.parameters.json`:

```json
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "workloadProfileName": {
      "value": "Consumption"
    },
    "workloadProfileType": {
      "value": "Consumption"
    }
  }
}
```

For **Dedicated plan**, use:

```json
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "workloadProfileName": {
      "value": "dedicated-d4"
    },
    "workloadProfileType": {
      "value": "D4"
    },
    "workloadProfileMinimumCount": {
      "value": 1
    },
    "workloadProfileMaximumCount": {
      "value": 3
    }
  }
}
```

### Option 2: Environment Variables (Simple)

Set environment variables before running `azd up`:

#### For Consumption (Default - Free Tier Eligible)
```powershell
# No action needed - this is the default
azd up
```

#### For Dedicated Plan
```powershell
$env:AZURE_CONTAINER_APPS_WORKLOAD_PROFILE_NAME = "dedicated-d4"
$env:AZURE_CONTAINER_APPS_WORKLOAD_PROFILE_TYPE = "D4"
azd up
```

### Option 3: Post-Deployment Modification via Azure CLI

You can change the plan after deployment:

#### Check Current Plan
```bash
az containerapp env show \
  --name <environment-name> \
  --resource-group <resource-group> \
  --query "properties.workloadProfiles"
```

#### Add a Dedicated Workload Profile
```bash
az containerapp env workload-profile add \
  --name <environment-name> \
  --resource-group <resource-group> \
  --workload-profile-name "dedicated-d4" \
  --workload-profile-type "D4" \
  --min-nodes 1 \
  --max-nodes 3
```

#### Assign Container App to Workload Profile
```bash
az containerapp update \
  --name budget-api \
  --resource-group <resource-group> \
  --workload-profile-name "dedicated-d4"
```

### Option 4: Custom Bicep Templates (Advanced)

If you need full control, create custom Bicep templates:

#### Create `Budget.AppHost\infra\main.bicep`

```bicep
targetScope = 'resourceGroup'

@description('The location for resources')
param location string = resourceGroup().location

@description('Workload profile name')
param workloadProfileName string = 'Consumption'

@description('Workload profile type (Consumption, D4, D8, D16, D32, E4, E8, E16, E32)')
param workloadProfileType string = 'Consumption'

@description('Minimum node count for dedicated profiles')
param workloadProfileMinCount int = 1

@description('Maximum node count for dedicated profiles')
param workloadProfileMaxCount int = 3

// Container Apps Environment
resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: 'cae-${uniqueString(resourceGroup().id)}'
  location: location
  properties: {
    workloadProfiles: workloadProfileType == 'Consumption' ? [
      {
        name: 'Consumption'
        workloadProfileType: 'Consumption'
      }
    ] : [
      {
        name: 'Consumption'
        workloadProfileType: 'Consumption'
      }
      {
        name: workloadProfileName
        workloadProfileType: workloadProfileType
        minimumCount: workloadProfileMinCount
        maximumCount: workloadProfileMaxCount
      }
    ]
  }
}

// Budget API Container App
resource budgetApiContainerApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'budget-api'
  location: location
  properties: {
    environmentId: containerAppsEnvironment.id
    workloadProfileName: workloadProfileName
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
      }
    }
    template: {
      containers: [
        {
          name: 'budget-api'
          image: 'mcr.microsoft.com/dotnet/samples:aspnetapp' // Placeholder
        }
      ]
    }
  }
}

// Budget Web Container App
resource budgetWebContainerApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'budget'
  location: location
  properties: {
    environmentId: containerAppsEnvironment.id
    workloadProfileName: workloadProfileName
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
      }
    }
    template: {
      containers: [
        {
          name: 'budget-web'
          image: 'mcr.microsoft.com/dotnet/samples:aspnetapp' // Placeholder
        }
      ]
    }
  }
}
```

**?? Warning**: Using custom Bicep templates breaks Aspire's automatic deployment. Only use this if you need very specific infrastructure configuration.

## Workload Profile Types and Pricing

### Consumption (Default)
- **Type**: `Consumption`
- **Cost**: Pay per second of execution
- **Free Tier**: 180,000 vCPU-seconds + 360,000 GiB-seconds per month
- **Best For**: Development, testing, low-traffic production apps

### Dedicated Profiles
| Profile | vCPU | Memory | Cost (Approximate) |
|---------|------|--------|-------------------|
| D4      | 4    | 8 GiB  | ~$0.20/hour       |
| D8      | 8    | 16 GiB | ~$0.40/hour       |
| D16     | 16   | 32 GiB | ~$0.80/hour       |
| D32     | 32   | 64 GiB | ~$1.60/hour       |
| E4      | 4    | 16 GiB | ~$0.26/hour       |
| E8      | 8    | 32 GiB | ~$0.52/hour       |
| E16     | 16   | 64 GiB | ~$1.04/hour       |
| E32     | 32   | 128 GiB| ~$2.08/hour       |

**Best For**: Production apps with predictable traffic, apps requiring guaranteed resources

## Recommendation for Your Budget App

For development and low-traffic production:
? **Use Consumption plan** (default)
- Free tier covers most development usage
- Automatically scales to zero when not in use
- No infrastructure costs when idle

For production with consistent traffic:
? **Use Dedicated D4 or D8**
- Predictable costs
- Better performance for user-facing apps
- No cold starts

## How to Verify Current Plan

```bash
# Check your Container Apps Environment
az containerapp env show \
  --name <environment-name> \
  --resource-group rg-BudgetApp2 \
  --query "{name:name, workloadProfiles:properties.workloadProfiles}" \
  --output table

# Check specific Container App
az containerapp show \
  --name budget-api \
  --resource-group rg-BudgetApp2 \
  --query "{name:name, workloadProfile:properties.workloadProfileName}" \
  --output table
```

## Default Behavior

**Current Setup**: Your Aspire deployment uses the **Consumption plan** (default)
- Free tier eligible
- Pay only for execution time
- Best for your current development/testing needs

No changes needed unless you want to switch to Dedicated for production!
