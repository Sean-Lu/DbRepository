using System;
using System.Linq.Expressions;

namespace Sean.Core.DbRepository;

public interface ISqlGroupBy<TEntity, out TResult>
{
    #region [GROUP BY]
    /// <summary>
    /// GROUP BY column_name
    /// </summary>
    /// <param name="groupBy">The [GROUP BY] keyword is not included.</param>
    /// <returns></returns>
    TResult GroupBy(string groupBy);
    TResult GroupBy(Expression<Func<TEntity, object>> fieldExpression);
    #endregion
}