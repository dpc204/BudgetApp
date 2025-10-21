if %1 == local goto local
if %1 == azure goto azure
echo Use update local|azure
goto end

:local 
set LocalIdentityConnection=Data Source=(localdb)\MSSQLLocalDB;Database=BudgetDB;Trusted_Connection=True;TrustServerCertificate=True
goto run

:azure
set LocalIdentityConnection=Data Source=fantumsqlserver.database.windows.net;Initial Catalog=BudgetDB;User ID=dpc;Password=Fred1$HugoMarisaConnelly;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False

:run
dotnet ef database update   --project Budget.Web   --startup-project Budget.Web   --context IdentityDBContext   

set LocalIdentityConnection=

:end