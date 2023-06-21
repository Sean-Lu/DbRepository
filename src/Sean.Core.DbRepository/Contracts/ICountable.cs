namespace Sean.Core.DbRepository;

public interface ICountable<TEntity> : IBaseSqlBuilder, ISqlWhere<TEntity, ICountable<TEntity>>, ISqlJoin<TEntity, ICountable<TEntity>>
{
    ICountable<TEntity> SetParameter(object param);
}