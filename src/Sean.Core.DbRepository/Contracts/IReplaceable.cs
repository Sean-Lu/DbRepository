using System;
using System.Linq.Expressions;

namespace Sean.Core.DbRepository;

public interface IReplaceable<TEntity> : IBaseSqlBuilder
{
    #region [Field]
    /// <summary>
    /// Include fields.
    /// </summary>
    /// <param name="fields">Database table field name.</param>
    /// <returns></returns>
    IReplaceable<TEntity> IncludeFields(params string[] fields);
    /// <summary>
    /// Ignore fields.
    /// </summary>
    /// <param name="fields">Database table field name.</param>
    /// <returns></returns>
    IReplaceable<TEntity> IgnoreFields(params string[] fields);

    /// <summary>
    /// Include fields.
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <returns></returns>
    IReplaceable<TEntity> IncludeFields(Expression<Func<TEntity, object>> fieldExpression);
    /// <summary>
    /// Ignore fields.
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <returns></returns>
    IReplaceable<TEntity> IgnoreFields(Expression<Func<TEntity, object>> fieldExpression);
    #endregion

    IReplaceable<TEntity> SetParameter(object param);
}