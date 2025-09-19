using System;
using System.Diagnostics;
using System.Reflection;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Azure.Extensions.AspNetCore.Configuration.Secrets;

namespace Budget.Shared;

public static class Misc
{
  public enum ConnectionStringType
  {
    Budget,
    Identity
  }

  public static string SetupConfigurationSources(IConfigurationBuilder configBuilder, IConfiguration configuration,
    Assembly assembly, ConnectionStringType connectionStringType)
  {
    var connectionType = connectionStringType.ToString();
    configBuilder.AddJsonFile("appsettings.json");
    configBuilder.AddUserSecrets(assembly);
    configBuilder.AddEnvironmentVariables();

    configBuilder.AddAzureKeyVault(
      new Uri("https://fantumkeyvault.vault.azure.net/"),
      new DefaultAzureCredential());

    string? s;
    if (configuration[$"Local{connectionType}Connection"] != null)
    {
      s = configuration[$"Local{connectionType}Connection"];
    }
    else if (configuration[$"{connectionType}connection"] != null)
    {
      // For local dev, fall back to regular connection string if LocalBudgetConnection is not set
      s = configuration[$"{connectionType}connection"];
    }
    else
    {
      // For deployed environments, fall back to regular connection string if LocalBudgetConnection is not set
      s = configuration.GetConnectionString($"{connectionType}connection");
    }

    if (string.IsNullOrWhiteSpace(s))
      s = configuration[$"{connectionType}connection"] ??
          throw new InvalidOperationException($"Connection string '{connectionType}Connection' not found.");

    Console.WriteLine("Console ConnectionString:" + s);

    ArgumentNullException.ThrowIfNull(s, connectionType);

    Debug.WriteLine("DEBUG " + s);
    Console.WriteLine("Console " + s);
    return s;
  }
}