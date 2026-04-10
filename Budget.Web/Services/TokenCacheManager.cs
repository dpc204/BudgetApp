namespace Budget.Web.Services;

using Budget.Shared.Services;
using Microsoft.Data.SqlClient;

/// <summary>
/// Manages token cache operations including detection and clearing of stale tokens
/// </summary>
public sealed class TokenCacheManager(
  IConnectionStringProvider connectionStringProvider,
  ILogger<TokenCacheManager> logger)
{
  private static readonly SemaphoreSlim _clearLock = new(1, 1);
  private static DateTime? _lastClearTime;
  private static readonly TimeSpan _minTimeBetweenClears = TimeSpan.FromMinutes(5);

  /// <summary>
  /// Detects if a 401 error is due to stale tokens and clears the cache if needed
  /// </summary>
  public async Task<bool> HandleStaleTokenAsync(string userId, CancellationToken cancellationToken = default)
  {
    try
    {
      // Prevent rapid consecutive clears
      if(_lastClearTime.HasValue && DateTime.UtcNow - _lastClearTime.Value < _minTimeBetweenClears)
      {
        logger.LogInformation("Skipping token cache clear - cleared {SecondsAgo} seconds ago",
          (DateTime.UtcNow - _lastClearTime.Value).TotalSeconds);
        return false;
      }

      await _clearLock.WaitAsync(cancellationToken);
      try
      {
        // Double-check after acquiring lock
        if(_lastClearTime.HasValue && DateTime.UtcNow - _lastClearTime.Value < _minTimeBetweenClears)
        {
          return false;
        }

        logger.LogWarning("Detected stale token for user {UserId} - clearing token cache", userId);

        var cleared = await ClearTokenCacheAsync(cancellationToken);

        if(cleared)
        {
          _lastClearTime = DateTime.UtcNow;
          logger.LogInformation("Token cache cleared successfully at {Time}", _lastClearTime);
        }
        else
        {
          logger.LogWarning("Token cache clear failed - user should manually sign out and sign in");
        }

        return cleared;
      }
      finally
      {
        _clearLock.Release();
      }
    }
    catch(Exception ex)
    {
      // Never let exceptions escape - this is a best-effort operation
      logger.LogError(ex, "Unexpected error in HandleStaleTokenAsync - suppressing exception");
      return false;
    }
  }

  /// <summary>
  /// Clears all tokens from the distributed cache (SQL Server SessionCache table)
  /// </summary>
  private async Task<bool> ClearTokenCacheAsync(CancellationToken cancellationToken)
  {
    try
    {
      // Get connection string from the provider
      var sqlConnection = connectionStringProvider.BudgetConnectionString;

      if(!string.IsNullOrEmpty(sqlConnection))
      {
        logger.LogInformation("Clearing SQL Server token cache");
        return await ClearSqlServerCacheAsync(sqlConnection, cancellationToken);
      }

      // If no SQL connection available, log and return
      logger.LogWarning("No SQL connection string available - cannot clear token cache");
      return false;
    }
    catch(Exception ex)
    {
      logger.LogError(ex, "Failed to clear token cache: {Message}", ex.Message);
      return false;
    }
  }

  /// <summary>
  /// Clears the SQL Server SessionCache table
  /// </summary>
  private async Task<bool> ClearSqlServerCacheAsync(string connectionString, CancellationToken cancellationToken)
  {
    try
    {
      await using var connection = new SqlConnection(connectionString);
      await connection.OpenAsync(cancellationToken);

      var command = connection.CreateCommand();
      command.CommandText = "DELETE FROM dbo.SessionCache";
      command.CommandTimeout = 30;

      var rowsDeleted = await command.ExecuteNonQueryAsync(cancellationToken);

      logger.LogInformation("Cleared {RowCount} entries from SQL Server SessionCache", rowsDeleted);
      return true;
    }
    catch(SqlException ex)
    {
      logger.LogError(ex, "SQL error clearing cache (SqlError {ErrorNumber}): {Message}",
        ex.Number, ex.Message);
      return false;
    }
    catch(InvalidOperationException ex) when(ex.Message.Contains("pooling"))
    {
      logger.LogError(ex, "Connection pool issue clearing cache - will retry on next request");
      return false;
    }
    catch(Exception ex)
    {
      logger.LogError(ex, "Failed to clear SQL Server cache: {Message}", ex.Message);
      return false;
    }
  }

  /// <summary>
  /// Checks if the token cache should be cleared based on error patterns
  /// </summary>
  public bool ShouldClearCache(string errorCode, string reasonPhrase)
  {
    // Clear cache for these specific scenarios
    return errorCode switch {
      "interaction_required" => true,
      "invalid_grant" => true,
      "consent_required" => true,
      "user_null" => true, // Token cache has no account data - must re-authenticate
      _ => reasonPhrase?.Contains("consent required", StringComparison.OrdinalIgnoreCase) == true ||
           reasonPhrase?.Contains("stale", StringComparison.OrdinalIgnoreCase) == true ||
           reasonPhrase?.Contains("user_null", StringComparison.OrdinalIgnoreCase) == true
    };
  }


}
