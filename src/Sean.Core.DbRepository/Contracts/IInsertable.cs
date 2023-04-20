using System;
using System.Linq.Expressions;

namespace Sean.Core.DbRepository;

public interface IInsertable<TEntity> : IBaseSqlBuilder
{
    #region [Field]
    /// <summary>
    /// Include fields.
    /// </summary>
    /// <param name="fields">Database table field name.</param>
    /// <returns></returns>
    IInsertable<TEntity> IncludeFields(params string[] fields);
    /// <summary>
    /// Ignore fields.
    /// </summary>
    /// <param name="fields">Database table field name.</param>
    /// <returns></returns>
    IInsertable<TEntity> IgnoreFields(params string[] fields);
    /// <summary>
    /// Auto-increment fields.
    /// </summary>
    /// <param name="fields">Database table field name.</param>
    /// <returns></returns>
    IInsertable<TEntity> IdentityFields(params string[] fields);

    /// <summary>
    /// Include fields.
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <returns></returns>
    IInsertable<TEntity> IncludeFields(Expression<Func<TEntity, object>> fieldExpression);
    /// <summary>
    /// Ignore fields.
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <returns></returns>
    IInsertable<TEntity> IgnoreFields(Expression<Func<TEntity, object>> fieldExpression);
    /// <summary>
    /// Auto-increment fields.
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <returns></returns>
    IInsertable<TEntity> IdentityFields(Expression<Func<TEntity, object>> fieldExpression);
    #endregion

    /// <summary>
    /// Whether to return the value of the auto-increment primary key.
    /// </summary>
    /// <param name="returnAutoIncrementId"></param>
    /// <returns></returns>
    IInsertable<TEntity> ReturnAutoIncrementId(bool returnAutoIncrementId = true);

    IInsertable<TEntity> SetParameter(object param);
}