using System;
using System.Linq.Expressions;

namespace Sean.Core.DbRepository;

public interface IQueryable<TEntity> : IBaseSqlBuilder
{
    #region [Field]
    /// <summary>
    /// Include fields.
    /// </summary>
    /// <param name="fields">Database table field name.</param>
    /// <returns></returns>
    IQueryable<TEntity> IncludeFields(params string[] fields);
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
    /// Include fields.
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <returns></returns>
    IQueryable<TEntity> IncludeFields(Expression<Func<TEntity, object>> fieldExpression);
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

    #region [Join Table]
    /// <summary>
    /// INNER JOIN
    /// </summary>
    /// <param name="joinTableSql">The [INNER JOIN] keyword is not included.</param>
    /// <returns></returns>
    IQueryable<TEntity> InnerJoin(string joinTableSql);
    /// <summary>
    /// LEFT JOIN
    /// </summary>
    /// <param name="joinTableSql">The [LEFT JOIN] keyword is not included.</param>
    /// <returns></returns>
    IQueryable<TEntity> LeftJoin(string joinTableSql);
    /// <summary>
    /// RIGHT JOIN
    /// </summary>
    /// <param name="joinTableSql">The [RIGHT JOIN] keyword is not included.</param>
    /// <returns></returns>
    IQueryable<TEntity> RightJoin(string joinTableSql);
    /// <summary>
    /// FULL JOIN
    /// </summary>
    /// <param name="joinTableSql">The [FULL JOIN] keyword is not included.</param>
    /// <returns></returns>
    IQueryable<TEntity> FullJoin(string joinTableSql);

    /// <summary>
    /// INNER JOIN table_name2 ON table_name1.column_name=table_name2.column_name
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <param name="fieldExpression2"></param>
    /// <returns></returns>
    IQueryable<TEntity> InnerJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2);
    /// <summary>
    /// LEFT JOIN table_name2 ON table_name1.column_name=table_name2.column_name
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <param name="fieldExpression2"></param>
    /// <returns></returns>
    IQueryable<TEntity> LeftJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2);
    /// <summary>
    /// RIGHT JOIN table_name2 ON table_name1.column_name=table_name2.column_name
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <param name="fieldExpression2"></param>
    /// <returns></returns>
    IQueryable<TEntity> RightJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2);
    /// <summary>
    /// FULL JOIN table_name2 ON table_name1.column_name=table_name2.column_name
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <param name="fieldExpression2"></param>
    /// <returns></returns>
    IQueryable<TEntity> FullJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2);
    #endregion

    #region [WHERE]
    /// <summary>
    /// WHERE column_name operator value
    /// </summary>
    /// <param name="where">The [WHERE] keyword is not included.</param>
    /// <returns></returns>
    IQueryable<TEntity> Where(string where);
    /// <summary>
    /// WHERE column_name operator value
    /// </summary>
    /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
    /// <returns></returns>
    IQueryable<TEntity> Where(Expression<Func<TEntity, bool>> whereExpression);
    IQueryable<TEntity> Where<TEntity2>(Expression<Func<TEntity2, bool>> whereExpression);

    /// <summary>
    /// WHERE column_name operator value
    /// </summary>
    /// <param name="operation"></param>
    /// <param name="keyword"></param>
    /// <param name="include"></param>
    /// <param name="paramName"></param>
    /// <returns></returns>
    IQueryable<TEntity> WhereField(Expression<Func<TEntity, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null);
    IQueryable<TEntity> WhereField<TEntity2>(Expression<Func<TEntity2, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null);

    IQueryable<TEntity> AndWhere(string where);
    IQueryable<TEntity> AndWhere(Expression<Func<TEntity, bool>> whereExpression);
    IQueryable<TEntity> AndWhere<TEntity2>(Expression<Func<TEntity2, bool>> whereExpression);
    #endregion

    #region [GROUP BY]
    /// <summary>
    /// GROUP BY column_name
    /// </summary>
    /// <param name="groupBy">The [GROUP BY] keyword is not included.</param>
    /// <returns></returns>
    IQueryable<TEntity> GroupBy(string groupBy);
    IQueryable<TEntity> GroupByField(params string[] fieldNames);
    IQueryable<TEntity> GroupByField(Expression<Func<TEntity, object>> fieldExpression);
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
    IQueryable<TEntity> OrderByField(OrderByType type, params string[] fieldNames);
    IQueryable<TEntity> OrderByField(OrderByType type, Expression<Func<TEntity, object>> fieldExpression);
    #endregion

    /// <summary>
    /// LIMIT <paramref name="top"/>
    /// </summary>
    /// <param name="top"></param>
    /// <returns></returns>
    IQueryable<TEntity> Top(int? top);
    /// <summary>
    /// LIMIT {(<paramref name="pageIndex"/> - 1) * <paramref name="pageSize"/>},{<paramref name="pageSize"/>}
    /// </summary>
    /// <param name="pageIndex">The current page index for paging query, the minimum value is 1.</param>
    /// <param name="pageSize">The page size for paging query.</param>
    /// <returns></returns>
    IQueryable<TEntity> Page(int? pageIndex, int? pageSize);
    /// <summary>
    /// LIMIT {<paramref name="offset"/>},{<paramref name="rows"/>}
    /// </summary>
    /// <param name="offset">Offset to use for this query.</param>
    /// <param name="rows">The number of rows queried.</param>
    /// <returns></returns>
    IQueryable<TEntity> Offset(int? offset, int? rows);

    IQueryable<TEntity> SetParameter(object param);
}