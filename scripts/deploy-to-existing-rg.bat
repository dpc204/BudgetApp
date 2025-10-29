@echo off
setlocal

REM Configure azd to target existing resource group and deploy the Aspire app to the existing Container App
if "%AZURE_SUBSCRIPTION_ID%"=="" (
 echo AZURE_SUBSCRIPTION_ID is not set. Set it to your subscription ID.
 exit /b1
)

set AZURE_RESOURCE_GROUP=rg-BudgetApp2

REM Optional: set region to match RG if needed
if "%AZURE_LOCATION%"=="" set AZURE_LOCATION=eastus

REM Ensure env exists and is pointed to the right subscription and RG
azd env new budget --subscription %AZURE_SUBSCRIPTION_ID% --location %AZURE_LOCATION% --no-prompt2>nul
azd env set AZURE_SUBSCRIPTION_ID %AZURE_SUBSCRIPTION_ID%
azd env set AZURE_RESOURCE_GROUP %AZURE_RESOURCE_GROUP%
azd env set AZURE_LOCATION %AZURE_LOCATION%

REM Deploy code only (no provisioning) to reuse existing resources
azd deploy --no-prompt

endlocal
