using System.Linq.Expressions;

namespace Budget.Api.Shared.Extensions
{
  public static class DynamicSortExtensions
  {
    public static IQueryable<T> OrderByDynamic<T>(this IQueryable<T> query, string propertyName)
    {
      var parameter = Expression.Parameter(typeof(T), "x");
      var property = Expression.Property(parameter, propertyName);
      var lambda = Expression.Lambda(property, parameter);

      return Queryable.OrderBy(query, (dynamic)lambda);
    }

    public static IQueryable<T> OrderByDescendingDynamic<T>(this IQueryable<T> query, string propertyName)
    {
      var parameter = Expression.Parameter(typeof(T), "x");
      var property = Expression.Property(parameter, propertyName);
      var lambda = Expression.Lambda(property, parameter);

      return Queryable.OrderByDescending(query, (dynamic)lambda);
    }
  }

}
