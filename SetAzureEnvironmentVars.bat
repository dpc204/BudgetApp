az containerapp update   --name budget   --resource-group rg-BudgetApp2  --set-env-vars BUDGET_API_URL=https://budget-api.delightfulsea-3ea5a8ad.eastus.azurecontainerapps.io

az containerapp update   --name budget-api   --resource-group rg-BudgetApp2  --set-env-vars ALLOWED_ORIGINS=https://budget.delightfulsea-3ea5a8ad.eastus.azurecontainerapps.io