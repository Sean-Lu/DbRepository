using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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

    public static Expression<Func<TEntity, object>> CreateFromDto<TDto, TEntity>(Expression<Func<TDto, object>> ignoreFieldExpression = null)
    {
        // 1. 获取 TDto 和 TEntity 的普通属性名并取交集
        var dtoProperties = new HashSet<string>(typeof(TDto).GetProperties().Select(p => p.Name));
        var entityProperties = new HashSet<string>(typeof(TEntity).GetEntityInfo().FieldInfos.Select(c => c.PropertyName));
        var commonProperties = dtoProperties.Intersect(entityProperties).ToList();

        List<string> finalFieldNames;

        // 2. 处理忽略逻辑
        if (ignoreFieldExpression != null)
        {
            // 2.1 因为 TDto 是普通类，手动解析表达式树获取需要忽略的 "属性名" (如 "UserId")
            var ignorePropNames = GetPropertyNameFromDtoExpression(ignoreFieldExpression);

            // 2.2 将交集属性转为真实的数据库字段名，并剔除那些需要忽略的属性
            var entityInfos = typeof(TEntity).GetEntityInfo().FieldInfos;
            finalFieldNames = entityInfos
                .Where(c => commonProperties.Contains(c.PropertyName) && !ignorePropNames.Contains(c.PropertyName))
                .Select(c => c.FieldName)
                .ToList();
        }
        else
        {
            // 3. 没有忽略项，直接将交集属性转为真实的数据库字段名
            finalFieldNames = typeof(TEntity).GetEntityInfo().FieldInfos
                .Where(c => commonProperties.Contains(c.PropertyName))
                .Select(c => c.FieldName)
                .ToList();
        }

        // 4. 传入真实的数据库字段名，交给 Create 处理
        return Create<TEntity>(finalFieldNames);
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

    public static Expression<Func<TEntity, object>> AddFieldsIfNotExists<TEntity>(Expression<Func<TEntity, object>> fieldExpression, params string[] addFieldNames)
    {
        return fieldExpression.AddFieldsIfNotExists(addFieldNames);
    }
    public static Expression<Func<TEntity, object>> AddFieldsIfNotExists<TEntity>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, object>> addFieldExpression)
    {
        return fieldExpression.AddFieldsIfNotExists(addFieldExpression);
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

    /// <summary>
    /// 辅助方法：从普通类的 Expression 中提取属性名称（支持单属性或多属性 New 对象）
    /// </summary>
    private static HashSet<string> GetPropertyNameFromDtoExpression<TDto>(Expression<Func<TDto, object>> expression)
    {
        // 轻量级的表达式树解析器。它能够兼容 x => x.UserId 以及 x => new { x.UserId, x.UserName } 这两种常见的写法。

        var names = new HashSet<string>();

        if (expression.Body is NewExpression newExpr)
        {
            // 情况一：x => new { x.Name, x.Age } 或 x => newDto { Name = x.Name }
            foreach (var arg in newExpr.Arguments)
            {
                if (arg is MemberExpression memberExpr && memberExpr.Member is PropertyInfo)
                {
                    names.Add(memberExpr.Member.Name);
                }
            }
        }
        else if (expression.Body is MemberExpression memberExprBody && memberExprBody.Member is PropertyInfo)
        {
            // 情况二：x => x.Name
            names.Add(memberExprBody.Member.Name);
        }
        else if (expression.Body is UnaryExpression unaryExpr && unaryExpr.Operand is MemberExpression unaryMember && unaryMember.Member is PropertyInfo)
        {
            // 情况三：x => (object)x.Name （由于 object 装箱产生的 UnaryExpression）
            names.Add(unaryMember.Member.Name);
        }

        return names;
    }
}