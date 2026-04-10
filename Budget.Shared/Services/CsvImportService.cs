namespace Budget.Shared.Services;

/// <summary>
/// Service to import CSV files into a DbSet.
/// </summary>
public static class CsvImportService
{
  /// <summary>
  /// Imports CSV data from a file into the specified DbSet.
  /// </summary>
  /// <typeparam name="T">The entity type of the DbSet.</typeparam>
  /// <param name="dbSet">The DbSet to import data into.</param>
  /// <param name="filename">The fully qualified path to the CSV file.</param>
  /// <param name="separator">The separator character used in the CSV file. Defaults to ",".</param>
  /// <param name="log">Optional logger for debugging.</param>
  /// <returns>A list of entities imported from the CSV file.</returns>
  /// <exception cref="ArgumentException">Thrown when the file does not exist or headers don't match properties.</exception>
  /// <exception cref="InvalidOperationException">Thrown when column headers don't match entity properties.</exception>
  public static async Task<List<T>> ImportAsync<T>(
    DbSet<T> dbSet,
    string filename,
    string separator = ",",
    ILogger? log = null) where T : class, new()
  {
    log?.LogInformation("Starting CSV import from file: {Filename}", filename);
    if(!File.Exists(filename))
    {
      throw new ArgumentException($"File not found: {filename}", nameof(filename));
    }

    var lines = await File.ReadAllLinesAsync(filename);
    return await ImportAsync(dbSet, [.. lines], separator, log);
  }

  /// <summary>
  /// Imports CSV data from a list of lines into the specified DbSet.
  /// </summary>
  /// <typeparam name="T">The entity type of the DbSet.</typeparam>
  /// <param name="dbSet">The DbSet to import data into.</param>
  /// <param name="lines">The CSV lines including the header line as the first element.</param>
  /// <param name="separator">The separator character used in the CSV data. Defaults to ",".</param>
  /// <param name="log">Optional logger for debugging.</param>
  /// <returns>A list of entities imported from the CSV data.</returns>
  /// <exception cref="ArgumentException">Thrown when lines are empty.</exception>
  /// <exception cref="InvalidOperationException">Thrown when column headers don't match entity properties.</exception>
  public static async Task<List<T>> ImportAsync<T>(
    DbSet<T> dbSet,
    List<string> lines,
    string separator = ",",
    ILogger? log = null) where T : class, new()
  {
    log?.LogInformation("Starting CSV import from {LineCount} lines", lines.Count);
    if(lines.Count == 0)
    {
      throw new ArgumentException("CSV data is empty.", nameof(lines));
    }

    // Parse header line
    var headers = ParseCsvLine(lines[0], separator);

    // Get properties of the entity type
    var entityType = typeof(T);
    var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
      .Where(p => p.CanWrite)
      .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

    // Validate that all CSV headers match entity properties
    var propertyMapping = new List<(int ColumnIndex, PropertyInfo Property)>();
    for(int i = 0; i < headers.Count; i++)
    {
      var header = headers[i].Trim();
      if(string.IsNullOrWhiteSpace(header))
      {
        continue;
      }

      if(!properties.TryGetValue(header, out var property))
      {
        throw new InvalidOperationException(
          $"CSV column '{header}' does not match any property in entity type '{entityType.Name}'. " +
          $"Available properties: {string.Join(", ", properties.Keys)}");
      }

      propertyMapping.Add((i, property));
      log?.LogInformation("Mapping CSV column '{Header}' to property '{PropertyName}'", header, property.Name);
    }

    // Parse data lines
    var entities = new List<T>();
    for(int lineIndex = 1; lineIndex < lines.Count; lineIndex++)
    {
      var line = lines[lineIndex];
      log?.LogInformation("Processing line {LineIndex}: {LineContent}", lineIndex + 1, line);
      if(string.IsNullOrWhiteSpace(line))
      {
        continue;
      }

      var values = ParseCsvLine(line, separator);
      var entity = new T();

      foreach(var (columnIndex, property) in propertyMapping)
      {
        if(columnIndex < values.Count)
        {
          var value = values[columnIndex];
          var convertedValue = ConvertValue(value, property.PropertyType, property.Name, lineIndex + 1);
          log?.LogInformation(
            "Setting property '{PropertyName}' to value '{Value}' (converted to {ConvertedValue})",
            property.Name,
            value,
            convertedValue ?? "null");
          property.SetValue(entity, convertedValue);
        }
      }

      entities.Add(entity);
    }

    // Add entities to DbSet
    await dbSet.AddRangeAsync(entities);

    return entities;
  }


  /// <summary>
  /// Parses a CSV line, handling quoted strings.
  /// </summary>
  private static List<string> ParseCsvLine(string line, string separator)
  {
    var result = new List<string>();
    var currentValue = new StringBuilder();
    bool inQuotes = false;
    var sepChar = separator.Length > 0 ? separator[0] : ',';

    for(int i = 0; i < line.Length; i++)
    {
      char c = line[i];

      if(c == '"')
      {
        if(inQuotes && i + 1 < line.Length && line[i + 1] == '"')
        {
          // Escaped quote
          currentValue.Append('"');
          i++;
        }
        else
        {
          // Toggle quote state
          inQuotes = !inQuotes;
        }
      }
      else if(c == sepChar && !inQuotes)
      {
        result.Add(currentValue.ToString());
        currentValue.Clear();
      }
      else
      {
        currentValue.Append(c);
      }
    }

    // Add the last value
    result.Add(currentValue.ToString());

    return result;
  }

  /// <summary>
  /// Converts a string value to the target type.
  /// </summary>
  private static object? ConvertValue(string value, Type targetType, string propertyName, int lineNumber)
  {
    // Handle nullable types
    var underlyingType = Nullable.GetUnderlyingType(targetType);
    var isNullable = underlyingType != null;
    var actualType = underlyingType ?? targetType;

    // Handle empty/whitespace values
    var trimmedValue = value.Trim();
    if(string.IsNullOrWhiteSpace(trimmedValue))
    {
      if(isNullable || !actualType.IsValueType)
      {
        return null;
      }

      // Return default value for non-nullable value types
      return Activator.CreateInstance(actualType);
    }

    try
    {
      // Handle common types
      if(actualType == typeof(string))
      {
        return trimmedValue;
      }

      if(actualType == typeof(int))
      {
        return int.Parse(trimmedValue);
      }

      if(actualType == typeof(long))
      {
        return long.Parse(trimmedValue);
      }

      if(actualType == typeof(decimal))
      {
        return decimal.Parse(trimmedValue);
      }

      if(actualType == typeof(double))
      {
        return double.Parse(trimmedValue);
      }

      if(actualType == typeof(float))
      {
        return float.Parse(trimmedValue);
      }

      if(actualType == typeof(bool))
      {
        return bool.Parse(trimmedValue);
      }

      if(actualType == typeof(DateTime))
      {
        return DateTime.Parse(trimmedValue);
      }

      if(actualType == typeof(DateOnly))
      {
        return DateOnly.Parse(trimmedValue);
      }

      if(actualType == typeof(TimeOnly))
      {
        return TimeOnly.Parse(trimmedValue);
      }

      if(actualType == typeof(Guid))
      {
        return Guid.Parse(trimmedValue);
      }

      if(actualType.IsEnum)
      {
        return Enum.Parse(actualType, trimmedValue, ignoreCase: true);
      }

      // Fallback to TypeConverter
      var converter = System.ComponentModel.TypeDescriptor.GetConverter(actualType);
      if(converter.CanConvertFrom(typeof(string)))
      {
        return converter.ConvertFromString(trimmedValue);
      }

      throw new InvalidOperationException(
        $"Cannot convert value '{trimmedValue}' to type '{actualType.Name}'.");
    }
    catch(Exception ex) when(ex is not InvalidOperationException)
    {
      throw new InvalidOperationException(
        $"Error converting value '{trimmedValue}' for property '{propertyName}' " +
        $"to type '{actualType.Name}' at line {lineNumber}: {ex.Message}",
        ex);
    }
  }
}