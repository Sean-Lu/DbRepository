using System;
using System.Linq.Expressions;

namespace Sean.Core.DbRepository;

public interface IDeleteable<TEntity> : IBaseSqlBuilder
{
    #region [Join Table]
    /// <summary>
    /// INNER JOIN
    /// </summary>
    /// <param name="joinTableSql">The [INNER JOIN] keyword is not included.</param>
    /// <returns></returns>
    IDeleteable<TEntity> InnerJoin(string joinTableSql);
    /// <summary>
    /// LEFT JOIN
    /// </summary>
    /// <param name="joinTableSql">The [LEFT JOIN] keyword is not included.</param>
    /// <returns></returns>
    IDeleteable<TEntity> LeftJoin(string joinTableSql);
    /// <summary>
    /// RIGHT JOIN
    /// </summary>
    /// <param name="joinTableSql">The [RIGHT JOIN] keyword is not included.</param>
    /// <returns></returns>
    IDeleteable<TEntity> RightJoin(string joinTableSql);
    /// <summary>
    /// FULL JOIN
    /// </summary>
    /// <param name="joinTableSql">The [FULL JOIN] keyword is not included.</param>
    /// <returns></returns>
    IDeleteable<TEntity> FullJoin(string joinTableSql);

    /// <summary>
    /// INNER JOIN table_name2 ON table_name1.column_name=table_name2.column_name
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <param name="fieldExpression2"></param>
    /// <returns></returns>
    IDeleteable<TEntity> InnerJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2);
    /// <summary>
    /// LEFT JOIN table_name2 ON table_name1.column_name=table_name2.column_name
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <param name="fieldExpression2"></param>
    /// <returns></returns>
    IDeleteable<TEntity> LeftJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2);
    /// <summary>
    /// RIGHT JOIN table_name2 ON table_name1.column_name=table_name2.column_name
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <param name="fieldExpression2"></param>
    /// <returns></returns>
    IDeleteable<TEntity> RightJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2);
    /// <summary>
    /// FULL JOIN table_name2 ON table_name1.column_name=table_name2.column_name
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <param name="fieldExpression2"></param>
    /// <returns></returns>
    IDeleteable<TEntity> FullJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2);
    #endregion

    #region [WHERE]
    /// <summary>
    /// WHERE column_name operator value
    /// </summary>
    /// <param name="where">The [WHERE] keyword is not included.</param>
    /// <returns></returns>
    IDeleteable<TEntity> Where(string where);
    /// <summary>
    /// WHERE column_name operator value
    /// </summary>
    /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
    /// <returns></returns>
    IDeleteable<TEntity> Where(Expression<Func<TEntity, bool>> whereExpression);
    IDeleteable<TEntity> Where<TEntity2>(Expression<Func<TEntity2, bool>> whereExpression);

    /// <summary>
    /// WHERE column_name operator value
    /// </summary>
    /// <param name="operation"></param>
    /// <param name="keyword"></param>
    /// <param name="include"></param>
    /// <param name="paramName"></param>
    /// <returns></returns>
    IDeleteable<TEntity> WhereField(Expression<Func<TEntity, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null);
    IDeleteable<TEntity> WhereField<TEntity2>(Expression<Func<TEntity2, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null);

    IDeleteable<TEntity> AndWhere(string where);
    IDeleteable<TEntity> AndWhere(Expression<Func<TEntity, bool>> whereExpression);
    IDeleteable<TEntity> AndWhere<TEntity2>(Expression<Func<TEntity2, bool>> whereExpression);
    #endregion

    /// <summary>
    /// Whether to allow empty WHERE clauses.
    /// <para>Note: By default, empty WHERE clauses are not allowed in order to prevent the execution of incorrect SQL from causing irreversible results.</para>
    /// </summary>
    /// <param name="allowEmptyWhereClause"></param>
    /// <returns></returns>
    IDeleteable<TEntity> AllowEmptyWhereClause(bool allowEmptyWhereClause = true);

    IDeleteable<TEntity> SetParameter(object param);
}