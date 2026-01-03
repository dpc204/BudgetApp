var builder = DistributedApplication.CreateBuilder(args);

// Define the Budget API service with service discovery
var budgetApi = builder.AddProject<Projects.Budget_Api>("budget-api")
    .WithExternalHttpEndpoints();

// Define the Blazor Server app
// Note: Redis is configured manually via docker-compose.yml for local dev
// Azure deployment uses SQL Server distributed cache (zero cost)
builder.AddProject<Projects.Budget_Web>("budget")
    .WithReference(budgetApi)
    .WithExternalHttpEndpoints();

builder.Build().Run();
