namespace Budget.Shared.Models;

/// <summary>
/// Represents the result of an operation that can succeed or fail
/// </summary>
public sealed class Result<T>
{
  /// <summary>
  /// Indicates whether the operation was successful
  /// </summary>
  public bool IsSuccess { get; init; }

  /// <summary>
  /// The value returned by a successful operation
  /// </summary>
  public T? Value { get; init; }

  /// <summary>
  /// The error message if the operation failed
  /// </summary>
  public string? Error { get; init; }

  /// <summary>
  /// Creates a successful result with a value
  /// </summary>
  public static Result<T> Success(T value) => new()
  {
    IsSuccess = true,
    Value = value
  };

  /// <summary>
  /// Creates a failed result with an error message
  /// </summary>
  public static Result<T> Failure(string error) => new()
  {
    IsSuccess = false,
    Error = error
  };
}
