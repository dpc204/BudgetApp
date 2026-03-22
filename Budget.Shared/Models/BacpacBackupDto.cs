namespace Budget.Shared.Models;

/// <summary>
/// Represents a BACPAC backup record stored in Azure Table Storage
/// </summary>
public sealed record BacpacBackupDto(
  string RowKey,
  string DatabaseName,
  DateTime CreatedAt,
  long SizeBytes,
  string BlobName);
