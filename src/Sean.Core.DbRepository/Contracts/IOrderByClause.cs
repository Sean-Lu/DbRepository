namespace Sean.Core.DbRepository;

public interface IOrderByClause<TEntity> : IBaseSqlBuilder<IOrderByClause<TEntity>>, ISqlOrderBy<TEntity, IOrderByClause<TEntity>>
{
    IOrderByClause<TEntity> IsMultiTable(bool isMultiTable);
    IOrderByClause<TEntity> IncludeKeyword(bool includeKeyword);
}