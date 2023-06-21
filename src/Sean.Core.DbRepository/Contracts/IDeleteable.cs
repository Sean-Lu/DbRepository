namespace Sean.Core.DbRepository;

public interface IDeleteable<TEntity> : IBaseSqlBuilder, ISqlWhere<TEntity, IDeleteable<TEntity>>, ISqlJoin<TEntity, IDeleteable<TEntity>>
{
    /// <summary>
    /// Whether to allow empty WHERE clauses.
    /// <para>Note: By default, empty WHERE clauses are not allowed in order to prevent the execution of incorrect SQL from causing irreversible results.</para>
    /// </summary>
    /// <param name="allowEmptyWhereClause"></param>
    /// <returns></returns>
    IDeleteable<TEntity> AllowEmptyWhereClause(bool allowEmptyWhereClause = true);

    IDeleteable<TEntity> SetParameter(object param);
}