namespace Sean.Core.DbRepository;

public static class SqlFactory
{
    public static IReplaceable<TEntity> CreateReplaceableBuilder<TEntity>(DatabaseType dbType, bool autoIncludeFields, string tableName = null)
    {
        return ReplaceableSqlBuilder<TEntity>.Create(dbType, autoIncludeFields, tableName);
    }
    public static IInsertable<TEntity> CreateInsertableBuilder<TEntity>(DatabaseType dbType, bool autoIncludeFields, string tableName = null)
    {
        return InsertableSqlBuilder<TEntity>.Create(dbType, autoIncludeFields, tableName);
    }
    public static IDeleteable<TEntity> CreateDeleteableBuilder<TEntity>(DatabaseType dbType, string tableName = null)
    {
        return DeleteableSqlBuilder<TEntity>.Create(dbType, tableName);
    }
    public static IUpdateable<TEntity> CreateUpdateableBuilder<TEntity>(DatabaseType dbType, bool autoIncludeFields, string tableName = null)
    {
        return UpdateableSqlBuilder<TEntity>.Create(dbType, autoIncludeFields, tableName);
    }
    public static IQueryable<TEntity> CreateQueryableBuilder<TEntity>(DatabaseType dbType, bool autoIncludeFields, string tableName = null)
    {
        return QueryableSqlBuilder<TEntity>.Create(dbType, autoIncludeFields, tableName);
    }
    public static ICountable<TEntity> CreateCountableBuilder<TEntity>(DatabaseType dbType, string tableName = null)
    {
        return CountableSqlBuilder<TEntity>.Create(dbType, tableName);
    }
    public static IWhereClause<TEntity> CreateWhereClauseBuilder<TEntity>(DatabaseType dbType = DatabaseType.Unknown)
    {
        return WhereClauseSqlBuilder<TEntity>.Create(dbType);
    }
    public static IOrderByClause<TEntity> CreateOrderByClauseBuilder<TEntity>(DatabaseType dbType = DatabaseType.Unknown)
    {
        return OrderByClauseSqlBuilder<TEntity>.Create(dbType);
    }
}