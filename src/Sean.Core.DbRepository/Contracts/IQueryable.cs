using System;
using System.Linq.Expressions;

namespace Sean.Core.DbRepository;

public interface IQueryable<TEntity> : IBaseSqlBuilder<IQueryable<TEntity>>, ISqlWhere<TEntity, IQueryable<TEntity>>, ISqlJoin<TEntity, IQueryable<TEntity>>
{
    #region [Field]
    /// <summary>
    /// SELECT fields.
    /// </summary>
    /// <param name="fields">Database table field name.</param>
    /// <returns></returns>
    IQueryable<TEntity> SelectFields(params string[] fields);
    /// <summary>
    /// Ignore fields.
    /// </summary>
    /// <param name="fields">Database table field name.</param>
    /// <returns></returns>
    IQueryable<TEntity> IgnoreFields(params string[] fields);

    /// <summary>
    /// MAX(field_name)
    /// </summary>
    /// <param name="fieldName">Database table field name.</param>
    /// <param name="aliasName">Alias name.</param>
    /// <param name="fieldNameFormatted">Whether <paramref name="fieldName"/> has been formatted.</param>
    /// <returns></returns>
    IQueryable<TEntity> MaxField(string fieldName, string aliasName = null, bool fieldNameFormatted = false);
    /// <summary>
    /// MIN(field_name)
    /// </summary>
    /// <param name="fieldName">Database table field name.</param>
    /// <param name="aliasName">Alias name.</param>
    /// <param name="fieldNameFormatted">Whether <paramref name="fieldName"/> has been formatted.</param>
    /// <returns></returns>
    IQueryable<TEntity> MinField(string fieldName, string aliasName = null, bool fieldNameFormatted = false);
    /// <summary>
    /// SUM(field_name)
    /// </summary>
    /// <param name="fieldName">Database table field name.</param>
    /// <param name="aliasName">Alias name.</param>
    /// <param name="fieldNameFormatted">Whether <paramref name="fieldName"/> has been formatted.</param>
    /// <returns></returns>
    IQueryable<TEntity> SumField(string fieldName, string aliasName = null, bool fieldNameFormatted = false);
    /// <summary>
    /// AVG(field_name)
    /// </summary>
    /// <param name="fieldName">Database table field name.</param>
    /// <param name="aliasName">Alias name.</param>
    /// <param name="fieldNameFormatted">Whether <paramref name="fieldName"/> has been formatted.</param>
    /// <returns></returns>
    IQueryable<TEntity> AvgField(string fieldName, string aliasName = null, bool fieldNameFormatted = false);
    /// <summary>
    /// COUNT(field_name)
    /// </summary>
    /// <param name="fieldName">Database table field name.</param>
    /// <param name="aliasName">Alias name.</param>
    /// <param name="fieldNameFormatted">Whether <paramref name="fieldName"/> has been formatted.</param>
    /// <returns></returns>
    IQueryable<TEntity> CountField(string fieldName, string aliasName = null, bool fieldNameFormatted = false);
    /// <summary>
    /// COUNT(DISTINCT field_name)
    /// </summary>
    /// <param name="fieldName">Database table field name.</param>
    /// <param name="aliasName">Alias name.</param>
    /// <returns></returns>
    IQueryable<TEntity> CountDistinctField(string fieldName, string aliasName = null);
    /// <summary>
    /// DISTINCT field_name1, field_name2,...
    /// </summary>
    /// <param name="fields">Database table field name.</param>
    /// <returns></returns>
    IQueryable<TEntity> DistinctFields(params string[] fields);

    /// <summary>
    /// SELECT fields.
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <returns></returns>
    IQueryable<TEntity> SelectFields(Expression<Func<TEntity, object>> fieldExpression);
    /// <summary>
    /// Ignore fields.
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <returns></returns>
    IQueryable<TEntity> IgnoreFields(Expression<Func<TEntity, object>> fieldExpression);

    /// <summary>
    /// MAX(field_name)
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <param name="aliasName">Alias name.</param>
    /// <param name="fieldNameFormatted">Whether field name has been formatted.</param>
    /// <returns></returns>
    IQueryable<TEntity> MaxField(Expression<Func<TEntity, object>> fieldExpression, string aliasName = null, bool fieldNameFormatted = false);
    /// <summary>
    /// MIN(field_name)
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <param name="aliasName">Alias name.</param>
    /// <param name="fieldNameFormatted">Whether field name has been formatted.</param>
    /// <returns></returns>
    IQueryable<TEntity> MinField(Expression<Func<TEntity, object>> fieldExpression, string aliasName = null, bool fieldNameFormatted = false);
    /// <summary>
    /// SUM(field_name)
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <param name="aliasName">Alias name.</param>
    /// <param name="fieldNameFormatted">Whether field name has been formatted.</param>
    /// <returns></returns>
    IQueryable<TEntity> SumField(Expression<Func<TEntity, object>> fieldExpression, string aliasName = null, bool fieldNameFormatted = false);
    /// <summary>
    /// AVG(field_name)
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <param name="aliasName">Alias name.</param>
    /// <param name="fieldNameFormatted">Whether field name has been formatted.</param>
    /// <returns></returns>
    IQueryable<TEntity> AvgField(Expression<Func<TEntity, object>> fieldExpression, string aliasName = null, bool fieldNameFormatted = false);
    /// <summary>
    /// COUNT(field_name)
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <param name="aliasName">Alias name.</param>
    /// <param name="fieldNameFormatted">Whether field name has been formatted.</param>
    /// <returns></returns>
    IQueryable<TEntity> CountField(Expression<Func<TEntity, object>> fieldExpression, string aliasName = null, bool fieldNameFormatted = false);
    /// <summary>
    /// COUNT(DISTINCT field_name)
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <param name="aliasName">Alias name.</param>
    /// <returns></returns>
    IQueryable<TEntity> CountDistinctField(Expression<Func<TEntity, object>> fieldExpression, string aliasName = null);
    /// <summary>
    /// DISTINCT field_name1, field_name2,...
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <returns></returns>
    IQueryable<TEntity> DistinctFields(Expression<Func<TEntity, object>> fieldExpression);
    #endregion

    #region [GROUP BY]
    /// <summary>
    /// GROUP BY column_name
    /// </summary>
    /// <param name="groupBy">The [GROUP BY] keyword is not included.</param>
    /// <returns></returns>
    IQueryable<TEntity> GroupBy(string groupBy);
    IQueryable<TEntity> GroupBy(Expression<Func<TEntity, object>> fieldExpression);
    #endregion

    #region [HAVING]
    /// <summary>
    /// HAVING aggregate_function(column_name) operator value
    /// </summary>
    /// <param name="having">The [HAVING] keyword is not included.</param>
    /// <returns></returns>
    IQueryable<TEntity> Having(string having);
    #endregion

    #region [ORDER BY]
    /// <summary>
    /// ORDER BY column_name,column_name ASC|DESC;
    /// </summary>
    /// <param name="orderBy">The [ORDER BY] keyword is not included.</param>
    /// <returns></returns>
    IQueryable<TEntity> OrderBy(string orderBy);
    IQueryable<TEntity> OrderBy(OrderByCondition orderBy);
    IQueryable<TEntity> OrderBy(OrderByType type, params string[] fieldNames);
    IQueryable<TEntity> OrderBy(OrderByType type, Expression<Func<TEntity, object>> fieldExpression);
    #endregion

    /// <summary>
    /// LIMIT <paramref name="top"/>
    /// </summary>
    /// <param name="top"></param>
    /// <returns></returns>
    IQueryable<TEntity> Top(int? top);
    /// <summary>
    /// LIMIT {(<paramref name="pageNumber"/> - 1) * <paramref name="pageSize"/>},{<paramref name="pageSize"/>}
    /// </summary>
    /// <param name="pageNumber">The current page number for paging query, the minimum value is 1.</param>
    /// <param name="pageSize">The page size for paging query.</param>
    /// <returns></returns>
    IQueryable<TEntity> Page(int? pageNumber, int? pageSize);
    /// <summary>
    /// LIMIT {<paramref name="offset"/>},{<paramref name="rows"/>}
    /// </summary>
    /// <param name="offset">Offset to use for this query.</param>
    /// <param name="rows">The number of rows queried.</param>
    /// <returns></returns>
    IQueryable<TEntity> Offset(int? offset, int? rows);

    IQueryable<TEntity> SetParameter(object param);
}