namespace Budget.Shared.Models;

/// <summary>
/// Represents a single table backup within a backup set
/// </summary>
public sealed record BackupTableDto(
  string TableName,
  string BlobName,
  long SizeBytes,
  DateTime ExportedAt,
  string PartitionKey);
