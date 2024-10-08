namespace Sean.Core.DbRepository;

public interface ICountable<TEntity> : IBaseSqlBuilder<ICountable<TEntity>>,
    ISqlJoin<TEntity, ICountable<TEntity>>,
    ISqlWhere<TEntity, ICountable<TEntity>>
{
    ICountable<TEntity> SetParameter(object param);
}