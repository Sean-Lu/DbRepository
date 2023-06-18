using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Sean.Core.DbRepository.Extensions;

public static class PropertyInfoExtensions
{
    public static Expression<Func<TEntity, object>> ToFieldExpression<TEntity>(this PropertyInfo propertyInfo)
    {
        var entityParam = Expression.Parameter(typeof(TEntity), "entity");
        var propExpr = Expression.Property(entityParam, propertyInfo);
        var castExpr = Expression.Convert(propExpr, typeof(object));
        return Expression.Lambda<Func<TEntity, object>>(castExpr, entityParam);
    }
}