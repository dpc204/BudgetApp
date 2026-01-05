var builder = DistributedApplication.CreateBuilder(args);

// Get environment variables for Azure deployment
var useAzureDb = builder.Configuration["USE_AZURE_DB"] ?? "false";
var aspnetEnv = builder.Configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development";
var azureAdTenantId = builder.Configuration["AZURE_AD_TENANT_ID"] ?? "";
var azureAdClientId = builder.Configuration["AZURE_AD_CLIENT_ID"] ?? "";
var storageAccountName = builder.Configuration["AZURE_STORAGE_ACCOUNT_NAME"] ?? "";
var storageBlobEndpoint = builder.Configuration["AZURE_STORAGE_BLOB_ENDPOINT"] ?? "";
var storageTableEndpoint = builder.Configuration["AZURE_STORAGE_TABLE_ENDPOINT"] ?? "";
var managedIdentityClientId = builder.Configuration["MANAGED_IDENTITY_CLIENT_ID"] ?? "";

// Define the Budget API service with service discovery and environment configuration
var budgetApi = builder.AddProject<Projects.Budget_Api>("budget-api")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", aspnetEnv)
    .WithEnvironment("AzureStorage__AccountName", storageAccountName)
    .WithEnvironment("AzureStorage__BlobEndpoint", storageBlobEndpoint)
    .WithEnvironment("AzureStorage__TableEndpoint", storageTableEndpoint)
    .WithExternalHttpEndpoints();

// Define the Blazor Server app with environment configuration
// Note: Redis is configured manually via docker-compose.yml for local dev
// Azure deployment uses SQL Server distributed cache (zero cost)
builder.AddProject<Projects.Budget_Web>("budget")
    .WithReference(budgetApi)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", aspnetEnv)
    .WithEnvironment("AZURE_CLIENT_ID", managedIdentityClientId)
    .WithExternalHttpEndpoints();

builder.Build().Run();
