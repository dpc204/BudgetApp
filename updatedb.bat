echo %1

echo on

if %1 == local goto local
if %1 == azure goto azure
echo Use update local|azure
goto eof

:local 
set LocalBudgetConnection=Data Source=(localdb)\MSSQLLocalDB;Database=BudgetDB;Trusted_Connection=True;TrustServerCertificate=True
goto run

:azure
set LocalBudgetConnection=Data Source=fantumsqlserver.database.windows.net;Database=BudgetDB;User ID=dpc;Password=Fred1$HugoMarisaConnelly;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False;Command Timeout=30

:run
dotnet ef  database update   --project Budget.DB   --startup-project Budget.Web   --context Budget.DB.BudgetContext


:eof
set LocalBudgetConnection=
