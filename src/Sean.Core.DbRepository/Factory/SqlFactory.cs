namespace Sean.Core.DbRepository
{
    public class SqlFactory
    {
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
    }

    public class SqlFactory<TEntity>
    {
        public static IInsertable<TEntity> CreateInsertableBuilder(DatabaseType dbType, bool autoIncludeFields, string tableName = null)
        {
            return InsertableSqlBuilder<TEntity>.Create(dbType, autoIncludeFields, tableName);
        }
        public static IDeleteable<TEntity> CreateDeleteableBuilder(DatabaseType dbType, string tableName = null)
        {
            return DeleteableSqlBuilder<TEntity>.Create(dbType, tableName);
        }
        public static IUpdateable<TEntity> CreateUpdateableBuilder(DatabaseType dbType, bool autoIncludeFields, string tableName = null)
        {
            return UpdateableSqlBuilder<TEntity>.Create(dbType, autoIncludeFields, tableName);
        }
        public static IQueryable<TEntity> CreateQueryableBuilder(DatabaseType dbType, bool autoIncludeFields, string tableName = null)
        {
            return QueryableSqlBuilder<TEntity>.Create(dbType, autoIncludeFields, tableName);
        }
        public static ICountable<TEntity> CreateCountableBuilder(DatabaseType dbType, string tableName = null)
        {
            return CountableSqlBuilder<TEntity>.Create(dbType, tableName);
        }
    }
}
