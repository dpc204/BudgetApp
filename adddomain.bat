@echo off
SETLOCAL ENABLEDELAYEDEXPANSION
REM adddomain.bat - runs hardcoded domain binding script for Container App
REM Adjust the hardcoded values inside infra\Bind-CustomDomains-Hardcoded.ps1 if anything changes.
azd up
set SCRIPT_DIR=%~dp0

echo Executing hardcoded custom domain bindings...
powershell -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%\BudgetApp.AppHost\infra\Bind-CustomDomains-Hardcoded.ps1"
if errorlevel 1 (
  echo Custom domain binding FAILED.
  exit /b 1
)

echo Custom domain binding SUCCEEDED.
ENDLOCAL
