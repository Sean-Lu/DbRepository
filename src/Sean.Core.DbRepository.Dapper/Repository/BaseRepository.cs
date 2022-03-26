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
        /// 如果表不存在，则通过 <see cref="BaseRepository.CreateTableSql"/> 方法获取创建表的SQL语句，然后执行来创建新表
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
        /// <param name="fieldExpression">指定 INSERT 的字段。如果值为null，实体所有字段都会 INSERT（不包含自增字段和忽略字段）。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual bool Add(TEntity entity, bool returnId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return Execute(connection => connection.Add(this, entity, returnId, fieldExpression, transaction, commandTimeout), true, transaction);
        }
        /// <summary>
        /// 批量新增数据
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="returnId">是否返回自增主键Id</param>
        /// <param name="fieldExpression">指定 INSERT 的字段。如果值为null，实体所有字段都会 INSERT（不包含自增字段和忽略字段）。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual bool Add(IEnumerable<TEntity> entities, bool returnId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return Execute(connection => connection.BulkAdd(this, entities, returnId, fieldExpression, transaction, commandTimeout), true, transaction);
        }
        /// <summary>
        /// 新增数据
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual bool Add(IInsertableSql sqlFactory, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return Execute(connection => connection.Add(this, sqlFactory, transaction, commandTimeout), true, transaction);
        }

        /// <summary>
        /// 删除数据
        /// <para>Delete by primary key.</para>
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
        /// <param name="whereExpression">WHERE过滤条件</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual int Delete(Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return Execute(connection => connection.Delete(this, whereExpression, transaction, commandTimeout), true, transaction);
        }
        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual int Delete(IDeleteableSql sqlFactory, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return Execute(connection => connection.Delete(this, sqlFactory, transaction, commandTimeout), true, transaction);
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="entity">实体</param>
        /// <param name="fieldExpression">指定需要更新的字段。如果值为null，实体所有字段都会更新（不包含自增字段和忽略字段）。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="whereExpression">WHERE过滤条件。如果值为null，默认的过滤条件是实体的主键字段。
        /// <para>注：</para>
        /// <para>1. 如果实体没有主键字段，则必须设置过滤条件，否则会抛出异常（防止错误更新全表数据）。</para>
        /// <para>2. 如果需要更新全表数据，可以设置为：entity => true</para>
        /// </param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual int Update(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, Expression<Func<TEntity, bool>> whereExpression = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return Execute(connection => connection.Update(this, entity, fieldExpression, whereExpression, transaction, commandTimeout), true, transaction);
        }
        /// <summary>
        /// 批量更新数据
        /// </summary>
        /// <param name="entities">实体</param>
        /// <param name="fieldExpression">指定需要更新的字段。如果值为null，实体所有字段都会更新（不包含自增字段和忽略字段）。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="whereExpression">WHERE过滤条件。如果值为null，默认的过滤条件是实体的主键字段。
        /// <para>注：</para>
        /// <para>1. 如果实体没有主键字段，则必须设置过滤条件，否则会抛出异常（防止错误更新全表数据）。</para>
        /// <para>2. 如果需要更新全表数据，可以设置为：entity => true</para>
        /// </param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual bool Update(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, Expression<Func<TEntity, bool>> whereExpression = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return Execute(connection => connection.BulkUpdate(this, entities, fieldExpression, whereExpression, transaction, commandTimeout), true, transaction);
        }
        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual int Update(IUpdateableSql sqlFactory, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return Execute(connection => connection.Update(this, sqlFactory, transaction, commandTimeout), true, transaction);
        }

        /// <summary>
        /// 数值字段递增
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="value"></param>
        /// <param name="fieldExpression"></param>
        /// <param name="whereExpression"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual bool Incr<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null, int? commandTimeout = null) where TValue : struct
        {
            return Execute(connection => connection.Incr(this, value, fieldExpression, whereExpression, transaction, commandTimeout), true, transaction);
        }
        /// <summary>
        /// 数值字段递减
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="value"></param>
        /// <param name="fieldExpression"></param>
        /// <param name="whereExpression"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual bool Decr<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null, int? commandTimeout = null) where TValue : struct
        {
            return Execute(connection => connection.Decr(this, value, fieldExpression, whereExpression, transaction, commandTimeout), true, transaction);
        }

        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual IEnumerable<TEntity> Query(IQueryableSql sqlFactory, bool master = true, int? commandTimeout = null)
        {
            return Execute(connection => connection.Query<TEntity>(this, sqlFactory, commandTimeout), master);
        }
        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="whereExpression">WHERE过滤条件</param>
        /// <param name="fieldExpression">指定需要返回的字段。如果值为null，默认会返回所有实体字段。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="orderByCondition">排序条件</param>
        /// <param name="pageIndex">分页参数：当前页号（最小值为1）</param>
        /// <param name="pageSize">分页参数：页大小</param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual IEnumerable<TEntity> Query(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderByCondition = null, int? pageIndex = null, int? pageSize = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true, int? commandTimeout = null)
        {
            return Execute(connection => connection.Query<TEntity>(this, whereExpression, orderByCondition, pageIndex, pageSize, fieldExpression, commandTimeout), master);
        }
        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="whereExpression">WHERE过滤条件</param>
        /// <param name="fieldExpression">指定需要返回的字段。如果值为null，默认会返回所有实体字段。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="orderByCondition">排序条件</param>
        /// <param name="offset">偏移量</param>
        /// <param name="rows">行数</param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual IEnumerable<TEntity> QueryOffset(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderByCondition = null, int? offset = null, int? rows = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true, int? commandTimeout = null)
        {
            return Execute(connection => connection.QueryOffset<TEntity>(this, whereExpression, orderByCondition, offset, rows, fieldExpression, commandTimeout), master);
        }

        /// <summary>
        /// 查询单个数据
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="singleCheck">是否执行单一结果检查。true：如果查询到多个结果会抛出异常，false：默认取第一个结果或默认值</param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual TEntity Get(IQueryableSql sqlFactory, bool singleCheck = false, bool master = true, int? commandTimeout = null)
        {
            return Execute(connection => connection.Get<TEntity>(this, sqlFactory, singleCheck, commandTimeout), master);
        }
        /// <summary>
        /// 查询单个数据
        /// </summary>
        /// <param name="whereExpression">WHERE过滤条件</param>
        /// <param name="fieldExpression">指定需要返回的字段。如果值为null，默认会返回所有实体字段。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="singleCheck">是否执行单一结果检查。true：如果查询到多个结果会抛出异常，false：默认取第一个结果或默认值</param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual TEntity Get(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, object>> fieldExpression = null, bool singleCheck = false, bool master = true, int? commandTimeout = null)
        {
            return Execute(connection => connection.Get<TEntity>(this, whereExpression, fieldExpression, singleCheck, commandTimeout), master);
        }

        /// <summary>
        /// 统计数量
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual int Count(ICountableSql sqlFactory, bool master = true, int? commandTimeout = null)
        {
            return Execute(connection => connection.Count(this, sqlFactory, commandTimeout), master);
        }
        /// <summary>
        /// 统计数量
        /// </summary>
        /// <param name="whereExpression">WHERE过滤条件</param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual int Count(Expression<Func<TEntity, bool>> whereExpression, bool master = true, int? commandTimeout = null)
        {
            return Execute(connection => connection.Count(this, whereExpression, commandTimeout), master);
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

            var tableExists = Execute(connection => connection.IsTableExists(this, tableName), master);
            if (tableExists)
            {
                TableInfoCache.IsTableExists(tableName, true);
            }
            return tableExists;
        }
        #endregion

        #region Asynchronous method
#if NETSTANDARD || NET45_OR_GREATER
        /// <summary>
        /// 新增数据
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="returnId">是否返回自增主键Id</param>
        /// <param name="fieldExpression">指定 INSERT 的字段。如果值为null，实体所有字段都会 INSERT（不包含自增字段和忽略字段）。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual async Task<bool> AddAsync(TEntity entity, bool returnId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.AddAsync(this, entity, returnId, fieldExpression, transaction, commandTimeout), true, transaction);
        }
        /// <summary>
        /// 批量新增数据
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="returnId">是否返回自增主键Id</param>
        /// <param name="fieldExpression">指定 INSERT 的字段。如果值为null，实体所有字段都会 INSERT（不包含自增字段和忽略字段）。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual async Task<bool> AddAsync(IEnumerable<TEntity> entities, bool returnId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.BulkAddAsync(this, entities, returnId, fieldExpression, transaction, commandTimeout), true, transaction);
        }
        /// <summary>
        /// 新增数据
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual async Task<bool> AddAsync(IInsertableSql sqlFactory, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.AddAsync(this, sqlFactory, transaction, commandTimeout), true, transaction);
        }

        /// <summary>
        /// 删除数据
        /// <para>Delete by primary key.</para>
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
        /// <param name="whereExpression">WHERE过滤条件</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual async Task<int> DeleteAsync(Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.DeleteAsync(this, whereExpression, transaction, commandTimeout), true, transaction);
        }
        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual async Task<int> DeleteAsync(IDeleteableSql sqlFactory, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.DeleteAsync(this, sqlFactory, transaction, commandTimeout), true, transaction);
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="entity">实体</param>
        /// <param name="fieldExpression">指定需要更新的字段。如果值为null，实体所有字段都会更新（不包含自增字段和忽略字段）。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="whereExpression">WHERE过滤条件。如果值为null，默认的过滤条件是实体的主键字段。
        /// <para>注：</para>
        /// <para>1. 如果实体没有主键字段，则必须设置过滤条件，否则会抛出异常（防止错误更新全表数据）。</para>
        /// <para>2. 如果需要更新全表数据，可以设置为：entity => true</para>
        /// </param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual async Task<int> UpdateAsync(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, Expression<Func<TEntity, bool>> whereExpression = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.UpdateAsync(this, entity, fieldExpression, whereExpression, transaction, commandTimeout), true, transaction);
        }
        /// <summary>
        /// 批量更新数据
        /// </summary>
        /// <param name="entities">实体</param>
        /// <param name="fieldExpression">指定需要更新的字段。如果值为null，实体所有字段都会更新（不包含自增字段和忽略字段）。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="whereExpression">WHERE过滤条件。如果值为null，默认的过滤条件是实体的主键字段。
        /// <para>注：</para>
        /// <para>1. 如果实体没有主键字段，则必须设置过滤条件，否则会抛出异常（防止错误更新全表数据）。</para>
        /// <para>2. 如果需要更新全表数据，可以设置为：entity => true</para>
        /// </param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual async Task<bool> UpdateAsync(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, Expression<Func<TEntity, bool>> whereExpression = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.BulkUpdateAsync(this, entities, fieldExpression, whereExpression, transaction, commandTimeout), true, transaction);
        }
        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual async Task<int> UpdateAsync(IUpdateableSql sqlFactory, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.UpdateAsync(this, sqlFactory, transaction, commandTimeout), true, transaction);
        }

        /// <summary>
        /// 数值字段递增
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="value"></param>
        /// <param name="fieldExpression"></param>
        /// <param name="whereExpression"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual async Task<bool> IncrAsync<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null, int? commandTimeout = null) where TValue : struct
        {
            return await ExecuteAsync(async connection => await connection.IncrAsync(this, value, fieldExpression, whereExpression, transaction, commandTimeout), true, transaction);
        }
        /// <summary>
        /// 数值字段递减
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="value"></param>
        /// <param name="fieldExpression"></param>
        /// <param name="whereExpression"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual async Task<bool> DecrAsync<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null, int? commandTimeout = null) where TValue : struct
        {
            return await ExecuteAsync(async connection => await connection.DecrAsync(this, value, fieldExpression, whereExpression, transaction, commandTimeout), true, transaction);
        }

        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual async Task<IEnumerable<TEntity>> QueryAsync(IQueryableSql sqlFactory, bool master = true, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.QueryAsync<TEntity>(this, sqlFactory, commandTimeout), master);
        }
        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="whereExpression">WHERE过滤条件</param>
        /// <param name="fieldExpression">指定需要返回的字段。如果值为null，默认会返回所有实体字段。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="orderByCondition">排序条件</param>
        /// <param name="pageIndex">分页参数：当前页号（最小值为1）</param>
        /// <param name="pageSize">分页参数：页大小</param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual async Task<IEnumerable<TEntity>> QueryAsync(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderByCondition = null, int? pageIndex = null, int? pageSize = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.QueryAsync<TEntity>(this, whereExpression, orderByCondition, pageIndex, pageSize, fieldExpression, commandTimeout), master);
        }
        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="whereExpression">WHERE过滤条件</param>
        /// <param name="fieldExpression">指定需要返回的字段。如果值为null，默认会返回所有实体字段。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="orderByCondition">排序条件</param>
        /// <param name="offset">偏移量</param>
        /// <param name="rows">行数</param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual async Task<IEnumerable<TEntity>> QueryOffsetAsync(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderByCondition = null, int? offset = null, int? rows = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.QueryOffsetAsync<TEntity>(this, whereExpression, orderByCondition, offset, rows, fieldExpression, commandTimeout), master);
        }

        /// <summary>
        /// 查询单个数据
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="singleCheck">是否执行单一结果检查。true：如果查询到多个结果会抛出异常，false：默认取第一个结果或默认值</param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual async Task<TEntity> GetAsync(IQueryableSql sqlFactory, bool singleCheck = false, bool master = true, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.GetAsync<TEntity>(this, sqlFactory, singleCheck, commandTimeout), master);
        }
        /// <summary>
        /// 查询单个数据
        /// </summary>
        /// <param name="whereExpression">WHERE过滤条件</param>
        /// <param name="fieldExpression">指定需要返回的字段。如果值为null，默认会返回所有实体字段。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="singleCheck">是否执行单一结果检查。true：如果查询到多个结果会抛出异常，false：默认取第一个结果或默认值</param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual async Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, object>> fieldExpression = null, bool singleCheck = false, bool master = true, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.GetAsync<TEntity>(this, whereExpression, fieldExpression, singleCheck, commandTimeout), master);
        }

        /// <summary>
        /// 统计数量
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual async Task<int> CountAsync(ICountableSql sqlFactory, bool master = true, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.CountAsync(this, sqlFactory, commandTimeout), master);
        }
        /// <summary>
        /// 统计数量
        /// </summary>
        /// <param name="whereExpression">WHERE过滤条件</param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>> whereExpression, bool master = true, int? commandTimeout = null)
        {
            return await ExecuteAsync(async connection => await connection.CountAsync(this, whereExpression, commandTimeout), master);
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

            var tableExists = await ExecuteAsync(async connection => await connection.IsTableExistsAsync(this, tableName), master);
            if (tableExists)
            {
                TableInfoCache.IsTableExists(tableName, true);
            }
            return tableExists;
        }
#endif
        #endregion
    }
}
