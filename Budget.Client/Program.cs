using System.Diagnostics;
using Budget.Client;
using Budget.Client.Services;
using Budget.Shared.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Syncfusion.Blazor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

Console.WriteLine($"Console Program running");

// Register EnvelopeState service
builder.Services.AddScoped<EnvelopeState>();

// Register HTTP logging handler
builder.Services.AddTransient<LoggingHttpHandler>();

var test = builder.Configuration["BUDGET_API_BASE_URL"];

Console.WriteLine($"BUDGET_API_BASE_URL from config: {test}");

// Register HttpClient for API calls with logging
var apiBaseUrl = builder.Configuration["BUDGET_API_BASE_URL"] ?? builder.HostEnvironment.BaseAddress;
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
