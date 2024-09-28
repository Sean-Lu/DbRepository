namespace Sean.Core.DbRepository;

public interface IWhereClause<TEntity> : IBaseSqlBuilder<IWhereClause<TEntity>>, ISqlWhere<TEntity, IWhereClause<TEntity>>
{
    IWhereClause<TEntity> SetParameter(object param);
}