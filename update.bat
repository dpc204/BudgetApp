

set LocalBudgetConnection=Data Source=(localdb)\MSSQLLocalDB;Database=BudgetDB;Trusted_Connection=True;TrustServerCertificate=True
dotnet ef database update   --project Budget.DB   --startup-project Budget.Web   --context Budget.DB.BudgetContext   