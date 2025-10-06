using System.Diagnostics;
using Budget.Client;
using Budget.Client.Services;
using Budget.Shared.Services;
using Budget.Shared.Models;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Syncfusion.Blazor;
using System.Net.Http.Json;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

Console.WriteLine($"Console Program running");

// Fetch configuration from server
var tempClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
ClientConfiguration? serverConfig = null;

try
{
    serverConfig = await tempClient.GetFromJsonAsync<ClientConfiguration>("api/client-config");
    Console.WriteLine($"Fetched server config - BUDGET_API_BASE_URL: {serverConfig?.BudgetApiBaseUrl}");
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to fetch server config: {ex.Message}");
}

// Register EnvelopeState service
builder.Services.AddScoped<EnvelopeState>();

// Register HTTP logging handler
builder.Services.AddTransient<LoggingHttpHandler>();



// Register HttpClient for API calls with logging
// Priority: server config > appsettings.json > host base address
var apiBaseUrl = serverConfig?.BudgetApiBaseUrl 
                ?? builder.Configuration["BUDGET_API_BASE_URL"] 
                ?? builder.HostEnvironment.BaseAddress;
Console.WriteLine($"url:{apiBaseUrl}");





builder.Services.AddScoped(sp =>
{
  var handler = sp.GetRequiredService<LoggingHttpHandler>();
  handler.InnerHandler = new HttpClientHandler();
  var client = new HttpClient(handler) { BaseAddress = new Uri(apiBaseUrl) };
  return client;
});

// Register API client services
builder.Services.AddScoped<IBudgetApiClient, BudgetApiClient>();
builder.Services.AddScoped<IBudgetMaintApiClient, BudgetMaintApiClient>();

// Register Syncfusion license and services
Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(
  "Ngo9BigBOggjGyl/Vkd+XU9FcVRDX3xKf0x/TGpQb19xflBPallYVBYiSV9jS3tTf0VkW35ecHFcRGdeUk91Xg==");
builder.Services.AddSyncfusionBlazor();

// Configure logging to see HTTP traces in browser console
builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Logging.AddFilter("Budget.Client.Services.LoggingHttpHandler", LogLevel.Information);

Console.WriteLine($"RunAsync Next");
await builder.Build().RunAsync();
