var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Budget_Web>("budget");

builder.Build().Run();