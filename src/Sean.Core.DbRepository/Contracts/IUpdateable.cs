using System;
using System.Linq.Expressions;

namespace Sean.Core.DbRepository;

public interface IUpdateable<TEntity> : IBaseSqlBuilder, ISqlWhere<TEntity, IUpdateable<TEntity>>, ISqlJoin<TEntity, IUpdateable<TEntity>>
{
    #region [Field]
    /// <summary>
    /// UPDATE fields.
    /// </summary>
    /// <param name="fields">Database table field name.</param>
    /// <returns></returns>
    IUpdateable<TEntity> UpdateFields(params string[] fields);
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
    /// UPDATE fields.
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <returns></returns>
    IUpdateable<TEntity> UpdateFields(Expression<Func<TEntity, object>> fieldExpression);
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

    /// <summary>
    /// Whether to allow empty WHERE clauses.
    /// <para>Note: By default, empty WHERE clauses are not allowed in order to prevent the execution of incorrect SQL from causing irreversible results.</para>
    /// </summary>
    /// <param name="allowEmptyWhereClause"></param>
    /// <returns></returns>
    IUpdateable<TEntity> AllowEmptyWhereClause(bool allowEmptyWhereClause = true);

    IUpdateable<TEntity> SetParameter(object param);
}