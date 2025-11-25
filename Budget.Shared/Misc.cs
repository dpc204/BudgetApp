using Budget.Shared.Services;

namespace Budget.Shared;

public static class Misc
{
  private static bool? UseAzureDb;

  public enum ConnectionStringType
  {
    Budget,
    Identity
  }

  public static string GetConnectionString(IConfiguration configuration, ConnectionStringType connectionStringType)
  {
    Console.WriteLine("SetupConfigurationSources - Begin");
    var connectionType = connectionStringType.ToString();
    Console.WriteLine($"SetupConfigurationSources - Type: {connectionType}");

    string? s;

    if (Misc.UseAzureDB)
      s = configuration[$"{connectionType}Connection"];
    else
      s = configuration[$"Local{connectionType}Connection"];

    if (string.IsNullOrWhiteSpace(s))
    {
      throw new InvalidOperationException(
        $"Connection string!@# '{connectionType}Connection' is null or empty. Checked: Local{connectionType}Connection, {connectionType}connection, ConnectionStrings:{connectionType}connection");
    }

    Console.WriteLine("SetupConfigurationsSources Done.  Conn Type: {0} Conn Str: {1} UseAzureDB {2}", connectionType, s,
      UseAzureDB);
    return s;
  }

  public static void SetupConfigurationSources(WebApplicationBuilder webApplicationBuilder, Assembly assembly1)
  {
    webApplicationBuilder.Configuration.AddJsonFile("appsettings.json");
    webApplicationBuilder.Configuration.AddUserSecrets(assembly1);
    webApplicationBuilder.Configuration.AddEnvironmentVariables();
    Console.WriteLine("SetupConfigurationSources - Added Json, Secrets and Environment Vars");

    if (Misc.UseAzureDB)
      try
      {
        Console.WriteLine($"Adding AzureKeyVault next");
        webApplicationBuilder.Configuration.AddAzureKeyVault(new Uri("https://fantumkeyvault.vault.azure.net/"),
          new DefaultAzureCredential());
        Console.WriteLine("SetupConfigurationSources Using AzureDB - Kevault Done");
      }
      catch (Exception ex)
      {
        // Log the exception but don't fail startup in development
        Console.WriteLine($"Azure Key Vault access failed: {ex.Message}");
        Console.WriteLine($"Azure Key Vault access failed: {ex.Message}");
      }
  }


  public static bool UseAzureDB
  {
    get
    {
      Console.WriteLine($"Checking UseAzureDB");
      if (AzureEnvironment.IsRunningOnAzure)
        return true;

      Console.WriteLine($"Checking UseAzureDB - Local");
      if (UseAzureDb is null)
      {
        var config = new ConfigurationBuilder()
          .SetBasePath(Directory.GetCurrentDirectory())
          .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
          .AddEnvironmentVariables()
          .Build();
        var sValue = config["UseAzureDB"];
        if (bool.TryParse(sValue, out var bValue))
        {
          UseAzureDb = bValue;
        }
        else
        {
          UseAzureDb = false;
        }
      }

      Console.WriteLine($"Checking UseAzureDB - Local");
      return (bool)UseAzureDb;
    }
  }

  public static string? ParseDataSource(string cs)
  {
    if(string.IsNullOrEmpty(cs)) return null;
    foreach(var part in cs.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
      if(part.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase) ||
         part.StartsWith("Server=", StringComparison.OrdinalIgnoreCase))
      {
        var idx = part.IndexOf('=');
        if(idx > -1 && idx < part.Length - 1)
          return part[(idx + 1)..];
      }
    }
    return null;
  }
}