using Aspire.Hosting.Azure;

var builder = DistributedApplication.CreateBuilder(args);

// Define the Budget API service with service discovery
var budgetApi = builder.AddProject<Projects.Budget_Api>("budget-api")
    .WithExternalHttpEndpoints();

// Define the Blazor Server app and expose an external HTTP endpoint
// Configure it to reference the budget-api service for HTTP calls
builder.AddProject<Projects.Budget_Web>("budget")
    .WithReference(budgetApi)
    .WithExternalHttpEndpoints();

builder.Build().Run();
