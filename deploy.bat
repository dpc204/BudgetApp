@echo off
SETLOCAL ENABLEDELAYEDEXPANSION
REM adddomain.bat - runs hardcoded domain binding script for Container App
REM Adjust the hardcoded values inside infra\Bind-CustomDomains-Hardcoded.ps1 if anything changes.
azd up
set SCRIPT_DIR=%~dp0

echo Executing hardcoded custom domain bindings...
echo Scriptdir %SCRIPT_DIR%
powershell -NoLogo -NoProfile -ExecutionPolicy Bypass -File "C:\Repos\BudgetApp\Budget.AppHost\infra\Bind-CustomDomains-Hardcoded.ps1"
if errorlevel 1 (
  echo Custom domain binding FAILED.
  exit /b 1
)
echo Custom domain binding SUCCEEDED.

powershell -NoLogo -NoProfile -ExecutionPolicy Bypass -File "C:\Repos\BudgetApp\scripts\Update-EntraRedirectUris-AzCli.ps1" -Environment BudgetApp2
if errorlevel 1 (
  echo Entra Redirect URI Binding FAILED.
  exit /b 1
)
echo Entra Redirect URI Binding SUCCEEDED.
date
ENDLOCAL
