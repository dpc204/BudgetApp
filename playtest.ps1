cd \repos\budgetapp
# Kill old process, rebuild, and start in test mode
Stop-Process -Name "Budget.AppHost" -Force -ErrorAction SilentlyContinue
Stop-Process -Name "Budget.Web" -Force -ErrorAction SilentlyContinue
Stop-Process -Name "Budget.Api" -Force -ErrorAction SilentlyContinue
dotnet build Budget.Web
$env:USE_TEST_AUTH = "true"
cd Budget.Web
dotnet run


