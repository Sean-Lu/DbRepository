using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository.Util;

public class FieldExpressionUtil
{
    public static Expression<Func<TEntity, object>> Create<TEntity>(Expression<Func<TEntity, object>> fieldExpression)
    {
        return fieldExpression;
    }
    public static Expression<Func<TEntity, object>> Create<TEntity>(bool condition, Expression<Func<TEntity, object>> trueFieldExpression, Expression<Func<TEntity, object>> falseFieldExpression)
    {
        return condition ? trueFieldExpression : falseFieldExpression;
    }

    public static Expression<Func<TEntity, object>> Create<TEntity>(IEnumerable<string> fieldNames)
    {
        Expression body = Expression.Constant(fieldNames);
        ParameterExpression param = Expression.Parameter(typeof(TEntity), "entity");
        var fieldExpression = Expression.Lambda<Func<TEntity, object>>(body, param);
        return fieldExpression;
    }

    public static List<string> GetFieldNames<TEntity>(Expression<Func<TEntity, object>> fieldExpression)
    {
        return fieldExpression.GetFieldNames();
    }

    public static bool IsFieldExists<TEntity>(Expression<Func<TEntity, object>> fieldExpression, string fieldName)
    {
        return fieldExpression.IsFieldExists(fieldName);
    }
    public static bool IsFieldExists<TEntity>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, object>> matchFieldExpression)
    {
        return fieldExpression.IsFieldExists(matchFieldExpression);
    }

    public static Expression<Func<TEntity, object>> AddFields<TEntity>(Expression<Func<TEntity, object>> fieldExpression, params string[] addFieldNames)
    {
        return fieldExpression.AddFields(addFieldNames);
    }
    public static Expression<Func<TEntity, object>> AddFields<TEntity>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, object>> addFieldExpression)
    {
        return fieldExpression.AddFields(addFieldExpression);
    }

    public static Expression<Func<TEntity, object>> IgnoreFields<TEntity>(Expression<Func<TEntity, object>> ignoreFieldExpression)
    {
        var allFieldNames = typeof(TEntity).GetEntityInfo().FieldInfos.Select(c => c.FieldName).ToList();
        var ignoreFieldNames = ignoreFieldExpression?.GetFieldNames();
        return ignoreFieldNames != null && ignoreFieldNames.Any()
            ? Create<TEntity>(allFieldNames.Except(ignoreFieldNames).ToList())
            : Create<TEntity>(allFieldNames);
    }
    public static Expression<Func<TEntity, object>> IgnoreFields<TEntity>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, object>> ignoreFieldExpression)
    {
        return fieldExpression.IgnoreFields(ignoreFieldExpression);
    }
}