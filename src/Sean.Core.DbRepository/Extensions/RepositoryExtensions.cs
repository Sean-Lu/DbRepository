using System;
using System.Collections.Generic;
using System.Data;
#if NETFRAMEWORK
using System.Data.Odbc;
using System.Data.OleDb;
#endif
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Transactions;
using Sean.Core.DbRepository.Util;

namespace Sean.Core.DbRepository.Extensions
{
    public static class RepositoryExtensions
    {
        #region SqlBuilder
        /// <summary>
        /// Create an instance of <see cref="IReplaceable{TEntity}"/>.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="repository"></param>
        /// <param name="autoIncludeFields">Whether all table fields are automatically resolved from <typeparamref name="TEntity"/>.</param>
        /// <param name="tableName">The table name.</param>
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
        /// <param name="autoIncludeFields">Whether all table fields are automatically resolved from <typeparamref name="TEntity"/>.</param>
        /// <param name="tableName">The table name.</param>
        /// <returns></returns>
        public static IReplaceable<TEntity> CreateReplaceableBuilder<TEntity>(this IBaseRepository<TEntity> repository, bool autoIncludeFields, string tableName = null) where TEntity : class
        {
            return ReplaceableSqlBuilder<TEntity>.Create(repository.DbType, autoIncludeFields, tableName ?? repository.TableName() ?? typeof(TEntity).GetMainTableName());
        }

        /// <summary>
        /// Create an instance of <see cref="IInsertable{TEntity}"/>.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="repository"></param>
        /// <param name="autoIncludeFields">Whether all table fields are automatically resolved from <typeparamref name="TEntity"/>.</param>
        /// <param name="tableName">The table name.</param>
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
        /// <param name="autoIncludeFields">Whether all table fields are automatically resolved from <typeparamref name="TEntity"/>.</param>
        /// <param name="tableName">The table name.</param>
        /// <returns></returns>
        public static IInsertable<TEntity> CreateInsertableBuilder<TEntity>(this IBaseRepository<TEntity> repository, bool autoIncludeFields, string tableName = null) where TEntity : class
        {
            return InsertableSqlBuilder<TEntity>.Create(repository.DbType, autoIncludeFields, tableName ?? repository.TableName() ?? typeof(TEntity).GetMainTableName());
        }

        /// <summary>
        /// Create an instance of <see cref="IDeleteable{TEntity}"/>.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="repository"></param>
        /// <param name="tableName">The table name.</param>
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
        /// <param name="tableName">The table name.</param>
        /// <returns></returns>
        public static IDeleteable<TEntity> CreateDeleteableBuilder<TEntity>(this IBaseRepository<TEntity> repository, string tableName = null) where TEntity : class
        {
            return DeleteableSqlBuilder<TEntity>.Create(repository.DbType, tableName ?? repository.TableName() ?? typeof(TEntity).GetMainTableName());
        }

        /// <summary>
        /// Create an instance of <see cref="IUpdateable{TEntity}"/>.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="repository"></param>
        /// <param name="autoIncludeFields">Whether all table fields are automatically resolved from <typeparamref name="TEntity"/>.</param>
        /// <param name="tableName">The table name.</param>
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
        /// <param name="autoIncludeFields">Whether all table fields are automatically resolved from <typeparamref name="TEntity"/>.</param>
        /// <param name="tableName">The table name.</param>
        /// <returns></returns>
        public static IUpdateable<TEntity> CreateUpdateableBuilder<TEntity>(this IBaseRepository<TEntity> repository, bool autoIncludeFields, string tableName = null) where TEntity : class
        {
            return UpdateableSqlBuilder<TEntity>.Create(repository.DbType, autoIncludeFields, tableName ?? repository.TableName() ?? typeof(TEntity).GetMainTableName());
        }

        /// <summary>
        /// Create an instance of <see cref="IQueryable{TEntity}"/>.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="repository"></param>
        /// <param name="autoIncludeFields">Whether all table fields are automatically resolved from <typeparamref name="TEntity"/>.</param>
        /// <param name="tableName">The table name.</param>
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
        /// <param name="autoIncludeFields">Whether all table fields are automatically resolved from <typeparamref name="TEntity"/>.</param>
        /// <param name="tableName">The table name.</param>
        /// <returns></returns>
        public static IQueryable<TEntity> CreateQueryableBuilder<TEntity>(this IBaseRepository<TEntity> repository, bool autoIncludeFields, string tableName = null) where TEntity : class
        {
            return QueryableSqlBuilder<TEntity>.Create(repository.DbType, autoIncludeFields, tableName ?? repository.TableName() ?? typeof(TEntity).GetMainTableName());
        }

        /// <summary>
        /// Create an instance of <see cref="ICountable{TEntity}"/>.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="repository"></param>
        /// <param name="tableName">The table name.</param>
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
        /// <param name="tableName">The table name.</param>
        /// <returns></returns>
        public static ICountable<TEntity> CreateCountableBuilder<TEntity>(this IBaseRepository<TEntity> repository, string tableName = null) where TEntity : class
        {
            return CountableSqlBuilder<TEntity>.Create(repository.DbType, tableName ?? repository.TableName() ?? typeof(TEntity).GetMainTableName());
        }

        /// <summary>
        /// Create an instance of <see cref="SqlWhereClauseBuilder{TEntity}"/>.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="repository"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static SqlWhereClauseBuilder<TEntity> CreateSqlWhereClauseBuilder<TEntity>(this IBaseRepository repository, TEntity entity = default)
        {
            return SqlWhereClauseBuilder<TEntity>.Create(repository.DbType, entity);
        }
        /// <summary>
        /// Create an instance of <see cref="SqlWhereClauseBuilder{TEntity}"/>.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="repository"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static SqlWhereClauseBuilder<TEntity> CreateSqlWhereClauseBuilder<TEntity>(this IBaseRepository<TEntity> repository, TEntity entity = default) where TEntity : class
        {
            return SqlWhereClauseBuilder<TEntity>.Create(repository.DbType, entity);
        }
        #endregion

        #region SqlCommand
        public static ISqlCommand GetSqlForAdd<TEntity>(this IBaseRepository repository, TEntity entity, bool returnAutoIncrementId = false, Expression<Func<TEntity, object>> fieldExpression = null)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            return repository.CreateInsertableBuilder<TEntity>(fieldExpression == null)
                .IncludeFields(fieldExpression)
                .ReturnAutoIncrementId(returnAutoIncrementId)
                .SetParameter(entity)
                .Build();
        }
        public static ISqlCommand GetSqlForBulkAdd<TEntity>(this IBaseRepository repository, IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null)
        {
            return repository.CreateInsertableBuilder<TEntity>(fieldExpression == null)
                .IncludeFields(fieldExpression)
                .SetParameter(entities)
                .Build();
        }

        public static ISqlCommand GetSqlForAddOrUpdate<TEntity>(this IBaseRepository repository, TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            return repository.CreateReplaceableBuilder<TEntity>(fieldExpression == null)
                .IncludeFields(fieldExpression)
                .SetParameter(entity)
                .Build();
        }
        public static ISqlCommand GetSqlForBulkAddOrUpdate<TEntity>(this IBaseRepository repository, IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null)
        {
            return repository.CreateReplaceableBuilder<TEntity>(fieldExpression == null)
                .IncludeFields(fieldExpression)
                .SetParameter(entities)
                .Build();
        }

        public static ISqlCommand GetSqlForDelete<TEntity>(this IBaseRepository repository, TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            return repository.CreateDeleteableBuilder<TEntity>()
                .SetParameter(entity)
                .Build();
        }
        public static ISqlCommand GetSqlForDelete<TEntity>(this IBaseRepository repository, Expression<Func<TEntity, bool>> whereExpression)
        {
            return repository.CreateDeleteableBuilder<TEntity>()
                .Where(whereExpression)
                .Build();
        }

        public static ISqlCommand GetSqlForUpdate<TEntity>(this IBaseRepository repository, TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, Expression<Func<TEntity, bool>> whereExpression = null)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            return repository.CreateUpdateableBuilder<TEntity>(fieldExpression == null)
                .IncludeFields(fieldExpression, entity)
                .Where(whereExpression)
                .Build();
        }

        public static ISqlCommand GetSqlForIncr<TEntity, TValue>(this IBaseRepository repository, TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression) where TValue : struct
        {
            return repository.CreateUpdateableBuilder<TEntity>(false)
                .IncrementFields(fieldExpression, value)
                .Where(whereExpression)
                .Build();
        }
        public static ISqlCommand GetSqlForDecr<TEntity, TValue>(this IBaseRepository repository, TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression) where TValue : struct
        {
            return repository.CreateUpdateableBuilder<TEntity>(false)
                .DecrementFields(fieldExpression, value)
                .Where(whereExpression)
                .Build();
        }

        public static ISqlCommand GetSqlForQuery<TEntity>(this IBaseRepository repository, Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? pageIndex = null, int? pageSize = null, Expression<Func<TEntity, object>> fieldExpression = null)
        {
            return repository.CreateQueryableBuilder<TEntity>(fieldExpression == null)
                .IncludeFields(fieldExpression)
                .Where(whereExpression)
                .OrderBy(orderBy)
                .Page(pageIndex, pageSize)
                .Build();
        }
        public static ISqlCommand GetSqlForQueryOffset<TEntity>(this IBaseRepository repository, Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? offset = null, int? rows = null, Expression<Func<TEntity, object>> fieldExpression = null)
        {
            return repository.CreateQueryableBuilder<TEntity>(fieldExpression == null)
                .IncludeFields(fieldExpression)
                .Where(whereExpression)
                .OrderBy(orderBy)
                .Offset(offset, rows)
                .Build();
        }

        public static ISqlCommand GetSqlForGet<TEntity>(this IBaseRepository repository, Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, object>> fieldExpression = null)
        {
            return repository.CreateQueryableBuilder<TEntity>(fieldExpression == null)
                .IncludeFields(fieldExpression)
                .Where(whereExpression)
                .Build();
        }

        public static ISqlCommand GetSqlForCount<TEntity>(this IBaseRepository repository, Expression<Func<TEntity, bool>> whereExpression)
        {
            return repository.CreateCountableBuilder<TEntity>()
                .Where(whereExpression)
                .Build();
        }

        public static ISqlCommand GetSqlForEntityExists<TEntity>(this IBaseRepository repository, TEntity entity)
        {
            var pkFields = typeof(TEntity).GetPrimaryKeys();
            if (pkFields == null || !pkFields.Any()) throw new Exception($"The entity class '{typeof(TEntity).Name}' has no primary key field.");

            ICountable<TEntity> countableBuilder = repository.CreateCountableBuilder<TEntity>();
            pkFields.ForEach(pkField => countableBuilder.WhereField(entity1 => pkField, SqlOperation.Equal));
            countableBuilder.SetParameter(entity);
            ISqlCommand countSql = countableBuilder.Build();
            return countSql;
        }
        #endregion

        public static bool IsTableExists(this IBaseRepository repository, string tableName, Func<string, IDbConnection, bool> func, bool master = true, bool useCache = true)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return false;
            }

            string connectionString = repository.Factory.ConnectionSettings.GetConnectionString(master);
            if (useCache && TableInfoCache.IsTableExists(connectionString, master, tableName))
            {
                return true;
            }

            bool? exists = null;
#if NET45
            using (var tranScope = new TransactionScope(TransactionScopeOption.Suppress))
#else
            using (var tranScope = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
#endif
            using (var connection = repository.Factory.OpenNewConnection(connectionString))
            {
                exists = DbContextConfiguration.Options.IsTableExists?.Invoke(repository.DbType, connection, tableName);

                if (!exists.HasValue && repository.DbType == DatabaseType.MsAccess)
                {
#if NETFRAMEWORK
                    exists = connection switch
                    {
                        OleDbConnection oleDbConnection => oleDbConnection.IsTableExists(tableName),
                        OdbcConnection odbcConnection => odbcConnection.IsTableExists(tableName),
                        _ => null
                    };
#endif
                }

                if (!exists.HasValue)
                {
                    var sql = repository.DbType.GetSqlForTableExists(connection, tableName);
                    exists = func(sql, connection);
                }
            }

            if (useCache && exists.GetValueOrDefault())
            {
                TableInfoCache.AddTable(connectionString, master, tableName);
            }
            return exists.GetValueOrDefault();
        }

        public static bool IsTableFieldExists(this IBaseRepository repository, string tableName, string fieldName, Func<string, IDbConnection, bool> func, bool master = true, bool useCache = true)
        {
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            string connectionString = repository.Factory.ConnectionSettings.GetConnectionString(master);
            if (useCache && TableInfoCache.IsTableFieldExists(connectionString, master, tableName, fieldName))
            {
                return true;
            }

            bool? exists = null;
#if NET45
            using (var tranScope = new TransactionScope(TransactionScopeOption.Suppress))
#else
            using (var tranScope = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
#endif
            using (var connection = repository.Factory.OpenNewConnection(connectionString))
            {
                exists = DbContextConfiguration.Options.IsTableFieldExists?.Invoke(repository.DbType, connection, tableName, fieldName);

                if (!exists.HasValue && repository.DbType == DatabaseType.MsAccess)
                {
#if NETFRAMEWORK
                    exists = connection switch
                    {
                        OleDbConnection oleDbConnection => oleDbConnection.IsTableFieldExists(tableName, fieldName),
                        OdbcConnection odbcConnection => odbcConnection.IsTableFieldExists(tableName, fieldName),
                        _ => null
                    };
#endif
                }

                if (!exists.HasValue)
                {
                    var sql = repository.DbType.GetSqlForTableFieldExists(connection, tableName, fieldName);
                    exists = func(sql, connection);
                }
            }

            if (useCache && exists.GetValueOrDefault())
            {
                TableInfoCache.AddTableField(connectionString, master, tableName, fieldName);
            }
            return exists.GetValueOrDefault();
        }

        public static async Task<bool> IsTableExistsAsync(this IBaseRepository repository, string tableName, Func<string, IDbConnection, Task<bool>> func, bool master = true, bool useCache = true)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return false;
            }

            string connectionString = repository.Factory.ConnectionSettings.GetConnectionString(master);
            if (useCache && TableInfoCache.IsTableExists(connectionString, master, tableName))
            {
                return true;
            }

            bool? exists = null;
#if NET45
            using (var tranScope = new TransactionScope(TransactionScopeOption.Suppress))
#else
            using (var tranScope = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
#endif
            using (var connection = repository.Factory.OpenNewConnection(connectionString))
            {
                exists = DbContextConfiguration.Options.IsTableExists?.Invoke(repository.DbType, connection, tableName);

                if (!exists.HasValue && repository.DbType == DatabaseType.MsAccess)
                {
#if NETFRAMEWORK
                    exists = connection switch
                    {
                        OleDbConnection oleDbConnection => oleDbConnection.IsTableExists(tableName),
                        OdbcConnection odbcConnection => odbcConnection.IsTableExists(tableName),
                        _ => null
                    };
#endif
                }

                if (!exists.HasValue)
                {
                    var sql = repository.DbType.GetSqlForTableExists(connection, tableName);
                    exists = await func(sql, connection);
                }
            }

            if (useCache && exists.GetValueOrDefault())
            {
                TableInfoCache.AddTable(connectionString, master, tableName);
            }
            return exists.GetValueOrDefault();
        }

        public static async Task<bool> IsTableFieldExistsAsync(this IBaseRepository repository, string tableName, string fieldName, Func<string, IDbConnection, Task<bool>> func, bool master = true, bool useCache = true)
        {
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            string connectionString = repository.Factory.ConnectionSettings.GetConnectionString(master);
            if (useCache && TableInfoCache.IsTableFieldExists(connectionString, master, tableName, fieldName))
            {
                return true;
            }

            bool? exists = null;
#if NET45
            using (var tranScope = new TransactionScope(TransactionScopeOption.Suppress))
#else
            using (var tranScope = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
#endif
            using (var connection = repository.Factory.OpenNewConnection(connectionString))
            {
                exists = DbContextConfiguration.Options.IsTableFieldExists?.Invoke(repository.DbType, connection, tableName, fieldName);

                if (!exists.HasValue && repository.DbType == DatabaseType.MsAccess)
                {
#if NETFRAMEWORK
                    exists = connection switch
                    {
                        OleDbConnection oleDbConnection => oleDbConnection.IsTableFieldExists(tableName, fieldName),
                        OdbcConnection odbcConnection => odbcConnection.IsTableFieldExists(tableName, fieldName),
                        _ => null
                    };
#endif
                }

                if (!exists.HasValue)
                {
                    var sql = repository.DbType.GetSqlForTableFieldExists(connection, tableName, fieldName);
                    exists = await func(sql, connection);
                }
            }

            if (useCache && exists.GetValueOrDefault())
            {
                TableInfoCache.AddTableField(connectionString, master, tableName, fieldName);
            }
            return exists.GetValueOrDefault();
        }
    }
}
