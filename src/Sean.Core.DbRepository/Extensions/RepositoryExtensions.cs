namespace Sean.Core.DbRepository.Extensions
{
    public static class RepositoryExtensions
    {
        /// <summary>
        /// Create an instance of <see cref="IReplaceable{TEntity}"/>.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="repository"></param>
        /// <param name="autoIncludeFields"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static IReplaceable<TEntity> CreateReplaceableBuilder<TEntity>(this IBaseRepository repository, bool autoIncludeFields, string tableName = null)
        {
            return ReplaceableSqlBuilder<TEntity>.Create(repository.DbType, autoIncludeFields, tableName ?? repository.TableName() ?? typeof(TEntity).GetMainTableName());
        }
        /// <summary>
        /// Create an instance of <see cref="IReplaceable{TEntity}"/>.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="repository"></param>
        /// <param name="autoIncludeFields"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static IReplaceable<TEntity> CreateReplaceableBuilder<TEntity>(this IBaseRepository<TEntity> repository, bool autoIncludeFields, string tableName = null)
        {
            return ReplaceableSqlBuilder<TEntity>.Create(repository.DbType, autoIncludeFields, tableName ?? repository.TableName() ?? typeof(TEntity).GetMainTableName());
        }

        /// <summary>
        /// Create an instance of <see cref="IInsertable{TEntity}"/>.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="repository"></param>
        /// <param name="autoIncludeFields"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static IInsertable<TEntity> CreateInsertableBuilder<TEntity>(this IBaseRepository repository, bool autoIncludeFields, string tableName = null)
        {
            return InsertableSqlBuilder<TEntity>.Create(repository.DbType, autoIncludeFields, tableName ?? repository.TableName() ?? typeof(TEntity).GetMainTableName());
        }
        /// <summary>
        /// Create an instance of <see cref="IInsertable{TEntity}"/>.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="repository"></param>
        /// <param name="autoIncludeFields"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static IInsertable<TEntity> CreateInsertableBuilder<TEntity>(this IBaseRepository<TEntity> repository, bool autoIncludeFields, string tableName = null)
        {
            return InsertableSqlBuilder<TEntity>.Create(repository.DbType, autoIncludeFields, tableName ?? repository.TableName() ?? typeof(TEntity).GetMainTableName());
        }

        /// <summary>
        /// Create an instance of <see cref="IDeleteable{TEntity}"/>.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="repository"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static IDeleteable<TEntity> CreateDeleteableBuilder<TEntity>(this IBaseRepository repository, string tableName = null)
        {
            return DeleteableSqlBuilder<TEntity>.Create(repository.DbType, tableName ?? repository.TableName() ?? typeof(TEntity).GetMainTableName());
        }
        /// <summary>
        /// Create an instance of <see cref="IDeleteable{TEntity}"/>.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="repository"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static IDeleteable<TEntity> CreateDeleteableBuilder<TEntity>(this IBaseRepository<TEntity> repository, string tableName = null)
        {
            return DeleteableSqlBuilder<TEntity>.Create(repository.DbType, tableName ?? repository.TableName() ?? typeof(TEntity).GetMainTableName());
        }

        /// <summary>
        /// Create an instance of <see cref="IUpdateable{TEntity}"/>.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="repository"></param>
        /// <param name="autoIncludeFields"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static IUpdateable<TEntity> CreateUpdateableBuilder<TEntity>(this IBaseRepository repository, bool autoIncludeFields, string tableName = null)
        {
            return UpdateableSqlBuilder<TEntity>.Create(repository.DbType, autoIncludeFields, tableName ?? repository.TableName() ?? typeof(TEntity).GetMainTableName());
        }
        /// <summary>
        /// Create an instance of <see cref="IUpdateable{TEntity}"/>.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="repository"></param>
        /// <param name="autoIncludeFields"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static IUpdateable<TEntity> CreateUpdateableBuilder<TEntity>(this IBaseRepository<TEntity> repository, bool autoIncludeFields, string tableName = null)
        {
            return UpdateableSqlBuilder<TEntity>.Create(repository.DbType, autoIncludeFields, tableName ?? repository.TableName() ?? typeof(TEntity).GetMainTableName());
        }

        /// <summary>
        /// Create an instance of <see cref="IQueryable{TEntity}"/>.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="repository"></param>
        /// <param name="autoIncludeFields"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static IQueryable<TEntity> CreateQueryableBuilder<TEntity>(this IBaseRepository repository, bool autoIncludeFields, string tableName = null)
        {
            return QueryableSqlBuilder<TEntity>.Create(repository.DbType, autoIncludeFields, tableName ?? repository.TableName() ?? typeof(TEntity).GetMainTableName());
        }
        /// <summary>
        /// Create an instance of <see cref="IQueryable{TEntity}"/>.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="repository"></param>
        /// <param name="autoIncludeFields"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static IQueryable<TEntity> CreateQueryableBuilder<TEntity>(this IBaseRepository<TEntity> repository, bool autoIncludeFields, string tableName = null)
        {
            return QueryableSqlBuilder<TEntity>.Create(repository.DbType, autoIncludeFields, tableName ?? repository.TableName() ?? typeof(TEntity).GetMainTableName());
        }

        /// <summary>
        /// Create an instance of <see cref="ICountable{TEntity}"/>.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="repository"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static ICountable<TEntity> CreateCountableBuilder<TEntity>(this IBaseRepository repository, string tableName = null)
        {
            return CountableSqlBuilder<TEntity>.Create(repository.DbType, tableName ?? repository.TableName() ?? typeof(TEntity).GetMainTableName());
        }
        /// <summary>
        /// Create an instance of <see cref="ICountable{TEntity}"/>.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="repository"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static ICountable<TEntity> CreateCountableBuilder<TEntity>(this IBaseRepository<TEntity> repository, string tableName = null)
        {
            return CountableSqlBuilder<TEntity>.Create(repository.DbType, tableName ?? repository.TableName() ?? typeof(TEntity).GetMainTableName());
        }
    }
}
