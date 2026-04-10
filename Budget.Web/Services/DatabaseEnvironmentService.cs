namespace Budget.Web.Services;

/// <summary>
/// Provides information about the database environment in use
/// </summary>
public class DatabaseEnvironmentService(
  IUtilitiesApiClient apiClient,
  ILogger<DatabaseEnvironmentService> logger)
{
  private BudgetSystemInfoDto? _systemInfo;
  private readonly SemaphoreSlim _lock = new(1, 1);

  /// <summary>
  /// Gets whether the application is using Azure database (true) or Local database (false)
  /// </summary>
  public bool IsAzureDatabase => GetSystemInfo().UseAzureDB;

  /// <summary>
  /// Gets a display string for the current database environment
  /// </summary>
  public string DatabaseEnvironmentLabel => GetSystemInfo().DatabaseEnvironment;

  private BudgetSystemInfoDto GetSystemInfo()
  {
    if(_systemInfo is not null)
      return _systemInfo;

    // If not loaded yet, trigger async load and return default for now
    _ = LoadSystemInfoAsync();

    // Return default until loaded
    return _systemInfo ?? new BudgetSystemInfoDto(false, "Loading...");
  }

  private async Task LoadSystemInfoAsync()
  {
    await _lock.WaitAsync();
    try
    {
      if(_systemInfo is not null)
        return;

      _systemInfo = await apiClient.GetSystemInfoAsync();
      logger.LogInformation("System info loaded: {Environment}", _systemInfo.DatabaseEnvironment);
    }
    catch(Exception ex)
    {
      logger.LogError(ex, "Failed to load system info from API");
      // Fallback to default
      _systemInfo = new BudgetSystemInfoDto(false, "Unknown");
    }
    finally
    {
      _lock.Release();
    }
  }

  /// <summary>
  /// Ensures system info is loaded from the API
  /// </summary>
  public async Task EnsureLoadedAsync()
  {
    await LoadSystemInfoAsync();
  }
}
