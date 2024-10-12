using System;
using System.Linq.Expressions;

namespace Sean.Core.DbRepository;

public interface ISqlJoin<TEntity, out TResult>
{
    #region [Join Table]
    /// <summary>
    /// INNER JOIN
    /// </summary>
    /// <param name="joinTableSql">The [INNER JOIN] keyword is not included.</param>
    /// <returns></returns>
    TResult InnerJoin(string joinTableSql);
    /// <summary>
    /// LEFT JOIN
    /// </summary>
    /// <param name="joinTableSql">The [LEFT JOIN] keyword is not included.</param>
    /// <returns></returns>
    TResult LeftJoin(string joinTableSql);
    /// <summary>
    /// RIGHT JOIN
    /// </summary>
    /// <param name="joinTableSql">The [RIGHT JOIN] keyword is not included.</param>
    /// <returns></returns>
    TResult RightJoin(string joinTableSql);
    /// <summary>
    /// FULL JOIN
    /// </summary>
    /// <param name="joinTableSql">The [FULL JOIN] keyword is not included.</param>
    /// <returns></returns>
    TResult FullJoin(string joinTableSql);

    /// <summary>
    /// INNER JOIN table_name2 ON table_name1.column_name=table_name2.column_name
    /// </summary>
    /// <param name="leftTableFieldExpression"></param>
    /// <param name="rightTableFieldExpression"></param>
    /// <returns></returns>
    TResult InnerJoin<TEntity2>(Expression<Func<TEntity, object>> leftTableFieldExpression, Expression<Func<TEntity2, object>> rightTableFieldExpression, string rightTableAliasName = null);
    /// <summary>
    /// LEFT JOIN table_name2 ON table_name1.column_name=table_name2.column_name
    /// </summary>
    /// <param name="leftTableFieldExpression"></param>
    /// <param name="rightTableFieldExpression"></param>
    /// <returns></returns>
    TResult LeftJoin<TEntity2>(Expression<Func<TEntity, object>> leftTableFieldExpression, Expression<Func<TEntity2, object>> rightTableFieldExpression, string rightTableAliasName = null);
    /// <summary>
    /// RIGHT JOIN table_name2 ON table_name1.column_name=table_name2.column_name
    /// </summary>
    /// <param name="leftTableFieldExpression"></param>
    /// <param name="rightTableFieldExpression"></param>
    /// <returns></returns>
    TResult RightJoin<TEntity2>(Expression<Func<TEntity, object>> leftTableFieldExpression, Expression<Func<TEntity2, object>> rightTableFieldExpression, string rightTableAliasName = null);
    /// <summary>
    /// FULL JOIN table_name2 ON table_name1.column_name=table_name2.column_name
    /// </summary>
    /// <param name="leftTableFieldExpression"></param>
    /// <param name="rightTableFieldExpression"></param>
    /// <returns></returns>
    TResult FullJoin<TEntity2>(Expression<Func<TEntity, object>> leftTableFieldExpression, Expression<Func<TEntity2, object>> rightTableFieldExpression, string rightTableAliasName = null);

    /// <summary>
    /// INNER JOIN table_name2 ON table_name1.column_name=table_name2.column_name
    /// </summary>
    /// <param name="leftTableFieldExpression"></param>
    /// <param name="rightTableFieldExpression"></param>
    /// <returns></returns>
    TResult InnerJoin<TEntity2, TEntity3>(Expression<Func<TEntity2, object>> leftTableFieldExpression, Expression<Func<TEntity3, object>> rightTableFieldExpression, string leftTableAliasName = null, string rightTableAliasName = null);
    /// <summary>
    /// LEFT JOIN table_name2 ON table_name1.column_name=table_name2.column_name
    /// </summary>
    /// <param name="leftTableFieldExpression"></param>
    /// <param name="rightTableFieldExpression"></param>
    /// <returns></returns>
    TResult LeftJoin<TEntity2, TEntity3>(Expression<Func<TEntity2, object>> leftTableFieldExpression, Expression<Func<TEntity3, object>> rightTableFieldExpression, string leftTableAliasName = null, string rightTableAliasName = null);
    /// <summary>
    /// RIGHT JOIN table_name2 ON table_name1.column_name=table_name2.column_name
    /// </summary>
    /// <param name="leftTableFieldExpression"></param>
    /// <param name="rightTableFieldExpression"></param>
    /// <returns></returns>
    TResult RightJoin<TEntity2, TEntity3>(Expression<Func<TEntity2, object>> leftTableFieldExpression, Expression<Func<TEntity3, object>> rightTableFieldExpression, string leftTableAliasName = null, string rightTableAliasName = null);
    /// <summary>
    /// FULL JOIN table_name2 ON table_name1.column_name=table_name2.column_name
    /// </summary>
    /// <param name="leftTableFieldExpression"></param>
    /// <param name="rightTableFieldExpression"></param>
    /// <returns></returns>
    TResult FullJoin<TEntity2, TEntity3>(Expression<Func<TEntity2, object>> leftTableFieldExpression, Expression<Func<TEntity3, object>> rightTableFieldExpression, string leftTableAliasName = null, string rightTableAliasName = null);
    #endregion
}