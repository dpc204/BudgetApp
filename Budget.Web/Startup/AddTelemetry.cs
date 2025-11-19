using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Budget.Web.Startup
{
  public class AddTelemetry
  {
    public static void ConfigureTelemetryAndServiceDefaults(WebApplicationBuilder builder)
    {
      builder.Logging.AddOpenTelemetry(logging =>
      {
        logging.IncludeFormattedMessage = true;
        logging.IncludeScopes = true;
      });

      builder.Services.AddOpenTelemetry()
        .WithMetrics(metrics =>
        {
          metrics.AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation();
        })
        .WithTracing(tracing =>
        {
          tracing.AddSource(builder.Environment.ApplicationName)
            .AddAspNetCoreInstrumentation(o =>
            {
              o.Filter = ctx =>
                !ctx.Request.Path.StartsWithSegments("/health") && !ctx.Request.Path.StartsWithSegments("/alive");
            })
            .AddHttpClientInstrumentation();
        });

      var otlpEndpoint =
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]; // Aspire sets this when dashboard collects
      if (!string.IsNullOrWhiteSpace(otlpEndpoint))
      {
        builder.Services.AddOpenTelemetry().UseOtlpExporter();
      }

      builder.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy(), new[] { "live" });
      builder.Services.AddServiceDiscovery();
      builder.Services.ConfigureHttpClientDefaults(http =>
      {
        http.AddStandardResilienceHandler();
        http.AddServiceDiscovery();
      });
    }
  }
}