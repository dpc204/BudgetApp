using Microsoft.Extensions.Caching.Hybrid;

namespace Budget.Web.Startup;

public static class ConfigureHybridCache
{
  public static void ConfigureServices(WebApplicationBuilder builder)
  {
    builder.Services.AddHybridCache(options =>
    {
      options.DefaultEntryOptions = new HybridCacheEntryOptions() {
        Expiration = TimeSpan.FromMinutes(10),
        Flags = HybridCacheEntryFlags.DisableDistributedCache
      };
    });
  }
}