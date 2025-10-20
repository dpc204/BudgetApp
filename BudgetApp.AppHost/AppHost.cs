var builder = DistributedApplication.CreateBuilder(args);

// Web app (server-side Blazor only). Host API in the same process.
var web = builder.AddProject<Projects.Budget_Web>("budget")
                 .WithExternalHttpEndpoints();

// Point API base URL at the web app itself (same-process hosting)
web.WithEnvironment("BUDGET_API_BASE_URL", web.GetEndpoint("https"));

builder.Build().Run();