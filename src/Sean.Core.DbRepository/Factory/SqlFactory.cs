namespace Sean.Core.DbRepository;

public class SqlFactory
{
    private readonly DatabaseType _dbType;

    public SqlFactory(DatabaseType dbType)
    {
        _dbType = dbType;
    }

    public IReplaceable<TEntity> CreateReplaceableBuilder<TEntity>(bool autoIncludeFields, string tableName = null)
    {
        return ReplaceableSqlBuilder<TEntity>.Create(_dbType, autoIncludeFields, tableName);
    }
    public IInsertable<TEntity> CreateInsertableBuilder<TEntity>(bool autoIncludeFields, string tableName = null)
    {
        return InsertableSqlBuilder<TEntity>.Create(_dbType, autoIncludeFields, tableName);
    }
    public IDeleteable<TEntity> CreateDeleteableBuilder<TEntity>(string tableName = null)
    {
        return DeleteableSqlBuilder<TEntity>.Create(_dbType, tableName);
    }
    public IUpdateable<TEntity> CreateUpdateableBuilder<TEntity>(bool autoIncludeFields, string tableName = null)
    {
        return UpdateableSqlBuilder<TEntity>.Create(_dbType, autoIncludeFields, tableName);
    }
    public IQueryable<TEntity> CreateQueryableBuilder<TEntity>(bool autoIncludeFields, string tableName = null)
    {
        return QueryableSqlBuilder<TEntity>.Create(_dbType, autoIncludeFields, tableName);
    }
    public ICountable<TEntity> CreateCountableBuilder<TEntity>(string tableName = null)
    {
        return CountableSqlBuilder<TEntity>.Create(_dbType, tableName);
    }
    public SqlWhereClauseBuilder<TEntity> CreateSqlWhereClauseBuilder<TEntity>(TEntity entity = default)
    {
        return SqlWhereClauseBuilder<TEntity>.Create(_dbType, entity);
    }

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
    public static SqlWhereClauseBuilder<TEntity> CreateSqlWhereClauseBuilder<TEntity>(DatabaseType dbType, TEntity entity = default)
    {
        return SqlWhereClauseBuilder<TEntity>.Create(dbType, entity);
    }
}