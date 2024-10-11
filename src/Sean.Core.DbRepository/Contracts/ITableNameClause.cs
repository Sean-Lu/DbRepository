namespace Sean.Core.DbRepository;

public interface ITableNameClause<TEntity> : IBaseSqlBuilder<ITableNameClause<TEntity>>, ISqlJoin<TEntity, ITableNameClause<TEntity>>
{
    ITableNameClause<TEntity> IncludeKeyword(bool includeKeyword);
}