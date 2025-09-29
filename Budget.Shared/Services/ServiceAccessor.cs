using Microsoft.Extensions.DependencyInjection;

namespace Budget.Shared.Services;

public static class ServiceAccessor
{
  public static IServiceProvider? Services { get; private set; }
  public static void Configure(IServiceProvider services) => Services = services;

  public static T GetRequiredService<T>() where T : notnull
  {
    if (Services == null)
      throw new InvalidOperationException("ServiceAccessor not initialized. Call ServiceAccessor.Configure in Program.cs after building the host.");
    return Services.GetRequiredService<T>();
  }
}
