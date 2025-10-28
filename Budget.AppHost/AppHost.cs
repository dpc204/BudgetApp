using Aspire.Hosting.Azure;

var builder = DistributedApplication.CreateBuilder(args);

// Define the Blazor Server app and expose an external HTTP endpoint so it can be deployed to Container Apps
builder.AddProject<Projects.Budget_Web>("budget-web")
 .WithExternalHttpEndpoints();

builder.Build().Run();
