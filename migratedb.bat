@echo 

if "%2" == "" goto paramerror

if %1 == local goto local
if %1 == azure goto azure
echo Use update local|azure
goto end

:local 
set LocalBudgetConnection=Data Source=(localdb)\MSSQLLocalDB;Database=BudgetDB;Trusted_Connection=True;TrustServerCertificate=True
goto run

:azure
set LocalBudgetConnection=Data Source=fantumsqlserver.database.windows.net;Initial Catalog=shisaDB;User ID=dpc;Password=Fred1$HugoMarisaConnelly;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False"

:run
dotnet ef migrations add %2  --project BudgetApp/Budget.DB   --startup-project BudgetApp/Budget.Web   --context Budget.DB.BudgetContext

:end
goto eof

:paramerror
Echo !!!!!!!!!!!!!! You must use the ADD parameter and give the migration a name







