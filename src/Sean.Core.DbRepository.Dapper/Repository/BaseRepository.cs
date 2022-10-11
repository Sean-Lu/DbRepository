using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Dapper;
using Sean.Core.DbRepository.Dapper.Extensions;
using Sean.Core.DbRepository.Extensions;
#if NETSTANDARD
using Microsoft.Extensions.Configuration;
#endif

namespace Sean.Core.DbRepository.Dapper
{
    /// <summary>
    /// Database table base repository
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public abstract class BaseRepository<TEntity> : BaseRepository, IBaseRepository<TEntity> where TEntity : class
    {
        public virtual string MainTableName => typeof(TEntity).GetMainTableName();

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

        /// <summary>
        /// 表名：默认返回主表表名 <see cref="MainTableName"/>
        /// </summary>
        /// <returns></returns>
        public override string TableName()
        {
            return MainTableName;
        }

        public virtual void CreateTableIfNotExist(string tableName, bool master = true)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(tableName));

            if (IsTableExists(tableName, master))
            {
                return;
            }

            var sql = CreateTableSql(tableName);
            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(CreateTableSql));
            }

            Execute(connection => connection.Execute(sql), master);
        }

        #region Synchronous method
        public virtual bool Add(TEntity entity, bool returnId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return Execute(connection => connection.Add(this, entity, returnId, fieldExpression, transaction, commandTimeout), true, transaction);
        }
        public virtual bool Add(IEnumerable<TEntity> entities, bool returnId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return Execute(connection => connection.BulkAdd(this, entities, returnId, fieldExpression, transaction, commandTimeout), true, transaction);
        }
        public virtual bool Add(IInsertableSql insertableSql, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return Execute(connection => connection.Add(insertableSql, transaction, this, commandTimeout), true, transaction);
        }

        public virtual bool AddOrUpdate(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return Execute(connection => connection.AddOrUpdate(this, entity, fieldExpression, transaction, commandTimeout), true, transaction);
        }
        public virtual bool AddOrUpdate(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return Execute(connection => connection.BulkAddOrUpdate(this, entities, fieldExpression, transaction, commandTimeout), true, transaction);
        }
        public virtual bool AddOrUpdate(IReplaceableSql replaceableSql, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return Execute(connection => connection.AddOrUpdate(replaceableSql, transaction, this, commandTimeout), true, transaction);
        }

        public virtual bool Delete(TEntity entity, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return Execute(connection => connection.Delete(this, entity, transaction, commandTimeout), true, transaction);
        }
        public virtual int Delete(Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return Execute(connection => connection.Delete(this, whereExpression, transaction, commandTimeout), true, transaction);
        }
        public virtual int Delete(IDeleteableSql deleteableSql, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return Execute(connection => connection.Delete(deleteableSql, transaction, this, commandTimeout), true, transaction);
        }

        public virtual int Update(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, Expression<Func<TEntity, bool>> whereExpression = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return Execute(connection => connection.Update(this, entity, fieldExpression, whereExpression, transaction, commandTimeout), true, transaction);
        }
        public virtual bool Update(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, Expression<Func<TEntity, bool>> whereExpression = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return Execute(connection => connection.BulkUpdate(this, entities, fieldExpression, whereExpression, transaction, commandTimeout), true, transaction);
        }
        public virtual int Update(IUpdateableSql updateableSql, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return Execute(connection => connection.Update(updateableSql, transaction, this, commandTimeout), true, transaction);
        }

        public virtual bool Incr<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null, int? commandTimeout = null) where TValue : struct
        {
            return Execute(connection => connection.Incr(this, value, fieldExpression, whereExpression, transaction, commandTimeout), true, transaction);
        }
        public virtual bool Decr<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null, int? commandTimeout = null) where TValue : struct
        {
            return Execute(connection => connection.Decr(this, value, fieldExpression, whereExpression, transaction, commandTimeout), true, transaction);
        }

        public virtual IEnumerable<TEntity> Query(IQueryableSql queryableSql, bool master = true, int? commandTimeout = null)
        {
            return Execute(connection => connection.Query<TEntity>(queryableSql, this, commandTimeout), master);
        }
        public virtual IEnumerable<TEntity> Query(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? pageIndex = null, int? pageSize = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true, int? commandTimeout = null)
        {
            return Execute(connection => connection.Query<TEntity>(this, whereExpression, orderBy, pageIndex, pageSize, fieldExpression, commandTimeout), master);
        }
        public virtual IEnumerable<TEntity> QueryOffset(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? offset = null, int? rows = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true, int? commandTimeout = null)
        {
            return Execute(connection => connection.QueryOffset<TEntity>(this, whereExpression, orderBy, offset, rows, fieldExpression, commandTimeout), master);
        }

        public virtual TEntity Get(IQueryableSql queryableSql, bool singleCheck = false, bool master = true, int? commandTimeout = null)
        {
            return Execute(connection => connection.Get<TEntity>(queryableSql, singleCheck, this, commandTimeout), master);
        }
        public virtual TEntity Get(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, object>> fieldExpression = null, bool singleCheck = false, bool master = true, int? commandTimeout = null)
        {
            return Execute(connection => connection.Get<TEntity>(this, whereExpression, fieldExpression, singleCheck, commandTimeout), master);
        }

        public virtual int Count(ICountableSql countableSql, bool master = true, int? commandTimeout = null)
        {
            return Execute(connection => connection.Count(countableSql, this, commandTimeout), master);
        }
        public virtual int Count(Expression<Func<TEntity, bool>> whereExpression, bool master = true, int? commandTimeout = null)
        {
            return Execute(connection => connection.Count(this, whereExpression, commandTimeout), master);
        }

        public virtual bool IsTableExists(string tableName, bool master = true)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return false;
            }

            if (TableInfoCache.IsTableExists(tableName))
            {
                return true;
            }

            var tableExists = Execute(connection => connection.IsTableExists(this, tableName), master);
            if (tableExists)
            {
                TableInfoCache.IsTableExists(tableName, true);
            }
            return tableExists;
        }

        public virtual bool IsTableFieldExists(string tableName, string fieldName, bool master = true)
        {
            return Execute(connection => connection.IsTableFieldExists(this, tableName, fieldName), master);
        }
        public virtual bool IsTableFieldExists(Expression<Func<TEntity, object>> fieldExpression, bool master = true)
        {
            var tableName = TableName();
            var fields = fieldExpression.GetFieldNames();
            if (fields == null || fields.Count < 1)
            {
                return false;
            }
            foreach (var fieldName in fields)
            {
                if (!Execute(connection => connection.IsTableFieldExists(this, tableName, fieldName), master))
                {
                    return false;
                }
            }
            return true;
        }
        #endregion

        #region Asynchronous method
#if NETSTANDARD || NET45_OR_GREATER
        public virtual async Task<bool> AddAsync(TEntity entity, bool returnId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.AddAsync(this, entity, returnId, fieldExpression, transaction, commandTimeout), true, transaction);
        }
        public virtual async Task<bool> AddAsync(IEnumerable<TEntity> entities, bool returnId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.BulkAddAsync(this, entities, returnId, fieldExpression, transaction, commandTimeout), true, transaction);
        }
        public virtual async Task<bool> AddAsync(IInsertableSql insertableSql, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.AddAsync(insertableSql, transaction, this, commandTimeout), true, transaction);
        }

        public virtual async Task<bool> AddOrUpdateAsync(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.AddOrUpdateAsync(this, entity, fieldExpression, transaction, commandTimeout), true, transaction);
        }
        public virtual async Task<bool> AddOrUpdateAsync(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.BulkAddOrUpdateAsync(this, entities, fieldExpression, transaction, commandTimeout), true, transaction);
        }
        public virtual async Task<bool> AddOrUpdateAsync(IReplaceableSql replaceableSql, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.AddOrUpdateAsync(replaceableSql, transaction, this, commandTimeout), true, transaction);
        }

        public virtual async Task<bool> DeleteAsync(TEntity entity, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.DeleteAsync(this, entity, transaction, commandTimeout), true, transaction);
        }
        public virtual async Task<int> DeleteAsync(Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.DeleteAsync(this, whereExpression, transaction, commandTimeout), true, transaction);
        }
        public virtual async Task<int> DeleteAsync(IDeleteableSql deleteableSql, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.DeleteAsync(deleteableSql, transaction, this, commandTimeout), true, transaction);
        }

        public virtual async Task<int> UpdateAsync(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, Expression<Func<TEntity, bool>> whereExpression = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.UpdateAsync(this, entity, fieldExpression, whereExpression, transaction, commandTimeout), true, transaction);
        }
        public virtual async Task<bool> UpdateAsync(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, Expression<Func<TEntity, bool>> whereExpression = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.BulkUpdateAsync(this, entities, fieldExpression, whereExpression, transaction, commandTimeout), true, transaction);
        }
        public virtual async Task<int> UpdateAsync(IUpdateableSql updateableSql, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.UpdateAsync(updateableSql, transaction, this, commandTimeout), true, transaction);
        }

        public virtual async Task<bool> IncrAsync<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null, int? commandTimeout = null) where TValue : struct
        {
            return await ExecuteAsync(async connection => await connection.IncrAsync(this, value, fieldExpression, whereExpression, transaction, commandTimeout), true, transaction);
        }
        public virtual async Task<bool> DecrAsync<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null, int? commandTimeout = null) where TValue : struct
        {
            return await ExecuteAsync(async connection => await connection.DecrAsync(this, value, fieldExpression, whereExpression, transaction, commandTimeout), true, transaction);
        }

        public virtual async Task<IEnumerable<TEntity>> QueryAsync(IQueryableSql queryableSql, bool master = true, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.QueryAsync<TEntity>(queryableSql, this, commandTimeout), master);
        }
        public virtual async Task<IEnumerable<TEntity>> QueryAsync(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? pageIndex = null, int? pageSize = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.QueryAsync<TEntity>(this, whereExpression, orderBy, pageIndex, pageSize, fieldExpression, commandTimeout), master);
        }
        public virtual async Task<IEnumerable<TEntity>> QueryOffsetAsync(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? offset = null, int? rows = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.QueryOffsetAsync<TEntity>(this, whereExpression, orderBy, offset, rows, fieldExpression, commandTimeout), master);
        }

        public virtual async Task<TEntity> GetAsync(IQueryableSql queryableSql, bool singleCheck = false, bool master = true, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.GetAsync<TEntity>(queryableSql, singleCheck, this, commandTimeout), master);
        }
        public virtual async Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, object>> fieldExpression = null, bool singleCheck = false, bool master = true, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.GetAsync<TEntity>(this, whereExpression, fieldExpression, singleCheck, commandTimeout), master);
        }

        public virtual async Task<int> CountAsync(ICountableSql countableSql, bool master = true, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.CountAsync(countableSql, this, commandTimeout), master);
        }
        public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>> whereExpression, bool master = true, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.CountAsync(this, whereExpression, commandTimeout), master);
        }

        public virtual async Task<bool> IsTableExistsAsync(string tableName, bool master = true)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return false;
            }

            if (TableInfoCache.IsTableExists(tableName))
            {
                return true;
            }

            var tableExists = await ExecuteAsync(async connection => await connection.IsTableExistsAsync(this, tableName), master);
            if (tableExists)
            {
                TableInfoCache.IsTableExists(tableName, true);
            }
            return tableExists;
        }

        public virtual async Task<bool> IsTableFieldExistsAsync(string tableName, string fieldName, bool master = true)
        {
            return await ExecuteAsync(async connection => await connection.IsTableFieldExistsAsync(this, tableName, fieldName), master);
        }
        public virtual async Task<bool> IsTableFieldExistsAsync(Expression<Func<TEntity, object>> fieldExpression, bool master = true)
        {
            var tableName = TableName();
            var fields = fieldExpression.GetFieldNames();
            if (fields == null || fields.Count < 1)
            {
                return false;
            }
            foreach (var fieldName in fields)
            {
                if (!await ExecuteAsync(async connection => await connection.IsTableFieldExistsAsync(this, tableName, fieldName), master))
                {
                    return false;
                }
            }
            return true;
        }
#endif
        #endregion
    }
}
