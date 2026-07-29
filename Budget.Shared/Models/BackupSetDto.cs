namespace Budget.Shared.Models;

/// <summary>
/// Represents a backup set (group of table backups)
/// </summary>
public sealed record BackupSetDto(
  string PartitionKey,
  DateTime BackupDate,
  int TableCount,
  long TotalSizeBytes,
  string Note = "");
