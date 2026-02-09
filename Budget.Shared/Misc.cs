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
        logger.Log(LogLevel.Information, "Adding AzureKeyVault next");

        var keyVaultUri = webApplicationBuilder.Configuration["KeyVault:Uri"]
          ?? webApplicationBuilder.Configuration["AZURE_KEY_VAULT_ENDPOINT"]
          ?? "https://fantumkeyvault.vault.azure.net/";
        
        // Get the Managed Identity Client ID for authentication
        // Try custom variable first (to avoid azd override), then fall back to standard names
        var managedIdentityClientId = webApplicationBuilder.Configuration["BUDGET_MANAGED_IDENTITY_CLIENT_ID"]
          ?? webApplicationBuilder.Configuration["AZURE_CLIENT_ID"]
          ?? webApplicationBuilder.Configuration["MANAGED_IDENTITY_CLIENT_ID"];
        
        Azure.Core.TokenCredential credential;
        
        if (!string.IsNullOrEmpty(managedIdentityClientId))
        {
          logger.LogInformation("Using Managed Identity with Client ID: {ClientId}", managedIdentityClientId);
          credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
          {
            ManagedIdentityClientId = managedIdentityClientId
          });
        }
        else
        {
          logger.LogInformation("Using DefaultAzureCredential without explicit Managed Identity Client ID");
          credential = new DefaultAzureCredential();
        }
        
        logger.LogInformation("Connecting to Key Vault: {KeyVaultUri}", keyVaultUri);
        webApplicationBuilder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), credential);

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

      var sValue = webApplicationBuilder.Configuration["UseAzureDB"]?.ToLower();
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

  /// <summary>
  /// Logs all configuration settings with their keys, values, and provider sources
  /// </summary>
  public static void LogAllConfigurationSettings(WebApplicationBuilder webApplicationBuilder)
  {
    if (!Debugger.IsAttached)
      return;

    if (webApplicationBuilder.Configuration is IConfigurationRoot configRoot)
    {
      var headerMessage = "===================== Configuration Settings by Provider === === === === === === === === === === === === === === === === === === === === === === === === === === === ";
      Debug.WriteLine(headerMessage);

      foreach(var provider in configRoot.Providers.Reverse())
      {
        var providerMessage = $"Provider: {provider.GetType().Name}";
        Debug.WriteLine(providerMessage);

        LoadProviderData(provider);
        
        foreach (var key in GetAllKeys(provider))
        {
          if (provider.TryGet(key, out var value))
          {
            // Mask sensitive values
            var keyValueMessage = $"  xKey: {key}, Value: {value}";
            Debug.WriteLine(keyValueMessage);
          }
        }
      }
      
      var footerMessage = "=== End Configuration Settings ===";
      Debug.WriteLine(footerMessage);
    }
    else
    {
      var warningMessage = "Configuration is not IConfigurationRoot, cannot enumerate providers";
      Debug.WriteLine(warningMessage);
    }
  }

  private static void LoadProviderData(IConfigurationProvider provider)
  {
    // Force the provider to load data if it hasn't already
    provider.Load();
  }

  private static List<string> GetAllKeys(IConfigurationProvider provider)
  {
    var keys = new List<string>();
    GetKeysRecursive(provider, null, keys);
    return keys;
  }

  private static void GetKeysRecursive(IConfigurationProvider provider, string? parentPath, List<string> keys)
  {
    var children = provider.GetChildKeys([], parentPath);
    
    foreach (var child in children)
    {
      var key = parentPath == null ? child : $"{parentPath}:{child}";
      keys.Add(key);
      GetKeysRecursive(provider, key, keys);
    }
  }

  private static bool IsSensitiveKey(string key)
  {
    var lowerKey = key.ToLowerInvariant();
    return lowerKey.Contains("password") ||
           lowerKey.Contains("secret") ||
           lowerKey.Contains("key") ||
           lowerKey.Contains("token") ||
           lowerKey.Contains("connectionstring");
  }
}