using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Fantum.Mediator;

public static class FantumMediator
{
  public static IServiceCollection AddFantumMediator(this IServiceCollection services, Assembly? assembly = null)
  {
    assembly ??= Assembly.GetCallingAssembly();

    services.AddScoped<ISender, Sender>();

    var handlerInterfaceType = typeof(IRequestHandler<,>);

    var handlerTypes = assembly
        .GetTypes()
        .Where(type => !type.IsAbstract && !type.IsInterface)
        .SelectMany(type => type.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType)
            .Select(i => new { Interface = i, Implementation = type }));

    foreach(var handler in handlerTypes)
    {
      services.AddScoped(handler.Interface, handler.Implementation);
    }

    return services;
  }
}
