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

    // Add Azure Key Vault with error handling
    try
    {
      configBuilder.AddAzureKeyVault(
        new Uri("https://fantumkeyvault.vault.azure.net/"),
        new DefaultAzureCredential());
    }
    catch (Exception ex)
    {
      // Log the exception but don't fail startup in development
      Debug.WriteLine($"Azure Key Vault access failed: {ex.Message}");
      Console.WriteLine($"Azure Key Vault access failed: {ex.Message}");
    }

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


    if (string.IsNullOrWhiteSpace(s))
    {
      throw new InvalidOperationException($"Connection string '{connectionType}Connection' is null or empty. Checked: Local{connectionType}Connection, {connectionType}connection, ConnectionStrings:{connectionType}connection");
    }

    Debug.WriteLine($"DEBUG {connectionType} String:{s}");
    Console.WriteLine($"Console {connectionType} String:{s}");
    return s;
  }
}