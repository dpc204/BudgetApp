namespace Budget.Shared.Models;

/// <summary>
/// System information about the Budget application environment
/// </summary>
public sealed record BudgetSystemInfoDto(
  bool UseAzureDB,
  string DatabaseEnvironment);
