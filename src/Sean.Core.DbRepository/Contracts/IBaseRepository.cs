using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Transactions;

namespace Sean.Core.DbRepository
{
    public interface IBaseRepository
    {
        /// <summary>
        /// Database factory
        /// </summary>
        DbFactory Factory { get; }

        /// <summary>
        /// Database type
        /// </summary>
        DatabaseType DbType { get; }

        /// <summary>
        /// 表名
        /// </summary>
        /// <returns></returns>
        string TableName();

        /// <summary>
        /// 返回创建表的SQL语句
        /// </summary>
        /// <param name="tableName">表名称</param>
        /// <returns></returns>
        string CreateTableSql(string tableName);

        /// <summary>
        /// 输出执行的SQL语句
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        void OutputExecutedSql(string sql, object param);

        #region Synchronous method
        /// <summary>
        /// 执行
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="transaction">事务</param>
        /// <returns></returns>
        T Execute<T>(Func<IDbConnection, T> func, bool master = true, IDbTransaction transaction = null);

        /// <summary>
        /// 执行事务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <returns></returns>
        T ExecuteTransaction<T>(Func<IDbTransaction, T> func);
        /// <summary>
        /// 执行事务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="func"></param>
        /// <returns></returns>
        T ExecuteTransaction<T>(IDbConnection connection, Func<IDbTransaction, T> func);

        /// <summary>
        /// <para><see cref="DbTransaction"/>、<see cref="TransactionScope"/>的区别：</para>
        /// <para><see cref="DbTransaction"/>：每个<see cref="DbTransaction"/>是基于每个<see cref="DbConnection"/>的。这种设计对于跨越多个程序集或者多个方法的事务行为来说，不是非常好，需要把事务和数据库连接作为参数传入。</para>
        /// <para><see cref="TransactionScope"/>：<see cref="TransactionScope"/>是基于当前线程的，在当前线程中，调用<see cref="Transaction.Current"/>方法可以看到当前事务的信息，调用<see cref="TransactionScope.Complete"/>方法提交事务。</para>
        /// </summary>
        /// <param name="toDoInTransactionScope"></param>
        /// <returns></returns>
        T ExecuteTransactionScope<T>(Func<TransactionScope, T> toDoInTransactionScope);
        #endregion

        #region Asynchronous method
#if NETSTANDARD || NET45_OR_GREATER
        /// <summary>
        /// 异步执行
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="transaction">事务</param>
        /// <returns></returns>
        Task<T> ExecuteAsync<T>(Func<IDbConnection, Task<T>> func, bool master = true, IDbTransaction transaction = null);

        /// <summary>
        /// 异步执行事务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <returns></returns>
        Task<T> ExecuteTransactionAsync<T>(Func<IDbTransaction, Task<T>> func);
        /// <summary>
        /// 异步执行事务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="func"></param>
        /// <returns></returns>
        Task<T> ExecuteTransactionAsync<T>(IDbConnection connection, Func<IDbTransaction, Task<T>> func);
#endif

#if NETSTANDARD || NET451_OR_GREATER
        /// <summary>
        /// <para><see cref="DbTransaction"/>、<see cref="TransactionScope"/>的区别：</para>
        /// <para><see cref="DbTransaction"/>：每个<see cref="DbTransaction"/>是基于每个<see cref="DbConnection"/>的。这种设计对于跨越多个程序集或者多个方法的事务行为来说，不是非常好，需要把事务和数据库连接作为参数传入。</para>
        /// <para><see cref="TransactionScope"/>：<see cref="TransactionScope"/>是基于当前线程的，在当前线程中，调用<see cref="Transaction.Current"/>方法可以看到当前事务的信息，调用<see cref="TransactionScope.Complete"/>方法提交事务。</para>
        /// </summary>
        /// <param name="toDoInTransactionScope"></param>
        /// <returns></returns>
        Task<T> ExecuteTransactionScopeAsync<T>(Func<TransactionScope, Task<T>> toDoInTransactionScope);
#endif
        #endregion
    }

    public interface IBaseRepository<TEntity> : IBaseRepository
    {
        /// <summary>
        /// 主表表名
        /// </summary>
        string MainTableName { get; }

        /// <summary>
        /// 如果表不存在，则通过 <see cref="IBaseRepository.CreateTableSql"/> 方法获取创建表的SQL语句，然后执行来创建新表
        /// </summary>
        /// <param name="tableName">表名称</param>
        /// <param name="master">true: 主库, false: 从库</param>
        void CreateTableIfNotExist(string tableName, bool master = true);

        #region Synchronous method
        /// <summary>
        /// 新增数据
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="returnId">是否返回自增主键Id</param>
        /// <param name="fieldExpression">指定 INSERT 的字段。如果值为null，实体所有字段都会 INSERT（不包含忽略字段）。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>是否执行成功</returns>
        bool Add(TEntity entity, bool returnId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null, int? commandTimeout = null);
        /// <summary>
        /// 批量新增数据
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="returnId">是否返回自增主键Id</param>
        /// <param name="fieldExpression">指定 INSERT 的字段。如果值为null，实体所有字段都会 INSERT（不包含忽略字段）。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>是否执行成功</returns>
        bool Add(IEnumerable<TEntity> entities, bool returnId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null, int? commandTimeout = null);
        /// <summary>
        /// 新增数据
        /// </summary>
        /// <param name="insertableSql"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>是否执行成功</returns>
        bool Add(IInsertableSql insertableSql, IDbTransaction transaction = null, int? commandTimeout = null);

        /// <summary>
        /// 新增或更新数据
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="fieldExpression">指定 INSERT OR UPDATE 的字段。如果值为null，实体所有字段都会 INSERT OR UPDATE（不包含忽略字段）。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>是否执行成功</returns>
        bool AddOrUpdate(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null, int? commandTimeout = null);
        /// <summary>
        /// 批量新增或更新数据
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="fieldExpression">指定 INSERT OR UPDATE 的字段。如果值为null，实体所有字段都会 INSERT OR UPDATE（不包含忽略字段）。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>是否执行成功</returns>
        bool AddOrUpdate(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null, int? commandTimeout = null);
        /// <summary>
        /// 新增或更新数据
        /// </summary>
        /// <param name="replaceableSql"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>是否执行成功</returns>
        bool AddOrUpdate(IReplaceableSql replaceableSql, IDbTransaction transaction = null, int? commandTimeout = null);

        /// <summary>
        /// 删除数据
        /// <para>Delete by primary key.</para>
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>是否执行成功</returns>
        bool Delete(TEntity entity, IDbTransaction transaction = null, int? commandTimeout = null);
        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="whereExpression">WHERE过滤条件</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>返回受影响的行数</returns>
        int Delete(Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null, int? commandTimeout = null);
        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="deleteableSql"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>返回受影响的行数</returns>
        int Delete(IDeleteableSql deleteableSql, IDbTransaction transaction = null, int? commandTimeout = null);

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="entity">实体</param>
        /// <param name="fieldExpression">指定 UPDATE 的字段。如果值为null，实体所有字段都会 UPDATE（不包含主键字段和忽略字段）。示例：
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
        /// <returns>返回受影响的行数</returns>
        int Update(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, Expression<Func<TEntity, bool>> whereExpression = null, IDbTransaction transaction = null, int? commandTimeout = null);
        /// <summary>
        /// 批量更新数据
        /// </summary>
        /// <param name="entities">实体</param>
        /// <param name="fieldExpression">指定 UPDATE 的字段。如果值为null，实体所有字段都会 UPDATE（不包含主键字段和忽略字段）。示例：
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
        /// <returns>是否执行成功</returns>
        bool Update(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, Expression<Func<TEntity, bool>> whereExpression = null, IDbTransaction transaction = null, int? commandTimeout = null);
        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="updateableSql"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>返回受影响的行数</returns>
        int Update(IUpdateableSql updateableSql, IDbTransaction transaction = null, int? commandTimeout = null);

        /// <summary>
        /// 数值字段递增
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="value"></param>
        /// <param name="fieldExpression"></param>
        /// <param name="whereExpression"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>是否执行成功</returns>
        bool Incr<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null, int? commandTimeout = null) where TValue : struct;
        /// <summary>
        /// 数值字段递减
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="value"></param>
        /// <param name="fieldExpression"></param>
        /// <param name="whereExpression"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>是否执行成功</returns>
        bool Decr<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null, int? commandTimeout = null) where TValue : struct;

        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="queryableSql"></param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        IEnumerable<TEntity> Query(IQueryableSql queryableSql, bool master = true, int? commandTimeout = null);
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
        IEnumerable<TEntity> Query(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderByCondition = null, int? pageIndex = null, int? pageSize = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true, int? commandTimeout = null);
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
        IEnumerable<TEntity> QueryOffset(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderByCondition = null, int? offset = null, int? rows = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true, int? commandTimeout = null);

        /// <summary>
        /// 查询单个数据
        /// </summary>
        /// <param name="queryableSql"></param>
        /// <param name="singleCheck">是否执行单一结果检查。true：如果查询到多个结果会抛出异常，false：默认取第一个结果或默认值</param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        TEntity Get(IQueryableSql queryableSql, bool singleCheck = false, bool master = true, int? commandTimeout = null);
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
        TEntity Get(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, object>> fieldExpression = null, bool singleCheck = false, bool master = true, int? commandTimeout = null);

        /// <summary>
        /// 统计数量
        /// </summary>
        /// <param name="countableSql"></param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        int Count(ICountableSql countableSql, bool master = true, int? commandTimeout = null);
        /// <summary>
        /// 统计数量
        /// </summary>
        /// <param name="whereExpression">WHERE过滤条件</param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        int Count(Expression<Func<TEntity, bool>> whereExpression, bool master = true, int? commandTimeout = null);

        /// <summary>
        /// 查询指定的表是否存在
        /// </summary>
        /// <param name="tableName">表名称</param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <returns></returns>
        bool IsTableExists(string tableName, bool master = true);
        #endregion

        #region Asynchronous method
#if NETSTANDARD || NET45_OR_GREATER
        /// <summary>
        /// 新增数据
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="returnId">是否返回自增主键Id</param>
        /// <param name="fieldExpression">指定 INSERT 的字段。如果值为null，实体所有字段都会 INSERT（不包含忽略字段）。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>是否执行成功</returns>
        Task<bool> AddAsync(TEntity entity, bool returnId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null, int? commandTimeout = null);
        /// <summary>
        /// 批量新增数据
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="returnId">是否返回自增主键Id</param>
        /// <param name="fieldExpression">指定 INSERT 的字段。如果值为null，实体所有字段都会 INSERT（不包含忽略字段）。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>是否执行成功</returns>
        Task<bool> AddAsync(IEnumerable<TEntity> entities, bool returnId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null, int? commandTimeout = null);
        /// <summary>
        /// 新增数据
        /// </summary>
        /// <param name="insertableSql"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>是否执行成功</returns>
        Task<bool> AddAsync(IInsertableSql insertableSql, IDbTransaction transaction = null, int? commandTimeout = null);

        /// <summary>
        /// 新增或更新数据
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="fieldExpression">指定 INSERT OR UPDATE 的字段。如果值为null，实体所有字段都会 INSERT OR UPDATE（不包含忽略字段）。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>是否执行成功</returns>
        Task<bool> AddOrUpdateAsync(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null, int? commandTimeout = null);
        /// <summary>
        /// 批量新增或更新数据
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="fieldExpression">指定 INSERT OR UPDATE 的字段。如果值为null，实体所有字段都会 INSERT OR UPDATE（不包含忽略字段）。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>是否执行成功</returns>
        Task<bool> AddOrUpdateAsync(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null, int? commandTimeout = null);
        /// <summary>
        /// 新增或更新数据
        /// </summary>
        /// <param name="replaceableSql"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>是否执行成功</returns>
        Task<bool> AddOrUpdateAsync(IReplaceableSql replaceableSql, IDbTransaction transaction = null, int? commandTimeout = null);

        /// <summary>
        /// 删除数据
        /// <para>Delete by primary key.</para>
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>是否执行成功</returns>
        Task<bool> DeleteAsync(TEntity entity, IDbTransaction transaction = null, int? commandTimeout = null);
        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="whereExpression">WHERE过滤条件</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>返回受影响的行数</returns>
        Task<int> DeleteAsync(Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null, int? commandTimeout = null);
        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="deleteableSql"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>返回受影响的行数</returns>
        Task<int> DeleteAsync(IDeleteableSql deleteableSql, IDbTransaction transaction = null, int? commandTimeout = null);

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="entity">实体</param>
        /// <param name="fieldExpression">指定 UPDATE 的字段。如果值为null，实体所有字段都会 UPDATE（不包含主键字段和忽略字段）。示例：
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
        /// <returns>返回受影响的行数</returns>
        Task<int> UpdateAsync(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, Expression<Func<TEntity, bool>> whereExpression = null, IDbTransaction transaction = null, int? commandTimeout = null);
        /// <summary>
        /// 批量更新数据
        /// </summary>
        /// <param name="entities">实体</param>
        /// <param name="fieldExpression">指定 UPDATE 的字段。如果值为null，实体所有字段都会 UPDATE（不包含主键字段和忽略字段）。示例：
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
        /// <returns>是否执行成功</returns>
        Task<bool> UpdateAsync(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, Expression<Func<TEntity, bool>> whereExpression = null, IDbTransaction transaction = null, int? commandTimeout = null);
        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="updateableSql"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>返回受影响的行数</returns>
        Task<int> UpdateAsync(IUpdateableSql updateableSql, IDbTransaction transaction = null, int? commandTimeout = null);

        /// <summary>
        /// 数值字段递增
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="value"></param>
        /// <param name="fieldExpression"></param>
        /// <param name="whereExpression"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>是否执行成功</returns>
        Task<bool> IncrAsync<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null, int? commandTimeout = null) where TValue : struct;
        /// <summary>
        /// 数值字段递减
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="value"></param>
        /// <param name="fieldExpression"></param>
        /// <param name="whereExpression"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>是否执行成功</returns>
        Task<bool> DecrAsync<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null, int? commandTimeout = null) where TValue : struct;

        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="queryableSql"></param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        Task<IEnumerable<TEntity>> QueryAsync(IQueryableSql queryableSql, bool master = true, int? commandTimeout = null);
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
        Task<IEnumerable<TEntity>> QueryAsync(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderByCondition = null, int? pageIndex = null, int? pageSize = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true, int? commandTimeout = null);
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
        Task<IEnumerable<TEntity>> QueryOffsetAsync(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderByCondition = null, int? offset = null, int? rows = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true, int? commandTimeout = null);

        /// <summary>
        /// 查询单个数据
        /// </summary>
        /// <param name="queryableSql"></param>
        /// <param name="singleCheck">是否执行单一结果检查。true：如果查询到多个结果会抛出异常，false：默认取第一个结果或默认值</param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        Task<TEntity> GetAsync(IQueryableSql queryableSql, bool singleCheck = false, bool master = true, int? commandTimeout = null);
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
        Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, object>> fieldExpression = null, bool singleCheck = false, bool master = true, int? commandTimeout = null);

        /// <summary>
        /// 统计数量
        /// </summary>
        /// <param name="countableSql"></param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        Task<int> CountAsync(ICountableSql countableSql, bool master = true, int? commandTimeout = null);
        /// <summary>
        /// 统计数量
        /// </summary>
        /// <param name="whereExpression">WHERE过滤条件</param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        Task<int> CountAsync(Expression<Func<TEntity, bool>> whereExpression, bool master = true, int? commandTimeout = null);

        /// <summary>
        /// 查询指定的表是否存在
        /// </summary>
        /// <param name="tableName">表名称</param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <returns></returns>
        Task<bool> IsTableExistsAsync(string tableName, bool master = true);
#endif
        #endregion
    }
}