

set LocalBudgetConnection=Data Source=(localdb)\MSSQLLocalDB;Database=BudgetDB;Trusted_Connection=True;TrustServerCertificate=True
dotnet ef database update   --project BudgetApp/Budget.DB   --startup-project BudgetApp/Budget.Web   --context Budget.DB.BudgetContext   