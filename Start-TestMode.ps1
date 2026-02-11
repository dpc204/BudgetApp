# Start Budget.Web in Test Mode for Playwright Tests

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "?? Starting Budget.Web in TEST MODE" -ForegroundColor Yellow
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Set environment variable to enable test authentication
$env:USE_TEST_AUTH = "true"
$env:ASPNETCORE_ENVIRONMENT = "Development"

Write-Host "? USE_TEST_AUTH = true" -ForegroundColor Green
Write-Host "? Mock authentication enabled" -ForegroundColor Green
Write-Host "? No Entra ID required" -ForegroundColor Green
Write-Host "? No ForwardAuthCookiesHandler (no ITokenAcquisition needed)" -ForegroundColor Green
Write-Host ""
Write-Host "Test User Credentials:" -ForegroundColor Cyan
Write-Host "  Name:  Test User" -ForegroundColor White
Write-Host "  Email: testuser@example.com" -ForegroundColor White
Write-Host "  ID:    test-user-id-12345" -ForegroundColor White
Write-Host ""
Write-Host "You should see:" -ForegroundColor Yellow
Write-Host "  ?? TEST MODE: Using mock authentication instead of Entra ID" -ForegroundColor Yellow
Write-Host ""
Write-Host "Starting Budget.Web..." -ForegroundColor Cyan
Write-Host "Press Ctrl+C to stop" -ForegroundColor Gray
Write-Host ""

# Navigate to Budget.Web and start it
Set-Location Budget.AppHost
dotnet run
