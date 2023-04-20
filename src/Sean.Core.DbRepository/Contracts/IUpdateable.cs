using System;
using System.Linq.Expressions;

namespace Sean.Core.DbRepository;

public interface IUpdateable<TEntity> : IBaseSqlBuilder
{
    #region [Field]
    /// <summary>
    /// Include fields.
    /// </summary>
    /// <param name="fields">Database table field name.</param>
    /// <returns></returns>
    IUpdateable<TEntity> IncludeFields(params string[] fields);
    /// <summary>
    /// Ignore fields.
    /// </summary>
    /// <param name="fields">Database table field name.</param>
    /// <returns></returns>
    IUpdateable<TEntity> IgnoreFields(params string[] fields);
    /// <summary>
    /// Primary key fields.
    /// </summary>
    /// <param name="fields">Database table field name.</param>
    /// <returns></returns>
    IUpdateable<TEntity> PrimaryKeyFields(params string[] fields);

    /// <summary>
    /// Include fields.
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <param name="entity"></param>
    /// <returns></returns>
    IUpdateable<TEntity> IncludeFields(Expression<Func<TEntity, object>> fieldExpression, TEntity entity = default);
    /// <summary>
    /// Ignore fields.
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <returns></returns>
    IUpdateable<TEntity> IgnoreFields(Expression<Func<TEntity, object>> fieldExpression);
    /// <summary>
    /// Primary key fields.
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <returns></returns>
    IUpdateable<TEntity> PrimaryKeyFields(Expression<Func<TEntity, object>> fieldExpression);

    /// <summary>
    /// Increment fields.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="fieldExpression"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    IUpdateable<TEntity> IncrementFields<TValue>(Expression<Func<TEntity, object>> fieldExpression, TValue value) where TValue : struct;
    /// <summary>
    /// Decrement fields.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="fieldExpression"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    IUpdateable<TEntity> DecrementFields<TValue>(Expression<Func<TEntity, object>> fieldExpression, TValue value) where TValue : struct;
    #endregion

    #region [Join Table]
    /// <summary>
    /// INNER JOIN
    /// </summary>
    /// <param name="joinTableSql">The [INNER JOIN] keyword is not included.</param>
    /// <returns></returns>
    IUpdateable<TEntity> InnerJoin(string joinTableSql);
    /// <summary>
    /// LEFT JOIN
    /// </summary>
    /// <param name="joinTableSql">The [LEFT JOIN] keyword is not included.</param>
    /// <returns></returns>
    IUpdateable<TEntity> LeftJoin(string joinTableSql);
    /// <summary>
    /// RIGHT JOIN
    /// </summary>
    /// <param name="joinTableSql">The [RIGHT JOIN] keyword is not included.</param>
    /// <returns></returns>
    IUpdateable<TEntity> RightJoin(string joinTableSql);
    /// <summary>
    /// FULL JOIN
    /// </summary>
    /// <param name="joinTableSql">The [FULL JOIN] keyword is not included.</param>
    /// <returns></returns>
    IUpdateable<TEntity> FullJoin(string joinTableSql);

    /// <summary>
    /// INNER JOIN table_name2 ON table_name1.column_name=table_name2.column_name
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <param name="fieldExpression2"></param>
    /// <returns></returns>
    IUpdateable<TEntity> InnerJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2);
    /// <summary>
    /// LEFT JOIN table_name2 ON table_name1.column_name=table_name2.column_name
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <param name="fieldExpression2"></param>
    /// <returns></returns>
    IUpdateable<TEntity> LeftJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2);
    /// <summary>
    /// RIGHT JOIN table_name2 ON table_name1.column_name=table_name2.column_name
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <param name="fieldExpression2"></param>
    /// <returns></returns>
    IUpdateable<TEntity> RightJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2);
    /// <summary>
    /// FULL JOIN table_name2 ON table_name1.column_name=table_name2.column_name
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <param name="fieldExpression2"></param>
    /// <returns></returns>
    IUpdateable<TEntity> FullJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2);
    #endregion

    #region [WHERE]
    /// <summary>
    /// WHERE column_name operator value
    /// </summary>
    /// <param name="where">The [WHERE] keyword is not included.</param>
    /// <returns></returns>
    IUpdateable<TEntity> Where(string where);
    /// <summary>
    /// WHERE column_name operator value
    /// </summary>
    /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
    /// <returns></returns>
    IUpdateable<TEntity> Where(Expression<Func<TEntity, bool>> whereExpression);
    IUpdateable<TEntity> Where<TEntity2>(Expression<Func<TEntity2, bool>> whereExpression);

    /// <summary>
    /// WHERE column_name operator value
    /// </summary>
    /// <param name="operation"></param>
    /// <param name="keyword"></param>
    /// <param name="include"></param>
    /// <param name="paramName"></param>
    /// <returns></returns>
    IUpdateable<TEntity> WhereField(Expression<Func<TEntity, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null);
    IUpdateable<TEntity> WhereField<TEntity2>(Expression<Func<TEntity2, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null);

    IUpdateable<TEntity> AndWhere(string where);
    IUpdateable<TEntity> AndWhere(Expression<Func<TEntity, bool>> whereExpression);
    IUpdateable<TEntity> AndWhere<TEntity2>(Expression<Func<TEntity2, bool>> whereExpression);
    #endregion

    /// <summary>
    /// Whether to allow empty WHERE clauses.
    /// <para>Note: By default, empty WHERE clauses are not allowed in order to prevent the execution of incorrect SQL from causing irreversible results.</para>
    /// </summary>
    /// <param name="allowEmptyWhereClause"></param>
    /// <returns></returns>
    IUpdateable<TEntity> AllowEmptyWhereClause(bool allowEmptyWhereClause = true);

    IUpdateable<TEntity> SetParameter(object param);
}