if %1 == local goto local
if %1 == azure goto azure
echo Use update local|azure
goto end

:local 
set LocalBudgetConnection=Data Source=(localdb)\MSSQLLocalDB;Database=AuthDB;Trusted_Connection=True;TrustServerCertificate=True
goto run

:azure
set LocalBudgetConnection=Data Source=fantumsqlserver.database.windows.net;Initial Catalog=AuthDB;User ID=dpc;Password=Fred1$HugoMarisaConnelly;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False"

:run
dotnet ef database update   --project Budget.Web   --startup-project Budget.Web   --context Budget.Data.ApplicationDbContext   

:end