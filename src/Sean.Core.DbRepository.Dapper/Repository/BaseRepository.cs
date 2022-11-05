using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Dapper;
using Sean.Core.DbRepository.Dapper.Extensions;
#if NETSTANDARD
using Microsoft.Extensions.Configuration;
#endif

namespace Sean.Core.DbRepository.Dapper
{
    /// <summary>
    /// Database table base repository
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public abstract class BaseRepository<TEntity> : EntityBaseRepository<TEntity> where TEntity : class
    {
        #region Constructors
#if NETSTANDARD
        /// <summary>
        /// Single or clustered database.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="configName">Configuration ConnectionStrings name</param>
        protected BaseRepository(IConfiguration configuration = null, string configName = Constants.Master) : base(configuration, configName)
        {
        }
#else
        /// <summary>
        /// Single or clustered database.
        /// </summary>
        /// <param name="configName">Configuration ConnectionStrings name</param>
        protected BaseRepository(string configName = Constants.Master) : base(configName)
        {
        }
#endif
        /// <summary>
        /// Single or clustered database.
        /// </summary>
        /// <param name="connectionSettings"></param>
        protected BaseRepository(MultiConnectionSettings connectionSettings) : base(connectionSettings)
        {
        }
        /// <summary>
        /// Single database.
        /// </summary>
        /// <param name="connString"></param>
        /// <param name="type"></param>
        protected BaseRepository(string connString, DatabaseType type) : this(new MultiConnectionSettings(new ConnectionStringOptions(connString, type)))
        {

        }
        /// <summary>
        /// Single database.
        /// </summary>
        /// <param name="connString"></param>
        /// <param name="factory"></param>
        protected BaseRepository(string connString, DbProviderFactory factory) : this(new MultiConnectionSettings(new ConnectionStringOptions(connString, factory)))
        {

        }
        /// <summary>
        /// Single database.
        /// </summary>
        /// <param name="connString"></param>
        /// <param name="providerName"></param>
        protected BaseRepository(string connString, string providerName) : this(new MultiConnectionSettings(new ConnectionStringOptions(connString, providerName)))
        {

        }
        #endregion

        #region Synchronous method
        public override int Execute(string sql, object param = null, IDbTransaction transaction = null, bool master = true)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

            return new DefaultSqlWithParameter
            {
                Sql = sql,
                Parameter = param
            }.Execute(this, transaction, master);
        }
        public override IEnumerable<T> Query<T>(string sql, object param = null, IDbTransaction transaction = null, bool master = true)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

            return new DefaultSqlWithParameter
            {
                Sql = sql,
                Parameter = param
            }.Query<T>(this, transaction, master);
        }
        public override T QueryFirstOrDefault<T>(string sql, object param = null, IDbTransaction transaction = null, bool master = true)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

            return new DefaultSqlWithParameter
            {
                Sql = sql,
                Parameter = param
            }.QueryFirstOrDefault<T>(this, transaction, master);
        }
        public override T ExecuteScalar<T>(string sql, object param = null, IDbTransaction transaction = null, bool master = true)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

            return new DefaultSqlWithParameter
            {
                Sql = sql,
                Parameter = param
            }.ExecuteScalar<T>(this, transaction, master);
        }

        public override bool Add(TEntity entity, bool returnAutoIncrementId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
        {
            return Execute(connection => connection.Add(this, entity, returnAutoIncrementId, fieldExpression, transaction), true, transaction);
        }
        public override bool Add(IEnumerable<TEntity> entities, bool returnAutoIncrementId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
        {
            return Execute(connection => connection.BulkAdd(this, entities, returnAutoIncrementId, fieldExpression, transaction), true, transaction);
        }

        public override bool AddOrUpdate(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
        {
            return Execute(connection => connection.AddOrUpdate(this, entity, fieldExpression, transaction), true, transaction);
        }
        public override bool AddOrUpdate(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
        {
            return Execute(connection => connection.BulkAddOrUpdate(this, entities, fieldExpression, transaction), true, transaction);
        }

        public override bool Delete(TEntity entity, IDbTransaction transaction = null)
        {
            return Execute(connection => connection.Delete(this, entity, transaction), true, transaction);
        }
        public override int Delete(Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null)
        {
            return Execute(connection => connection.Delete(this, whereExpression, transaction), true, transaction);
        }

        public override int Update(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, Expression<Func<TEntity, bool>> whereExpression = null, IDbTransaction transaction = null)
        {
            return Execute(connection => connection.Update(this, entity, fieldExpression, whereExpression, transaction), true, transaction);
        }
        public override bool Update(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
        {
            return Execute(connection => connection.BulkUpdate(this, entities, fieldExpression, transaction), true, transaction);
        }

        public override bool Incr<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null) where TValue : struct
        {
            return Execute(connection => connection.Incr(this, value, fieldExpression, whereExpression, transaction), true, transaction);
        }
        public override bool Decr<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null) where TValue : struct
        {
            return Execute(connection => connection.Decr(this, value, fieldExpression, whereExpression, transaction), true, transaction);
        }

        public override IEnumerable<TEntity> Query(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? pageIndex = null, int? pageSize = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true)
        {
            return Execute(connection => connection.Query<TEntity>(this, whereExpression, orderBy, pageIndex, pageSize, fieldExpression), master);
        }
        public override IEnumerable<TEntity> QueryOffset(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? offset = null, int? rows = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true)
        {
            return Execute(connection => connection.QueryOffset<TEntity>(this, whereExpression, orderBy, offset, rows, fieldExpression), master);
        }

        public override TEntity Get(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true)
        {
            return Execute(connection => connection.Get<TEntity>(this, whereExpression, fieldExpression), master);
        }

        public override int Count(Expression<Func<TEntity, bool>> whereExpression, bool master = true)
        {
            return Execute(connection => connection.Count(this, whereExpression), master);
        }

        public override bool IsTableExists(string tableName, bool master = true, bool useCache = true)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return false;
            }

            if (useCache && TableInfoCache.Exists(tableName))
            {
                return true;
            }

            return Execute(connection => connection.IsTableExists(this, tableName, useCache), master);
        }

        public override bool IsTableFieldExists(string tableName, string fieldName, bool master = true, bool useCache = true)
        {
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            if (useCache && TableInfoCache.Exists(tableName, fieldName))
            {
                return true;
            }

            return Execute(connection => connection.IsTableFieldExists(this, tableName, fieldName, useCache), master);
        }
        #endregion

        #region Asynchronous method
#if NETSTANDARD || NET45_OR_GREATER
        public override async Task<int> ExecuteAsync(string sql, object param = null, IDbTransaction transaction = null, bool master = true)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

            return await new DefaultSqlWithParameter
            {
                Sql = sql,
                Parameter = param
            }.ExecuteAsync(this, transaction, master);
        }
        public override async Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null, IDbTransaction transaction = null, bool master = true)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

            return await new DefaultSqlWithParameter
            {
                Sql = sql,
                Parameter = param
            }.QueryAsync<T>(this, transaction, master);
        }
        public override async Task<T> QueryFirstOrDefaultAsync<T>(string sql, object param = null, IDbTransaction transaction = null, bool master = true)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

            return await new DefaultSqlWithParameter
            {
                Sql = sql,
                Parameter = param
            }.QueryFirstOrDefaultAsync<T>(this, transaction, master);
        }
        public override async Task<T> ExecuteScalarAsync<T>(string sql, object param = null, IDbTransaction transaction = null, bool master = true)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

            return await new DefaultSqlWithParameter
            {
                Sql = sql,
                Parameter = param
            }.ExecuteScalarAsync<T>(this, transaction, master);
        }

        public override async Task<bool> AddAsync(TEntity entity, bool returnId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
        {
            return await ExecuteAsync(async connection => await connection.AddAsync(this, entity, returnId, fieldExpression, transaction), true, transaction);
        }
        public override async Task<bool> AddAsync(IEnumerable<TEntity> entities, bool returnId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
        {
            return await ExecuteAsync(async connection => await connection.BulkAddAsync(this, entities, returnId, fieldExpression, transaction), true, transaction);
        }

        public override async Task<bool> AddOrUpdateAsync(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
        {
            return await ExecuteAsync(async connection => await connection.AddOrUpdateAsync(this, entity, fieldExpression, transaction), true, transaction);
        }
        public override async Task<bool> AddOrUpdateAsync(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
        {
            return await ExecuteAsync(async connection => await connection.BulkAddOrUpdateAsync(this, entities, fieldExpression, transaction), true, transaction);
        }

        public override async Task<bool> DeleteAsync(TEntity entity, IDbTransaction transaction = null)
        {
            return await ExecuteAsync(async connection => await connection.DeleteAsync(this, entity, transaction), true, transaction);
        }
        public override async Task<int> DeleteAsync(Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null)
        {
            return await ExecuteAsync(async connection => await connection.DeleteAsync(this, whereExpression, transaction), true, transaction);
        }

        public override async Task<int> UpdateAsync(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, Expression<Func<TEntity, bool>> whereExpression = null, IDbTransaction transaction = null)
        {
            return await ExecuteAsync(async connection => await connection.UpdateAsync(this, entity, fieldExpression, whereExpression, transaction), true, transaction);
        }
        public override async Task<bool> UpdateAsync(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
        {
            return await ExecuteAsync(async connection => await connection.BulkUpdateAsync(this, entities, fieldExpression, transaction), true, transaction);
        }

        public override async Task<bool> IncrAsync<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null) where TValue : struct
        {
            return await ExecuteAsync(async connection => await connection.IncrAsync(this, value, fieldExpression, whereExpression, transaction), true, transaction);
        }
        public override async Task<bool> DecrAsync<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null) where TValue : struct
        {
            return await ExecuteAsync(async connection => await connection.DecrAsync(this, value, fieldExpression, whereExpression, transaction), true, transaction);
        }

        public override async Task<IEnumerable<TEntity>> QueryAsync(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? pageIndex = null, int? pageSize = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true)
        {
            return await ExecuteAsync(async connection => await connection.QueryAsync<TEntity>(this, whereExpression, orderBy, pageIndex, pageSize, fieldExpression), master);
        }
        public override async Task<IEnumerable<TEntity>> QueryOffsetAsync(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? offset = null, int? rows = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true)
        {
            return await ExecuteAsync(async connection => await connection.QueryOffsetAsync<TEntity>(this, whereExpression, orderBy, offset, rows, fieldExpression), master);
        }

        public override async Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true)
        {
            return await ExecuteAsync(async connection => await connection.GetAsync<TEntity>(this, whereExpression, fieldExpression), master);
        }

        public override async Task<int> CountAsync(Expression<Func<TEntity, bool>> whereExpression, bool master = true)
        {
            return await ExecuteAsync(async connection => await connection.CountAsync(this, whereExpression), master);
        }

        public override async Task<bool> IsTableExistsAsync(string tableName, bool master = true, bool useCache = true)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return false;
            }

            if (useCache && TableInfoCache.Exists(tableName))
            {
                return true;
            }

            return await ExecuteAsync(async connection => await connection.IsTableExistsAsync(this, tableName, useCache), master);
        }

        public override async Task<bool> IsTableFieldExistsAsync(string tableName, string fieldName, bool master = true, bool useCache = true)
        {
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            if (useCache && TableInfoCache.Exists(tableName, fieldName))
            {
                return true;
            }

            return await ExecuteAsync(async connection => await connection.IsTableFieldExistsAsync(this, tableName, fieldName, useCache), master);
        }
#endif
        #endregion
    }
}
