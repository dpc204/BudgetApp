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
      switch (filter.Operator)
      {
        case "contains":
        case "Contains":
          predicate = Expression.Call(left, typeof(string).GetMethod("Contains", new[] { typeof(string) })!,
            right);
          break;
        case "starts":
        case "StartsWith":
          predicate = Expression.Call(left, typeof(string).GetMethod("StartsWith", new[] { typeof(string) })!,
            right);
          break;
        case "ends":
        case "EndsWith":
          predicate = Expression.Call(left, typeof(string).GetMethod("EndsWith", new[] { typeof(string) })!,
            right);
          break;
        default:

          predicate = Expression.Equal(left, right);
          break;
      }

// Build lambda: x => x.Property == value

      var lambda = Expression.Lambda<Func<T, bool>>(predicate!, parameter);

// Apply to query
      query = query.Where(lambda);
    }

    return query;
  }
}