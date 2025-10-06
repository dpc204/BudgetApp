var builder = DistributedApplication.CreateBuilder(args);

// API project (expose external endpoints so it can be reached directly in local dev / Azure)
var api = builder.AddProject<Projects.Budget_Api>("budget-api")
                 .WithExternalHttpEndpoints();

// Web app hosts the Blazor WASM client (Budget.Client is a project reference in Budget.Web)
// Provide the API base URL via environment variable
var web = builder.AddProject<Projects.Budget_Web>("budget")
                 .WithReference(api)
                 .WithEnvironment("BUDGET_API_BASE_URL", api.GetEndpoint("https"))
                 .WithExternalHttpEndpoints();

// Configure API to allow CORS from the web app's origin
api.WithEnvironment("ALLOWED_ORIGINS", web.GetEndpoint("https"));

builder.Build().Run();