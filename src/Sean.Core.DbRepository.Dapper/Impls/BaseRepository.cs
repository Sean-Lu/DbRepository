using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Sean.Core.DbRepository.Cache;
using Sean.Core.DbRepository.Contracts;
using Sean.Core.DbRepository.Dapper.Extensions;
using Sean.Core.DbRepository.Extensions;
using Sean.Core.DbRepository.Factory;
using Sean.Core.DbRepository.Impls;
#if NETSTANDARD
using Microsoft.Extensions.Configuration;
#endif

namespace Sean.Core.DbRepository.Dapper.Impls
{
    /// <summary>
    /// Database table base repository
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public abstract class BaseRepository<TEntity> : BaseRepository, IBaseRepository<TEntity>
    {
        /// <summary>
        /// 主表表名
        /// </summary>
        public virtual string MainTableName => typeof(TEntity).GetMainTableName();

        #region Constructors
#if NETSTANDARD
        protected BaseRepository(IConfiguration configuration = null, string configName = Constants.Master) : base(configuration, configName)
        {
        }
#else
        protected BaseRepository(string configName = Constants.Master) : base(configName)
        {
        }
#endif
        protected BaseRepository(MultiConnectionSettings connectionSettings) : base(connectionSettings)
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

        /// <summary>
        /// 如果表不存在，则执行SQL语句（<see cref="BaseRepository.CreateTableSql"/>）来创建新表
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="master">true: 主库, false: 从库</param>
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
                throw new Exception($"Value cannot be null or whitespace: {nameof(CreateTableSql)}.");
            }

            Execute(connection => connection.Execute(sql), master);
        }

        /// <summary>
        /// <see cref="SqlFactory{TEntity}.Build(IBaseRepository, bool)"/>
        /// </summary>
        /// <param name="autoIncludeFields"></param>
        /// <returns></returns>
        public virtual SqlFactory<TEntity> NewSqlFactory(bool autoIncludeFields)
        {
            return SqlFactory<TEntity>.Build(this, autoIncludeFields);
        }

        #region 同步方法
        /// <summary>
        /// 新增数据
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="returnId">是否返回自增主键Id</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual bool Add(TEntity entity, bool returnId = false, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return transaction?.Connection == null
                ? Execute(connection => connection.Add(this, entity, returnId, transaction, commandTimeout), true)
                : transaction.Connection.Add(this, entity, returnId, transaction, commandTimeout);
        }
        /// <summary>
        /// 批量新增数据
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="returnId">是否返回自增主键Id</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual bool Add(IList<TEntity> entities, bool returnId = false, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return transaction?.Connection == null
                ? Execute(connection => connection.Add(this, entities, returnId, transaction, commandTimeout), true)
                : transaction.Connection.Add(this, entities, returnId, transaction, commandTimeout);
        }

        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual bool Delete(TEntity entity, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return transaction?.Connection == null
                ? Execute(connection => connection.Delete(this, entity, transaction, commandTimeout), true)
                : transaction.Connection.Delete(this, entity, transaction, commandTimeout);
        }
        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual int Delete(SqlFactory sqlFactory, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return transaction?.Connection == null
                ? Execute(connection => connection.Delete(this, sqlFactory, transaction, commandTimeout), true)
                : transaction.Connection.Delete(this, sqlFactory, transaction, commandTimeout);
        }
        /// <summary>
        /// 删除所有数据
        /// </summary>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual int DeleteAll(IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return transaction?.Connection == null
                ? Execute(connection => connection.DeleteAll<TEntity>(this, transaction, commandTimeout), true)
                : transaction.Connection.DeleteAll<TEntity>(this, transaction, commandTimeout);
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual bool Update(TEntity entity, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return transaction?.Connection == null
                ? Execute(connection => connection.Update(this, entity, transaction, commandTimeout), true)
                : transaction.Connection.Update(this, entity, transaction, commandTimeout);
        }
        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual int Update(SqlFactory sqlFactory, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return transaction?.Connection == null
                ? Execute(connection => connection.Update(this, sqlFactory, transaction, commandTimeout), true)
                : transaction.Connection.Update(this, sqlFactory, transaction, commandTimeout);
        }

        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual IEnumerable<TEntity> Query(SqlFactory sqlFactory, bool master = true, int? commandTimeout = null)
        {
            return Execute(connection => connection.Query<TEntity>(this, sqlFactory, commandTimeout), master);
        }

        /// <summary>
        /// 查询单个数据
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="singleCheck">是否执行单一结果检查。true：如果查询到多个结果会抛出异常，false：默认取第一个结果或默认值</param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual TEntity Get(SqlFactory sqlFactory, bool singleCheck = false, bool master = true, int? commandTimeout = null)
        {
            return Execute(connection => connection.Get<TEntity>(this, sqlFactory, singleCheck, commandTimeout), master);
        }

        /// <summary>
        /// 统计数量
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual int Count(SqlFactory sqlFactory, bool master = true, int? commandTimeout = null)
        {
            return Execute(connection => connection.Count(this, sqlFactory, commandTimeout), master);
        }
        /// <summary>
        /// 统计所有数量
        /// </summary>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual int CountAll(bool master = true, int? commandTimeout = null)
        {
            return Execute(connection => connection.CountAll<TEntity>(this, commandTimeout), master);
        }

        /// <summary>
        /// 查询指定的表是否存在
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <returns></returns>
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

            return Execute(connection => connection.IsTableExists(this, tableName), master);
        }
        #endregion

        #region 异步方法
#if NETSTANDARD || NET45_OR_GREATER
        /// <summary>
        /// 新增数据
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="returnId">是否返回自增主键Id</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual async Task<bool> AddAsync(TEntity entity, bool returnId = false, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return transaction?.Connection == null
                ? await ExecuteAsync(async connection => await connection.AddAsync(this, entity, returnId, transaction, commandTimeout), true)
                : await transaction.Connection.AddAsync(this, entity, returnId, transaction, commandTimeout);
        }
        /// <summary>
        /// 批量新增数据
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="returnId">是否返回自增主键Id</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual async Task<bool> AddAsync(IList<TEntity> entities, bool returnId = false, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return transaction?.Connection == null
                ? await ExecuteAsync(async connection => await connection.AddAsync(this, entities, returnId, transaction, commandTimeout), true)
                : await transaction.Connection.AddAsync(this, entities, returnId, transaction, commandTimeout);
        }

        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual async Task<bool> DeleteAsync(TEntity entity, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return transaction?.Connection == null
                ? await ExecuteAsync(async connection => await connection.DeleteAsync(this, entity, transaction, commandTimeout), true)
                : await transaction.Connection.DeleteAsync(this, entity, transaction, commandTimeout);
        }
        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual async Task<int> DeleteAsync(SqlFactory sqlFactory, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return transaction?.Connection == null
                ? await ExecuteAsync(async connection => await connection.DeleteAsync(this, sqlFactory, transaction, commandTimeout), true)
                : await transaction.Connection.DeleteAsync(this, sqlFactory, transaction, commandTimeout);
        }
        /// <summary>
        /// 删除所有数据
        /// </summary>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual async Task<int> DeleteAllAsync(IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return transaction?.Connection == null
                ? await ExecuteAsync(async connection => await connection.DeleteAllAsync<TEntity>(this, transaction, commandTimeout), true)
                : await transaction.Connection.DeleteAllAsync<TEntity>(this, transaction, commandTimeout);
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual async Task<bool> UpdateAsync(TEntity entity, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return transaction?.Connection == null
                ? await ExecuteAsync(async connection => await connection.UpdateAsync(this, entity, transaction, commandTimeout), true)
                : await transaction.Connection.UpdateAsync(this, entity, transaction, commandTimeout);
        }
        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual async Task<int> UpdateAsync(SqlFactory sqlFactory, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return transaction?.Connection == null
                ? await ExecuteAsync(async connection => await connection.UpdateAsync(this, sqlFactory, transaction, commandTimeout), true)
                : await transaction.Connection.UpdateAsync(this, sqlFactory, transaction, commandTimeout);
        }

        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual async Task<IEnumerable<TEntity>> QueryAsync(SqlFactory sqlFactory, bool master = true, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.QueryAsync<TEntity>(this, sqlFactory, commandTimeout), master);
        }

        /// <summary>
        /// 查询单个数据
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="singleCheck">是否执行单一结果检查。true：如果查询到多个结果会抛出异常，false：默认取第一个结果或默认值</param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual async Task<TEntity> GetAsync(SqlFactory sqlFactory, bool singleCheck = false, bool master = true, int? commandTimeout = null)
        {
            return await Execute(async connection => await connection.GetAsync<TEntity>(this, sqlFactory, singleCheck, commandTimeout), master);
        }

        /// <summary>
        /// 统计数量
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual async Task<int> CountAsync(SqlFactory sqlFactory, bool master = true, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.CountAsync(this, sqlFactory, commandTimeout), master);
        }
        /// <summary>
        /// 统计所有数量
        /// </summary>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual async Task<int> CountAllAsync(bool master = true, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.CountAllAsync<TEntity>(this, commandTimeout), master);
        }

        /// <summary>
        /// 查询指定的表是否存在
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <returns></returns>
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

            return await ExecuteAsync(async connection => await connection.IsTableExistsAsync(this, tableName), master);
        }
#endif
        #endregion
    }
}
