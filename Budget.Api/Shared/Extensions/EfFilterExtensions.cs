using Budget.Shared.Models.Queries;
using System.Linq.Expressions;

namespace Budget.Api.Shared.Extensions;

public static class EfFilterExtensions
{
  public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, List<FilterItem>? filters)
  {
    if (filters == null || filters.Count == 0) return query;

    var parameter = Expression.Parameter(typeof(T), "x");

    foreach (var filter in filters)
    {
      if (string.IsNullOrWhiteSpace(filter.Column) || string.IsNullOrWhiteSpace(filter.Value))
        continue;

// Find property
      var property = typeof(T).GetProperty(filter.Column);
      if (property == null) continue;

// Convert string value to property type
      var convertedValue = Convert.ChangeType(filter.Value, property.PropertyType);

// Build expression: x.Property
      var left = Expression.Property(parameter, property);

// Build constant expression
      var right = Expression.Constant(convertedValue);

      Expression? predicate = null;


      // Choose operator 
      predicate = filter.Operator switch {
        "contains" or "Contains" => Expression.Call(left, typeof(string).GetMethod("Contains", [typeof(string)])!,
                    right),
        "starts" or "StartsWith" => Expression.Call(left, typeof(string).GetMethod("StartsWith", [typeof(string)])!,
                    right),
        "ends" or "EndsWith" => Expression.Call(left, typeof(string).GetMethod("EndsWith", [typeof(string)])!,
                    right),
        _ => Expression.Equal(left, right),
      };

      // Build lambda: x => x.Property == value

      var lambda = Expression.Lambda<Func<T, bool>>(predicate!, parameter);

// Apply to query
      query = query.Where(lambda);
    }

    return query;
  }
}