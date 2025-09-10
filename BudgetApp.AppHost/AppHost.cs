var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Budget_Web>("budget")
       .WithExternalHttpEndpoints();

builder.AddProject<Projects.Budget_Api>("budget-api");

builder.Build().Run();