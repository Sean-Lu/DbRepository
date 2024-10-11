using System;
using System.Linq.Expressions;

namespace Sean.Core.DbRepository;

public interface ISqlOrderBy<TEntity, out TResult>
{
    #region [ORDER BY]
    /// <summary>
    /// ORDER BY column_name,column_name ASC|DESC;
    /// </summary>
    /// <param name="orderBy">The [ORDER BY] keyword is not included.</param>
    /// <returns></returns>
    TResult OrderBy(string orderBy);
    TResult OrderBy(OrderByCondition orderBy);
    TResult OrderBy(OrderByType type, params string[] fieldNames);
    TResult OrderBy(OrderByType type, Expression<Func<TEntity, object>> fieldExpression);
    TResult OrderBy<TEntity2>(OrderByType type, Expression<Func<TEntity2, object>> fieldExpression, string aliasName = null);
    #endregion
}