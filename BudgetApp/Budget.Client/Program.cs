using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Budget.Shared.Services;
using Budget.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();

// Shared state container
builder.Services.AddScoped<EnvelopeState>();

// HttpClient configured for API calls (adjust BaseAddress if API hosted elsewhere)
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// API client abstraction
builder.Services.AddScoped<IBudgetApiClient, BudgetApiClient>();

await builder.Build().RunAsync();
