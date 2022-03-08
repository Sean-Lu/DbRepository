using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
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

        #region Synchronous method
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
            return Execute(connection => connection.Add(this, entity, returnId, transaction, commandTimeout), true, transaction);
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
            return Execute(connection => connection.Add(this, entities, returnId, transaction, commandTimeout), true, transaction);
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
            return Execute(connection => connection.Delete(this, entity, transaction, commandTimeout), true, transaction);
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
            return Execute(connection => connection.Delete(this, sqlFactory, transaction, commandTimeout), true, transaction);
        }
        /// <summary>
        /// 删除全部数据
        /// </summary>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual int DeleteAll(IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return Execute(connection => connection.DeleteAll<TEntity>(this, transaction, commandTimeout), true, transaction);
        }

        /// <summary>
        /// 更新数据（实体所有字段都会更新）
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual bool Update(TEntity entity, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return Execute(connection => connection.Update(this, entity, transaction, commandTimeout), true, transaction);
        }
        /// <summary>
        /// 更新数据（更新指定的字段，实体必须有主键字段且有值）
        /// </summary>
        /// <param name="fieldExpression">指定需要更新的字段。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：new { entity.Status, entity.UpdateTime }</para>
        /// <para>多个字段（数组\IEnumerable）：new object[] { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="entity">实体</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual bool Update(Expression<Func<TEntity, object>> fieldExpression, TEntity entity, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return Execute(connection => connection.Update(this, fieldExpression, entity, transaction, commandTimeout), true, transaction);
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
            return Execute(connection => connection.Update(this, sqlFactory, transaction, commandTimeout), true, transaction);
        }
        /// <summary>
        /// 更新全部数据
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual int UpdateAll(SqlFactory sqlFactory, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return Execute(connection => connection.UpdateAll(this, sqlFactory, transaction, commandTimeout), true, transaction);
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
        /// 统计全部数量
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

        #region Asynchronous method
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
            return await ExecuteAsync(async connection => await connection.AddAsync(this, entity, returnId, transaction, commandTimeout), true, transaction);
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
            return await ExecuteAsync(async connection => await connection.AddAsync(this, entities, returnId, transaction, commandTimeout), true, transaction);
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
            return await ExecuteAsync(async connection => await connection.DeleteAsync(this, entity, transaction, commandTimeout), true, transaction);
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
            return await ExecuteAsync(async connection => await connection.DeleteAsync(this, sqlFactory, transaction, commandTimeout), true, transaction);
        }
        /// <summary>
        /// 删除全部数据
        /// </summary>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual async Task<int> DeleteAllAsync(IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.DeleteAllAsync<TEntity>(this, transaction, commandTimeout), true, transaction);
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
            return await ExecuteAsync(async connection => await connection.UpdateAsync(this, entity, transaction, commandTimeout), true, transaction);
        }
        /// <summary>
        /// 更新数据（更新指定的字段，实体必须有主键字段且有值）
        /// </summary>
        /// <param name="fieldExpression">指定需要更新的字段。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：new { entity.Status, entity.UpdateTime }</para>
        /// <para>多个字段（数组\IEnumerable）：new object[] { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="entity">实体</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual async Task<bool> UpdateAsync(Expression<Func<TEntity, object>> fieldExpression, TEntity entity, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.UpdateAsync(this, fieldExpression, entity, transaction, commandTimeout), true, transaction);
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
            return await ExecuteAsync(async connection => await connection.UpdateAsync(this, sqlFactory, transaction, commandTimeout), true, transaction);
        }
        /// <summary>
        /// 更新全部数据
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual async Task<int> UpdateAllAsync(SqlFactory sqlFactory, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.UpdateAllAsync(this, sqlFactory, transaction, commandTimeout), true, transaction);
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
            return await ExecuteAsync(async connection => await connection.GetAsync<TEntity>(this, sqlFactory, singleCheck, commandTimeout), master);
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
        /// 统计全部数量
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
