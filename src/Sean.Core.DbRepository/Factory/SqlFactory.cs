namespace Sean.Core.DbRepository;

public static class SqlFactory
{
    public static IReplaceable<TEntity> CreateReplaceableBuilder<TEntity>(bool autoIncludeFields, DatabaseType dbType = DatabaseType.Unknown)
    {
        return ReplaceableSqlBuilder<TEntity>.Create(dbType, autoIncludeFields);
    }
    public static IInsertable<TEntity> CreateInsertableBuilder<TEntity>(bool autoIncludeFields, DatabaseType dbType = DatabaseType.Unknown)
    {
        return InsertableSqlBuilder<TEntity>.Create(dbType, autoIncludeFields);
    }
    public static IDeleteable<TEntity> CreateDeleteableBuilder<TEntity>(DatabaseType dbType = DatabaseType.Unknown)
    {
        return DeleteableSqlBuilder<TEntity>.Create(dbType);
    }
    public static IUpdateable<TEntity> CreateUpdateableBuilder<TEntity>(bool autoIncludeFields, DatabaseType dbType = DatabaseType.Unknown)
    {
        return UpdateableSqlBuilder<TEntity>.Create(dbType, autoIncludeFields);
    }
    public static IQueryable<TEntity> CreateQueryableBuilder<TEntity>(bool autoIncludeFields, DatabaseType dbType = DatabaseType.Unknown)
    {
        return QueryableSqlBuilder<TEntity>.Create(dbType, autoIncludeFields);
    }
    public static ICountable<TEntity> CreateCountableBuilder<TEntity>(DatabaseType dbType = DatabaseType.Unknown)
    {
        return CountableSqlBuilder<TEntity>.Create(dbType);
    }

    public static ITableNameClause<TEntity> CreateTableNameClauseBuilder<TEntity>(DatabaseType dbType = DatabaseType.Unknown)
    {
        return TableNameClauseSqlBuilder<TEntity>.Create(dbType);
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