using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
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
        /// 主表
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
        #endregion

        #region 同步方法
        /// <summary>
        /// 默认返回主表表名
        /// </summary>
        /// <returns></returns>
        public override string TableName()
        {
            return MainTableName;
        }

        /// <summary>
        /// <see cref="SqlFactory{TEntity}.Build(IBaseRepository, bool)"/>
        /// </summary>
        /// <param name="autoIncludeFields"></param>
        /// <returns></returns>
        public virtual SqlFactory<TEntity> NewSqlFactory(bool autoIncludeFields = true)
        {
            return SqlFactory<TEntity>.Build(this, autoIncludeFields);
        }

        /// <summary>
        /// 新增
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
        /// 新增
        /// </summary>
        /// <param name="entitys"></param>
        /// <param name="returnId">是否返回自增主键Id</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual bool Add(IList<TEntity> entitys, bool returnId = false, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return transaction?.Connection == null
                ? Execute(connection => connection.Add(this, entitys, returnId, transaction, commandTimeout), true)
                : transaction.Connection.Add(this, entitys, returnId, transaction, commandTimeout);
        }

        /// <summary>
        /// 删除
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
        /// 更新
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
        /// 查询
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
        /// 分页查询
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual IEnumerable<TEntity> QueryPage(SqlFactory sqlFactory, bool master = true, int? commandTimeout = null)
        {
            return Execute(connection => connection.QueryPage<TEntity>(this, sqlFactory, commandTimeout), master);
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
        /// 表是否存在
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <returns></returns>
        public virtual bool IsTableExists(string tableName, bool master = true)
        {
            return Execute(connection => connection.IsTableExists(this, tableName), master);
        }
        #endregion

        #region 异步方法
        /// <summary>
        /// 新增
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
        /// 新增
        /// </summary>
        /// <param name="entitys"></param>
        /// <param name="returnId">是否返回自增主键Id</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual async Task<bool> AddAsync(IList<TEntity> entitys, bool returnId = false, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return transaction?.Connection == null
                ? await ExecuteAsync(async connection => await connection.AddAsync(this, entitys, returnId, transaction, commandTimeout), true)
                : await transaction.Connection.AddAsync(this, entitys, returnId, transaction, commandTimeout);
        }

        /// <summary>
        /// 删除
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
        /// 更新
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
        /// 查询
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
        /// 分页查询
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual async Task<IEnumerable<TEntity>> QueryPageAsync(SqlFactory sqlFactory, bool master = true, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.QueryPageAsync<TEntity>(this, sqlFactory, commandTimeout), master);
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
        /// 表是否存在
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <returns></returns>
        public virtual async Task<bool> IsTableExistsAsync(string tableName, bool master = true)
        {
            return await ExecuteAsync(async connection => await connection.IsTableExistsAsync(this, tableName), master);
        }
        #endregion
    }
}
