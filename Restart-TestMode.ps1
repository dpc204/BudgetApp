# Complete Restart Script for Test Mode

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "?? Restarting Budget.Web in TEST MODE" -ForegroundColor Yellow
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Stop any running instances
Write-Host "1. Stopping any running Budget.Web instances..." -ForegroundColor Yellow
Stop-Process -Name "Budget.Web" -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# Step 2: Clean and rebuild
Write-Host "2. Building Budget.Web..." -ForegroundColor Yellow
dotnet build Budget.Web\Budget.Web.csproj

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "? Build failed!" -ForegroundColor Red
    Write-Host "Please check the error messages above." -ForegroundColor Red
    exit 1
}

Write-Host "? Build successful!" -ForegroundColor Green
Write-Host ""

# Step 3: Set environment and start
Write-Host "3. Starting in TEST MODE..." -ForegroundColor Yellow
$env:USE_TEST_AUTH = "true"
$env:ASPNETCORE_ENVIRONMENT = "Development"

Write-Host ""
Write-Host "? USE_TEST_AUTH = true" -ForegroundColor Green
Write-Host "? Mock authentication enabled" -ForegroundColor Green
Write-Host "? No Entra ID required" -ForegroundColor Green
Write-Host "? No ForwardAuthCookiesHandler" -ForegroundColor Green
Write-Host "? No TokenCacheValidationMiddleware" -ForegroundColor Green
Write-Host ""
Write-Host "Test User:" -ForegroundColor Cyan
Write-Host "  Name:  Test User" -ForegroundColor White
Write-Host "  Email: testuser@example.com" -ForegroundColor White
Write-Host "  ID:    test-user-id-12345" -ForegroundColor White
Write-Host ""
Write-Host "Expected output:" -ForegroundColor Yellow
Write-Host "  ?? TEST MODE: Using mock authentication instead of Entra ID" -ForegroundColor Yellow
Write-Host "  ? Test mode authentication configured with controllers" -ForegroundColor Yellow
Write-Host "  Now listening on: http://localhost:XXXX" -ForegroundColor Yellow
Write-Host ""
Write-Host "Starting..." -ForegroundColor Cyan
Write-Host "Press Ctrl+C to stop" -ForegroundColor Gray
Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

Set-Location Budget.Web
dotnet run
