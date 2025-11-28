# Azure Deployment Configuration Guide

## Overview
This document explains how to configure the Budget application for deployment to Azure Container Apps.

## Required Environment Variables

### Budget.Api (API Service)

**No required environment variables** - The API will work with default settings.

#### Optional - CORS Configuration
- **Variable**: `ALLOWED_ORIGINS`
- **Description**: Comma-separated list of allowed origins for CORS
- **Example**: `https://budget-xxxxx.azurecontainerapps.io,https://yourdomain.com`
- **Default Behavior**: If not set, the API will automatically allow:
  - `*.azurecontainerapps.io` domains
  - `*.azurewebsites.net` domains
  - `localhost` for testing
  - Custom domains specified in `CUSTOM_DOMAIN` variable

#### Optional - Custom Domain
- **Variable**: `CUSTOM_DOMAIN`
- **Description**: Comma-separated list of custom domains to allow for CORS
- **Example**: `yourdomain.com,www.yourdomain.com`
- **Note**: Only used when `ALLOWED_ORIGINS` is not set

### Budget.Web (Blazor Web App)

#### ?? REQUIRED - API URL Configuration
- **Variable**: `BUDGET_API_URL`
- **Description**: The full URL to the Budget.Api service in Azure
- **Example**: `https://budget-api-xxxxx.azurecontainerapps.io`
- **?? CRITICAL**: This **MUST** be set in Azure deployments or the web app will try to use service discovery and fail
- **How to find the URL**: 
  1. Go to Azure Portal
  2. Find your Budget.Api container app
  3. Copy the "Application Url" (looks like `https://budget-api-xxxxx.azurecontainerapps.io`)
  4. Set this as the `BUDGET_API_URL` environment variable in your Budget.Web container app

## Troubleshooting

### Issue: Still seeing "https://budget-api/..." in traces instead of full Azure URL
**Root Cause**: The `BUDGET_API_URL` environment variable is not set in Budget.Web container app.

**Solution**: 
1. Find your Budget.Api container app URL in Azure Portal (Application Url)
2. Set the environment variable in Budget.Web:
   ```bash
   az containerapp update \
     --name budget \
     --resource-group <your-resource-group> \
     --set-env-vars BUDGET_API_URL=https://budget-api-xxxxx.azurecontainerapps.io
   ```
3. Verify it's set by checking the container app's environment variables in the portal
4. The change requires a new revision - Azure will automatically create one
5. Check the logs to confirm the full URL is now being used

### Issue: "ALLOWED_ORIGINS environment variable must be set in production"
**Solution**: This error should no longer occur with the updated CORS configuration. The API now has sensible defaults for Azure deployments.

### Issue: Budget.Web cannot connect to Budget.Api
**Solution**: 
1. Verify `BUDGET_API_URL` is set correctly in Budget.Web container app
2. Ensure the URL points to the correct Budget.Api container app
3. Check that both services are in the same Container Apps environment for internal networking
4. Verify both container apps are running (check the Azure Portal)

### Issue: CORS errors when accessing the API
**Solution**:
1. Set `ALLOWED_ORIGINS` explicitly in Budget.Api with the Budget.Web URL
2. Or ensure the automatic detection is working (check that URLs end with `.azurecontainerapps.io`)
3. Check the Budget.Api logs for CORS-related errors
