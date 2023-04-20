using System;
using System.Linq.Expressions;

namespace Sean.Core.DbRepository;

public interface ICountable<TEntity> : IBaseSqlBuilder
{
    #region [Join Table]
    /// <summary>
    /// INNER JOIN
    /// </summary>
    /// <param name="joinTableSql">The [INNER JOIN] keyword is not included.</param>
    /// <returns></returns>
    ICountable<TEntity> InnerJoin(string joinTableSql);
    /// <summary>
    /// LEFT JOIN
    /// </summary>
    /// <param name="joinTableSql">The [LEFT JOIN] keyword is not included.</param>
    /// <returns></returns>
    ICountable<TEntity> LeftJoin(string joinTableSql);
    /// <summary>
    /// RIGHT JOIN
    /// </summary>
    /// <param name="joinTableSql">The [RIGHT JOIN] keyword is not included.</param>
    /// <returns></returns>
    ICountable<TEntity> RightJoin(string joinTableSql);
    /// <summary>
    /// FULL JOIN
    /// </summary>
    /// <param name="joinTableSql">The [FULL JOIN] keyword is not included.</param>
    /// <returns></returns>
    ICountable<TEntity> FullJoin(string joinTableSql);

    /// <summary>
    /// INNER JOIN table_name2 ON table_name1.column_name=table_name2.column_name
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <param name="fieldExpression2"></param>
    /// <returns></returns>
    ICountable<TEntity> InnerJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2);
    /// <summary>
    /// LEFT JOIN table_name2 ON table_name1.column_name=table_name2.column_name
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <param name="fieldExpression2"></param>
    /// <returns></returns>
    ICountable<TEntity> LeftJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2);
    /// <summary>
    /// RIGHT JOIN table_name2 ON table_name1.column_name=table_name2.column_name
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <param name="fieldExpression2"></param>
    /// <returns></returns>
    ICountable<TEntity> RightJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2);
    /// <summary>
    /// FULL JOIN table_name2 ON table_name1.column_name=table_name2.column_name
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <param name="fieldExpression2"></param>
    /// <returns></returns>
    ICountable<TEntity> FullJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2);
    #endregion

    #region [WHERE]
    /// <summary>
    /// WHERE column_name operator value
    /// </summary>
    /// <param name="where">The [WHERE] keyword is not included.</param>
    /// <returns></returns>
    ICountable<TEntity> Where(string where);
    /// <summary>
    /// WHERE column_name operator value
    /// </summary>
    /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
    /// <returns></returns>
    ICountable<TEntity> Where(Expression<Func<TEntity, bool>> whereExpression);
    ICountable<TEntity> Where<TEntity2>(Expression<Func<TEntity2, bool>> whereExpression);

    /// <summary>
    /// WHERE column_name operator value
    /// </summary>
    /// <param name="operation"></param>
    /// <param name="keyword"></param>
    /// <param name="include"></param>
    /// <param name="paramName"></param>
    /// <returns></returns>
    ICountable<TEntity> WhereField(Expression<Func<TEntity, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null);
    ICountable<TEntity> WhereField<TEntity2>(Expression<Func<TEntity2, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null);

    ICountable<TEntity> AndWhere(string where);
    ICountable<TEntity> AndWhere(Expression<Func<TEntity, bool>> whereExpression);
    ICountable<TEntity> AndWhere<TEntity2>(Expression<Func<TEntity2, bool>> whereExpression);
    #endregion

    ICountable<TEntity> SetParameter(object param);
}