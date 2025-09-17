using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Budget.Shared.Services;
using Budget.Client.Services;
using Budget.DTO;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();

// Shared state container
builder.Services.AddScoped<EnvelopeState>();

// Resolve API base URL from configuration/environment (injected by Aspire AppHost)
var apiBase = builder.Configuration["BUDGET_API_BASE_URL"];
if (string.IsNullOrWhiteSpace(apiBase))
{
    apiBase = builder.HostEnvironment.BaseAddress; // fallback
}

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBase) });

// API client abstraction
builder.Services.AddScoped<Budget.DTO.IBudgetApiClient, Budget.Client.Services.BudgetApiClient>();

await builder.Build().RunAsync();
