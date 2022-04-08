namespace Sean.Core.DbRepository
{
    public class SqlFactory
    {
        public static IInsertable<TEntity> CreateInsertable<TEntity>(DatabaseType dbType, bool autoIncludeFields, string tableName = null)
        {
            return InsertableSqlBuilder<TEntity>.Create(dbType, autoIncludeFields, tableName);
        }
        public static IDeleteable<TEntity> CreateDeleteable<TEntity>(DatabaseType dbType, string tableName = null)
        {
            return DeleteableSqlBuilder<TEntity>.Create(dbType, tableName);
        }
        public static IUpdateable<TEntity> CreateUpdateable<TEntity>(DatabaseType dbType, bool autoIncludeFields, string tableName = null)
        {
            return UpdateableSqlBuilder<TEntity>.Create(dbType, autoIncludeFields, tableName);
        }
        public static IQueryable<TEntity> CreateQueryable<TEntity>(DatabaseType dbType, bool autoIncludeFields, string tableName = null)
        {
            return QueryableSqlBuilder<TEntity>.Create(dbType, autoIncludeFields, tableName);
        }
        public static ICountable<TEntity> CreateCountable<TEntity>(DatabaseType dbType, string tableName = null)
        {
            return CountableSqlBuilder<TEntity>.Create(dbType, tableName);
        }
    }

    public class SqlFactory<TEntity>
    {
        public static IInsertable<TEntity> CreateInsertable(DatabaseType dbType, bool autoIncludeFields, string tableName = null)
        {
            return InsertableSqlBuilder<TEntity>.Create(dbType, autoIncludeFields, tableName);
        }
        public static IDeleteable<TEntity> CreateDeleteable(DatabaseType dbType, string tableName = null)
        {
            return DeleteableSqlBuilder<TEntity>.Create(dbType, tableName);
        }
        public static IUpdateable<TEntity> CreateUpdateable(DatabaseType dbType, bool autoIncludeFields, string tableName = null)
        {
            return UpdateableSqlBuilder<TEntity>.Create(dbType, autoIncludeFields, tableName);
        }
        public static IQueryable<TEntity> CreateQueryable(DatabaseType dbType, bool autoIncludeFields, string tableName = null)
        {
            return QueryableSqlBuilder<TEntity>.Create(dbType, autoIncludeFields, tableName);
        }
        public static ICountable<TEntity> CreateCountable(DatabaseType dbType, string tableName = null)
        {
            return CountableSqlBuilder<TEntity>.Create(dbType, tableName);
        }
    }
}
