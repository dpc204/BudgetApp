var builder = DistributedApplication.CreateBuilder(args);

// API project (expose external endpoints so it can be reached directly in local dev / Azure)
var api = builder.AddProject<Projects.Budget_Api>("budget-api")
                 .WithExternalHttpEndpoints();

// Web app (server-side Blazor only)
var web = builder.AddProject<Projects.Budget_Web>("budget")
                 .WithReference(api)
                 .WithEnvironment("BUDGET_API_BASE_URL", api.GetEndpoint("https"))
                 .WithExternalHttpEndpoints();

// Configure API to allow CORS from the web app's origin (may be trimmed later since no WASM)
api.WithEnvironment("ALLOWED_ORIGINS", web.GetEndpoint("https"));

builder.Build().Run();