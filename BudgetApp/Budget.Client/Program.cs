using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Budget.Shared.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();
builder.Services.AddScoped<EnvelopeState>();

// TODO: Add HttpClient for API calls when wiring up EnvelopeState.RefreshAsync()
builder.Services.AddScoped(sp => new HttpClient (){ BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Envelope state container for WASM
builder.Services.AddScoped<EnvelopeState>();

await builder.Build().RunAsync();
