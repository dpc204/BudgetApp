if %1 == local goto local
if %1 == azure goto azure
echo Use update local|azure
goto eof

:local 
set LocalBudgetConnection=Data Source=(localdb)\MSSQLLocalDB;Database=BudgetDB;Trusted_Connection=True;TrustServerCertificate=True
goto run

:azure
set LocalBudgetConnection=Data Source=fantumsqlserver.database.windows.net;Initial Catalog=shisaDB;User ID=dpc;Password=Fred1$HugoMarisaConnelly;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False"

:run
dotnet ef database update   --project BudgetApp/Budget.DB   --startup-project BudgetApp/Budget.Web   --context Budget.DB.BudgetContext


set LocalBudgetConnection=Data Source=(localdb)\MSSQLLocalDB;Database=BudgetDB;Trusted_Connection=True;TrustServerCertificate=True
dotnet ef database update   --project BudgetApp/Budget.DB   --startup-project BudgetApp/Budget.Web   --context Budget.DB.BudgetContext   
