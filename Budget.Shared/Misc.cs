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

  public static string GetConnectionString(WebApplicationBuilder webApplicationBuilder,
    ConnectionStringType connectionStringType, ILogger logger)
  {
    var connectionType = connectionStringType.ToString();
    logger.Log(LogLevel.Information, "SetupConfigurationSources - Type: {ConnectionType}", connectionType);

    string? s;
    var configuration = webApplicationBuilder.Configuration;
    s = Misc.UseAzureDB(webApplicationBuilder, logger)
      ? configuration[$"{connectionType}Connection"]
      : configuration[$"Local{connectionType}Connection"];

    if (string.IsNullOrWhiteSpace(s))
    {
      throw new InvalidOperationException(
        $"Connection string!@# '{connectionType}Connection' is null or empty. Checked: Local{connectionType}Connection, {connectionType}connection, ConnectionStrings:{connectionType}connection");
    }

    logger.Log(LogLevel.Information, "SetupConfigurationsSources Done.  Conn Type: {ConnType} Conn Str: {ConnString} UseAzureDB {UserAzureDB}",
      connectionType, s,
      UseAzureDB(webApplicationBuilder, logger));
    return s;
  }

  public static void SetupConfigurationSources(WebApplicationBuilder webApplicationBuilder, Assembly assembly1,
    ILogger logger)
  {
    webApplicationBuilder.Configuration.AddUserSecrets(assembly1);
    webApplicationBuilder.Configuration.AddEnvironmentVariables();

    if (Misc.UseAzureDB(webApplicationBuilder, logger))
    {
      try
      {
        logger.Log(LogLevel.Information, $"Adding AzureKeyVault next");

        var keyVaultUri = webApplicationBuilder.Configuration["KeyVault:Uri"]
          ?? "https://fantumkeyvault.vault.azure.net/";
        foreach(var config in webApplicationBuilder.Configuration.AsEnumerable().Where(a => a.Key.Contains("AzureAd")))
        {
          logger.Log(LogLevel.Information, "Config1:{key}:{value}", config.Key, config.Value);
        }
        webApplicationBuilder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri),
          new DefaultAzureCredential());

        

        logger.Log(LogLevel.Information, "SetupConfigurationSources Using AzureDB - KeyVault Done");
      }
      catch (Azure.RequestFailedException ex) when (ex.Status == 403)
      {
        // Log but don't fail if Key Vault access is denied
        logger.LogWarning("Azure Key Vault access denied (403 Forbidden). This is expected if managed identity permissions are not configured yet. Continuing without Key Vault. Error: {Message}", ex.Message);
      }
      catch (Exception ex)
      {
        // Log the exception but don't fail startup in development or when Key Vault is not critical
        logger.LogWarning("Azure Key Vault access failed: {Message}. Continuing without Key Vault.", ex.Message);
      }
    }
  }


  public static bool UseAzureDB(WebApplicationBuilder webApplicationBuilder, ILogger logger)
  {
    logger.Log(LogLevel.Information, $"Checking UseAzureDB");


    logger.Log(LogLevel.Information, $"Checking If UseAzureDB is null");
    if (UseAzureDb is null)
    {
      logger.Log(LogLevel.Information, "UseAzureDb is null");
      if (AzureEnvironment.IsRunningOnAzure)
      {
        UseAzureDb = true;
        logger.Log(LogLevel.Information, $"IsRunningOnAzure = true");
        return true;
      }
      else {
        logger.Log(LogLevel.Information, $"IsRunningOnAzure = false");
      }

      var sValue = webApplicationBuilder.Configuration["UseAzureDB"];
      logger.Log(LogLevel.Information, "UseAzureDB from config: {SValue}", sValue);
      if (bool.TryParse(sValue, out var bValue))
      {
        UseAzureDb = bValue;
      }
      else
      {
        UseAzureDb = false;
      }
    }

    Console.WriteLine($"Checking UseAzureDB - UserAzureDB: {UseAzureDb}");
    return (bool)UseAzureDb;
  }

  public static string? ParseDataSource(string cs)
  {
    if (string.IsNullOrEmpty(cs)) return null;
    foreach (var part in cs.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
      if (part.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase) ||
          part.StartsWith("Server=", StringComparison.OrdinalIgnoreCase))
      {
        var idx = part.IndexOf('=');
        if (idx > -1 && idx < part.Length - 1)
          return part[(idx + 1)..];
      }
    }

    return null;
  }
}