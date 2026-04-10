namespace Budget.Shared.Services;

/// <summary>
/// Service to export data from a DbSet to CSV format.
/// </summary>
public static class CsvExportService
{
  /// <summary>
  /// Exports entities from a DbSet to CSV format.
  /// </summary>
  /// <typeparam name="T">The entity type of the DbSet.</typeparam>
  /// <param name="entities">The collection of entities to export.</param>
  /// <param name="separator">The separator character to use in the CSV file. Defaults to ",".</param>
  /// <param name="log">Optional logger for debugging.</param>
  /// <returns>A string containing the CSV data with headers and all entity rows.</returns>
  public static string ExportToCsv<T>(
    IEnumerable<T> entities,
    string separator = ",",
    ILogger? log = null) where T : class
  {
    log?.LogInformation("Starting CSV export for {EntityType}", typeof(T).Name);

    var entityList = entities.ToList();
    if(entityList.Count == 0)
    {
      log?.LogWarning("No entities to export");
      return string.Empty;
    }

    // Get properties of the entity type
    var entityType = typeof(T);
    var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
      .Where(p => p.CanRead && IsSimpleType(p.PropertyType))
      .ToList();

    if(properties.Count == 0)
    {
      log?.LogWarning("No exportable properties found for {EntityType}", entityType.Name);
      return string.Empty;
    }

    var csvBuilder = new StringBuilder();

    // Create header line
    var headers = properties.Select(p => EscapeCsvValue(p.Name, separator));
    csvBuilder.AppendLine(string.Join(separator, headers));
    log?.LogInformation("Created CSV header with {PropertyCount} columns", properties.Count);

    // Create data lines
    foreach(var entity in entityList)
    {
      var values = properties.Select(p =>
      {
        var value = p.GetValue(entity);
        return EscapeCsvValue(FormatValue(value), separator);
      });
      csvBuilder.AppendLine(string.Join(separator, values));
    }

    log?.LogInformation("Exported {EntityCount} entities to CSV", entityList.Count);
    return csvBuilder.ToString();
  }

  /// <summary>
  /// Determines if a type is a simple type that can be exported to CSV.
  /// </summary>
  private static bool IsSimpleType(Type type)
  {
    var actualType = Nullable.GetUnderlyingType(type) ?? type;

    return actualType.IsPrimitive
           || actualType.IsEnum
           || actualType == typeof(string)
           || actualType == typeof(decimal)
           || actualType == typeof(DateTime)
           || actualType == typeof(DateOnly)
           || actualType == typeof(TimeOnly)
           || actualType == typeof(Guid);
  }

  /// <summary>
  /// Formats a value for CSV output.
  /// </summary>
  private static string FormatValue(object? value)
  {
    if(value is null)
    {
      return string.Empty;
    }

    return value switch {
      DateTime dt => dt.ToString("O"), // ISO 8601 format
      DateOnly d => d.ToString("yyyy-MM-dd"),
      TimeOnly t => t.ToString("HH:mm:ss"),
      decimal dec => dec.ToString("0.############################"), // Remove trailing zeros
      double dbl => dbl.ToString("G"),
      float flt => flt.ToString("G"),
      _ => value.ToString() ?? string.Empty
    };
  }

  /// <summary>
  /// Escapes a CSV value by wrapping it in quotes if it contains special characters.
  /// </summary>
  private static string EscapeCsvValue(string value, string separator)
  {
    if(string.IsNullOrEmpty(value))
    {
      return string.Empty;
    }

    var sepChar = separator.Length > 0 ? separator[0] : ',';

    // Check if value needs escaping (contains separator, quotes, newlines)
    if(value.Contains(sepChar) || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
    {
      // Escape quotes by doubling them
      var escaped = value.Replace("\"", "\"\"");
      return $"\"{escaped}\"";
    }

    return value;
  }
}
