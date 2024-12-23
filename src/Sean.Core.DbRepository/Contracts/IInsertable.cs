﻿using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Sean.Core.DbRepository;

public interface IInsertable<TEntity> : IBaseSqlBuilder<IInsertable<TEntity>>
{
    #region [Field]
    /// <summary>
    /// INSERT fields.
    /// </summary>
    /// <param name="fields">Database table field name.</param>
    /// <returns></returns>
    IInsertable<TEntity> InsertFields(params string[] fields);
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
    /// INSERT fields.
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <returns></returns>
    IInsertable<TEntity> InsertFields(Expression<Func<TEntity, object>> fieldExpression);
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

    IInsertable<TEntity> OutputParameter(TEntity outputTarget, PropertyInfo outputPropertyInfo);
    IInsertable<TEntity> OutputParameterIF(bool condition, TEntity outputTarget, PropertyInfo outputPropertyInfo);

    IInsertable<TEntity> SetParameter(object param);
}