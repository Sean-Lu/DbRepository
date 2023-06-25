using System.Linq.Expressions;
using System;

namespace Sean.Core.DbRepository;

public interface ISqlWhere<TEntity, out TResult>
{
    #region [WHERE]
    /// <summary>
    /// WHERE column_name operator value
    /// </summary>
    /// <param name="where">The [WHERE] keyword is not included.</param>
    /// <returns></returns>
    TResult Where(string where);

    /// <summary>
    /// WHERE column_name operator value
    /// </summary>
    /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
    /// <returns></returns>
    TResult Where(Expression<Func<TEntity, bool>> whereExpression);
    TResult Where<TEntity2>(Expression<Func<TEntity2, bool>> whereExpression);

    TResult WhereIF(bool condition, Expression<Func<TEntity, bool>> whereExpression);
    TResult WhereIF(bool condition, Expression<Func<TEntity, bool>> trueWhereExpression, Expression<Func<TEntity, bool>> falseWhereExpression);
    TResult WhereIF<TEntity2>(bool condition, Expression<Func<TEntity2, bool>> whereExpression);
    TResult WhereIF<TEntity2>(bool condition, Expression<Func<TEntity2, bool>> trueWhereExpression, Expression<Func<TEntity2, bool>> falseWhereExpression);
    TResult WhereIF<TEntity2, TEntity3>(bool condition, Expression<Func<TEntity2, bool>> trueWhereExpression, Expression<Func<TEntity3, bool>> falseWhereExpression);

    /// <summary>
    /// WHERE column_name operator value
    /// </summary>
    /// <param name="operation"></param>
    /// <param name="keyword"></param>
    /// <param name="include"></param>
    /// <param name="paramName"></param>
    /// <returns></returns>
    TResult WhereField(Expression<Func<TEntity, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null);
    TResult WhereField<TEntity2>(Expression<Func<TEntity2, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null);
    #endregion
}