using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Budget.Functions;

/// <summary>
/// Azure Function that performs a scheduled BACPAC backup at 2:00 AM UTC daily.
/// </summary>
public class BacpacTimerFunction(
  BacpacBackupService backupService,
  ILogger<BacpacTimerFunction> logger)
{
  /// <summary>
  /// Timer-triggered backup that runs at 2:00 AM UTC every day.
  /// Cron expression: "0 0 2 * * *"
  /// </summary>
  [Function("BacpacDailyBackup")]
  public async Task RunAsync([TimerTrigger("0 0 2 * * *")] TimerInfo timerInfo)
  {
    logger.LogInformation("BacpacDailyBackup timer trigger fired at {Time} UTC", DateTime.UtcNow);

    if(timerInfo.IsPastDue)
    {
      logger.LogWarning("Timer is running late. Previous schedule was overdue.");
    }

    try
    {
      await backupService.RunBackupAsync();
      logger.LogInformation("BacpacDailyBackup completed successfully.");
    }
    catch(Exception ex)
    {
      logger.LogError(ex, "BacpacDailyBackup failed with error: {Message}", ex.Message);
      throw;
    }
  }
}
