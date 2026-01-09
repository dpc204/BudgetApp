using Aspire.Hosting.Azure;

var builder = DistributedApplication.CreateBuilder(args);

// Add Azure Container Apps Environment (required for PublishAsAzureContainerApp)
// WithAzdResourceNaming() makes it reference the existing environment created by azd
var cae = builder.AddAzureContainerAppEnvironment("cae")
    .WithAzdResourceNaming();

// Get environment variables for Azure deployment
var useAzureDb = builder.Configuration["UseAzureDB"] ?? "false";
var aspnetEnv = builder.Configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development";
var storageAccountName = builder.Configuration["AZURE_STORAGE_ACCOUNT_NAME"] ?? "";
var storageBlobEndpoint = builder.Configuration["AZURE_STORAGE_BLOB_ENDPOINT"] ?? "";
var storageTableEndpoint = builder.Configuration["AZURE_STORAGE_TABLE_ENDPOINT"] ?? "";
var managedIdentityClientId = builder.Configuration["MANAGED_IDENTITY_CLIENT_ID"] ?? "";
var keyVaultEndpoint = builder.Configuration["AZURE_KEY_VAULT_ENDPOINT"] ?? "https://kv-ujhsryhos5xlq.vault.azure.net/";

// Log configuration values for debugging
Console.WriteLine($"=== AppHost Configuration ===");
Console.WriteLine($"UseAzureDB: {useAzureDb}");
Console.WriteLine($"ASPNETCORE_ENVIRONMENT: {aspnetEnv}");
Console.WriteLine($"MANAGED_IDENTITY_CLIENT_ID: {managedIdentityClientId}");
Console.WriteLine($"AZURE_KEY_VAULT_ENDPOINT: {keyVaultEndpoint}");
Console.WriteLine($"AZURE_STORAGE_ACCOUNT_NAME: {storageAccountName}");
Console.WriteLine($"AZURE_STORAGE_BLOB_ENDPOINT: {storageBlobEndpoint}");
Console.WriteLine($"AZURE_STORAGE_TABLE_ENDPOINT: {storageTableEndpoint}");

// Define the Budget API service with service discovery and environment configuration
var budgetApi = builder.AddProject<Projects.Budget_Api>("budget-api")
  .WithEnvironment("UseAzureDB", useAzureDb)
  .WithEnvironment("ASPNETCORE_ENVIRONMENT", aspnetEnv)
  .WithEnvironment("AZURE_CLIENT_ID", managedIdentityClientId)
  .WithEnvironment("KeyVault__Uri", keyVaultEndpoint)
  .WithEnvironment("AZURE_STORAGE_ACCOUNT_NAME", storageAccountName)
  .WithEnvironment("AZURE_STORAGE_BLOB_ENDPOINT", storageBlobEndpoint)
  .WithEnvironment("AZURE_STORAGE_TABLE_ENDPOINT", storageTableEndpoint)
  .WithExternalHttpEndpoints()
  .PublishAsAzureContainerApp((infrastructure, app) =>
  {
    // Scale to zero when idle, max 10 replicas, 600s cooldown
    app.Template.Scale.MinReplicas = 0;
    app.Template.Scale.MaxReplicas = 10;
    app.Template.Scale.CooldownPeriod = 600;
  });

// Define the Blazor Server app with environment configuration
// Note: Redis is configured manually via docker-compose.yml for local dev
// Azure deployment uses SQL Server distributed cache (zero cost)
// Azure AD configuration (ClientId, ClientSecret, TenantId) is loaded from Key Vault automatically
builder.AddProject<Projects.Budget_Web>("budget")
  .WithReference(budgetApi)
  .WithEnvironment("UseAzureDB", useAzureDb)
  .WithEnvironment("ASPNETCORE_ENVIRONMENT", aspnetEnv)
  .WithEnvironment("AZURE_CLIENT_ID", managedIdentityClientId)
  .WithEnvironment("KeyVault__Uri", keyVaultEndpoint)
  .WithExternalHttpEndpoints()
  .PublishAsAzureContainerApp((infrastructure, app) =>
  {
    // Scale to zero when idle, max 10 replicas, 600s cooldown
    app.Template.Scale.MinReplicas = 0;
    app.Template.Scale.MaxReplicas = 10;
    app.Template.Scale.CooldownPeriod = 600;
  });

builder.Build().Run();