using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Transactions;

namespace Sean.Core.DbRepository
{
    public interface IBaseRepository : ISqlMonitor
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
        /// Number of seconds before command execution timeout.
        /// </summary>
        int? CommandTimeout { get; set; }

        /// <summary>
        /// Return table name.
        /// </summary>
        /// <returns></returns>
        string TableName();

        /// <summary>
        /// 返回创建表的SQL语句
        /// </summary>
        /// <param name="tableName">The table name.</param>
        /// <returns></returns>
        string CreateTableSql(string tableName);

        /// <summary>
        /// Create and open a new connection
        /// </summary>
        /// <param name="master">true: use master database, false: use slave database.</param>
        /// <returns></returns>
        IDbConnection OpenNewConnection(bool master);

        #region Synchronous method
        int Execute(string sql, object param = null, IDbTransaction transaction = null, bool master = true);
        IEnumerable<T> Query<T>(string sql, object param = null, IDbTransaction transaction = null, bool master = true);
        T QueryFirstOrDefault<T>(string sql, object param = null, IDbTransaction transaction = null, bool master = true);
        T ExecuteScalar<T>(string sql, object param = null, IDbTransaction transaction = null, bool master = true);

        /// <summary>
        /// 执行
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <param name="master">true: use master database, false: use slave database.</param>
        /// <param name="transaction">The transaction to use for this command.</param>
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
        /// 执行事务（自动提交或回滚事务）
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        bool ExecuteAutoTransaction(Func<IDbTransaction, bool> func);
        /// <summary>
        /// 执行事务（自动提交或回滚事务）
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="func"></param>
        /// <returns></returns>
        bool ExecuteAutoTransaction(IDbConnection connection, Func<IDbTransaction, bool> func);

        /// <summary>
        /// 执行事务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tranScope"></param>
        /// <returns></returns>
        T ExecuteTransactionScope<T>(Func<TransactionScope, T> tranScope);
        /// <summary>
        /// 执行事务（自动提交或回滚事务）
        /// </summary>
        /// <param name="tranScope"></param>
        /// <returns></returns>
        bool ExecuteAutoTransactionScope(Func<TransactionScope, bool> tranScope);
        #endregion

        #region Asynchronous method
#if NETSTANDARD || NET45_OR_GREATER
        Task<int> ExecuteAsync(string sql, object param = null, IDbTransaction transaction = null, bool master = true);
        Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null, IDbTransaction transaction = null, bool master = true);
        Task<T> QueryFirstOrDefaultAsync<T>(string sql, object param = null, IDbTransaction transaction = null, bool master = true);
        Task<T> ExecuteScalarAsync<T>(string sql, object param = null, IDbTransaction transaction = null, bool master = true);

        /// <summary>
        /// 异步执行
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <param name="master">true: use master database, false: use slave database.</param>
        /// <param name="transaction">The transaction to use for this command.</param>
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
        /// <summary>
        /// 异步执行事务（自动提交或回滚事务）
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        Task<bool> ExecuteAutoTransactionAsync(Func<IDbTransaction, Task<bool>> func);
        /// <summary>
        /// 异步执行事务（自动提交或回滚事务）
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        Task<bool> ExecuteAutoTransactionAsync(IDbConnection connection, Func<IDbTransaction, Task<bool>> func);
#endif

#if NETSTANDARD || NET451_OR_GREATER
        /// <summary>
        /// 异步执行事务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tranScope"></param>
        /// <returns></returns>
        Task<T> ExecuteTransactionScopeAsync<T>(Func<TransactionScope, Task<T>> tranScope);
        /// <summary>
        /// 异步执行事务（自动提交或回滚事务）
        /// </summary>
        /// <param name="tranScope"></param>
        /// <returns></returns>
        Task<bool> ExecuteAutoTransactionScopeAsync(Func<TransactionScope, Task<bool>> tranScope);
#endif

        #endregion
    }

    public interface IBaseRepository<TEntity> : IBaseRepository where TEntity : class
    {
        /// <summary>
        /// 主表表名
        /// </summary>
        string MainTableName { get; }

        /// <summary>
        /// 如果表不存在，则通过 <see cref="IBaseRepository.CreateTableSql"/> 方法获取创建表的SQL语句，然后执行来创建新表
        /// </summary>
        /// <param name="tableName">The table name.</param>
        void AutoCreateTable(string tableName);

        #region Synchronous method
        /// <summary>
        /// 新增数据
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="returnAutoIncrementId">Whether to return the auto-increment primary key Id.</param>
        /// <param name="fieldExpression">The fields to insert. If the value is null, all fields of the entity are inserted (excluding NotMapped fields). Example: 
        /// <para>1. Single field: entity => entity.Status</para>
        /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>Whether the command is executed successfully.</returns>
        bool Add(TEntity entity, bool returnAutoIncrementId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null);
        /// <summary>
        /// 批量新增数据
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="returnAutoIncrementId">Whether to return the auto-increment primary key Id.</param>
        /// <param name="fieldExpression">The fields to insert. If the value is null, all fields of the entity are inserted (excluding NotMapped fields). Example: 
        /// <para>1. Single field: entity => entity.Status</para>
        /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>Whether the command is executed successfully.</returns>
        bool Add(IEnumerable<TEntity> entities, bool returnAutoIncrementId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null);

        /// <summary>
        /// 新增或更新数据
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="fieldExpression">The fields to insert or update. If the value is null, all fields of the entity are inserted or updated (excluding NotMapped fields). Example: 
        /// <para>1. Single field: entity => entity.Status</para>
        /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>Whether the command is executed successfully.</returns>
        bool AddOrUpdate(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null);
        /// <summary>
        /// 批量新增或更新数据
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="fieldExpression">The fields to insert or update. If the value is null, all fields of the entity are inserted or updated (excluding NotMapped fields). Example: 
        /// <para>1. Single field: entity => entity.Status</para>
        /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>Whether the command is executed successfully.</returns>
        bool AddOrUpdate(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null);

        /// <summary>
        /// 删除数据
        /// <para>Delete by primary key.</para>
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>Whether the command is executed successfully.</returns>
        bool Delete(TEntity entity, IDbTransaction transaction = null);
        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>The number of rows affected.</returns>
        int Delete(Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null);

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="fieldExpression">The fields to update. If the value is null, all fields of the entity are updated (excluding primary key fields and NotMapped fields). Example: 
        /// <para>1. Single field: entity => entity.Status</para>
        /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition. If the value is null, the SQL WHERE condition will default to the primary key field of the entity.
        /// <para>1. If the entity does not contain primary key field and the value is null, an exception will be thrown (to prevent all data in the table from being incorrectly updated).</para>
        /// <para>2. Code example for update all data in the table: entity => true.</para>
        /// </param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>The number of rows affected.</returns>
        int Update(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, Expression<Func<TEntity, bool>> whereExpression = null, IDbTransaction transaction = null);
        /// <summary>
        /// 批量更新数据
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="fieldExpression">The fields to update. If the value is null, all fields of the entity are updated (excluding primary key fields and NotMapped fields). Example: 
        /// <para>1. Single field: entity => entity.Status</para>
        /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>Whether the command is executed successfully.</returns>
        bool Update(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null);

        /// <summary>
        /// 数值字段递增
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="value"></param>
        /// <param name="fieldExpression"></param>
        /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>Whether the command is executed successfully.</returns>
        bool Incr<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null) where TValue : struct;
        /// <summary>
        /// 数值字段递减
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="value"></param>
        /// <param name="fieldExpression"></param>
        /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>Whether the command is executed successfully.</returns>
        bool Decr<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null) where TValue : struct;

        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
        /// <param name="fieldExpression">The fields to query. If the value is null, all fields of the entity are queried. Example: 
        /// <para>1. Single field: entity => entity.Status</para>
        /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="orderBy">SQL ORDER BY condition.</param>
        /// <param name="pageIndex">The current page index for paging query, the minimum value is 1.</param>
        /// <param name="pageSize">The page size for paging query.</param>
        /// <param name="master">true: use master database, false: use slave database.</param>
        /// <returns></returns>
        IEnumerable<TEntity> Query(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? pageIndex = null, int? pageSize = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true);
        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
        /// <param name="fieldExpression">The fields to query. If the value is null, all fields of the entity are queried. Example: 
        /// <para>1. Single field: entity => entity.Status</para>
        /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="orderBy">SQL ORDER BY condition.</param>
        /// <param name="offset">Offset to use for this query.</param>
        /// <param name="rows">The number of rows queried.</param>
        /// <param name="master">true: use master database, false: use slave database.</param>
        /// <returns></returns>
        IEnumerable<TEntity> QueryOffset(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? offset = null, int? rows = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true);

        /// <summary>
        /// 查询单个数据
        /// </summary>
        /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
        /// <param name="fieldExpression">The fields to query. If the value is null, all fields of the entity are queried. Example: 
        /// <para>1. Single field: entity => entity.Status</para>
        /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="master">true: use master database, false: use slave database.</param>
        /// <returns></returns>
        TEntity Get(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true);

        /// <summary>
        /// 统计数量
        /// </summary>
        /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
        /// <param name="master">true: use master database, false: use slave database.</param>
        /// <returns></returns>
        int Count(Expression<Func<TEntity, bool>> whereExpression, bool master = true);

        /// <summary>
        /// Whether the specified table exists.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        /// <param name="master">true: use master database, false: use slave database.</param>
        /// <returns></returns>
        bool IsTableExists(string tableName, bool master = true, bool useCache = true);

        bool IsTableFieldExists(string tableName, string fieldName, bool master = true, bool useCache = true);
        bool IsTableFieldExists(Expression<Func<TEntity, object>> fieldExpression, bool master = true, bool useCache = true);
        #endregion

        #region Asynchronous method
#if NETSTANDARD || NET45_OR_GREATER
        /// <summary>
        /// 新增数据
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="returnAutoIncrementId">Whether to return the auto-increment primary key Id.</param>
        /// <param name="fieldExpression">The fields to insert. If the value is null, all fields of the entity are inserted (excluding NotMapped fields). Example: 
        /// <para>1. Single field: entity => entity.Status</para>
        /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>Whether the command is executed successfully.</returns>
        Task<bool> AddAsync(TEntity entity, bool returnAutoIncrementId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null);
        /// <summary>
        /// 批量新增数据
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="returnAutoIncrementId">Whether to return the auto-increment primary key Id.</param>
        /// <param name="fieldExpression">The fields to insert. If the value is null, all fields of the entity are inserted (excluding NotMapped fields). Example: 
        /// <para>1. Single field: entity => entity.Status</para>
        /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>Whether the command is executed successfully.</returns>
        Task<bool> AddAsync(IEnumerable<TEntity> entities, bool returnAutoIncrementId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null);

        /// <summary>
        /// 新增或更新数据
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="fieldExpression">The fields to insert or update. If the value is null, all fields of the entity are inserted or updated (excluding NotMapped fields). Example: 
        /// <para>1. Single field: entity => entity.Status</para>
        /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>Whether the command is executed successfully.</returns>
        Task<bool> AddOrUpdateAsync(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null);
        /// <summary>
        /// 批量新增或更新数据
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="fieldExpression">The fields to insert or update. If the value is null, all fields of the entity are inserted or updated (excluding NotMapped fields). Example: 
        /// <para>1. Single field: entity => entity.Status</para>
        /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>Whether the command is executed successfully.</returns>
        Task<bool> AddOrUpdateAsync(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null);

        /// <summary>
        /// 删除数据
        /// <para>Delete by primary key.</para>
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>Whether the command is executed successfully.</returns>
        Task<bool> DeleteAsync(TEntity entity, IDbTransaction transaction = null);
        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>The number of rows affected.</returns>
        Task<int> DeleteAsync(Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null);

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="fieldExpression">The fields to update. If the value is null, all fields of the entity are updated (excluding primary key fields and NotMapped fields). Example: 
        /// <para>1. Single field: entity => entity.Status</para>
        /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition. If the value is null, the SQL WHERE condition will default to the primary key field of the entity.
        /// <para>1. If the entity does not contain primary key field and the value is null, an exception will be thrown (to prevent all data in the table from being incorrectly updated).</para>
        /// <para>2. Code example for update all data in the table: entity => true.</para>
        /// </param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>The number of rows affected.</returns>
        Task<int> UpdateAsync(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, Expression<Func<TEntity, bool>> whereExpression = null, IDbTransaction transaction = null);
        /// <summary>
        /// 批量更新数据
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="fieldExpression">The fields to update. If the value is null, all fields of the entity are updated (excluding primary key fields and NotMapped fields). Example: 
        /// <para>1. Single field: entity => entity.Status</para>
        /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>Whether the command is executed successfully.</returns>
        Task<bool> UpdateAsync(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null);

        /// <summary>
        /// 数值字段递增
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="value"></param>
        /// <param name="fieldExpression"></param>
        /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>Whether the command is executed successfully.</returns>
        Task<bool> IncrAsync<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null) where TValue : struct;
        /// <summary>
        /// 数值字段递减
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="value"></param>
        /// <param name="fieldExpression"></param>
        /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>Whether the command is executed successfully.</returns>
        Task<bool> DecrAsync<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null) where TValue : struct;

        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
        /// <param name="fieldExpression">The fields to query. If the value is null, all fields of the entity are queried. Example: 
        /// <para>1. Single field: entity => entity.Status</para>
        /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="orderBy">SQL ORDER BY condition.</param>
        /// <param name="pageIndex">The current page index for paging query, the minimum value is 1.</param>
        /// <param name="pageSize">The page size for paging query.</param>
        /// <param name="master">true: use master database, false: use slave database.</param>
        /// <returns></returns>
        Task<IEnumerable<TEntity>> QueryAsync(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? pageIndex = null, int? pageSize = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true);
        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
        /// <param name="fieldExpression">The fields to query. If the value is null, all fields of the entity are queried. Example: 
        /// <para>1. Single field: entity => entity.Status</para>
        /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="orderBy">SQL ORDER BY condition.</param>
        /// <param name="offset">Offset to use for this query.</param>
        /// <param name="rows">The number of rows queried.</param>
        /// <param name="master">true: use master database, false: use slave database.</param>
        /// <returns></returns>
        Task<IEnumerable<TEntity>> QueryOffsetAsync(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? offset = null, int? rows = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true);

        /// <summary>
        /// 查询单个数据
        /// </summary>
        /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
        /// <param name="fieldExpression">The fields to query. If the value is null, all fields of the entity are queried. Example: 
        /// <para>1. Single field: entity => entity.Status</para>
        /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="master">true: use master database, false: use slave database.</param>
        /// <returns></returns>
        Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true);

        /// <summary>
        /// 统计数量
        /// </summary>
        /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
        /// <param name="master">true: use master database, false: use slave database.</param>
        /// <returns></returns>
        Task<int> CountAsync(Expression<Func<TEntity, bool>> whereExpression, bool master = true);

        /// <summary>
        /// Whether the specified table exists.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        /// <param name="master">true: use master database, false: use slave database.</param>
        /// <returns></returns>
        Task<bool> IsTableExistsAsync(string tableName, bool master = true, bool useCache = true);

        Task<bool> IsTableFieldExistsAsync(string tableName, string fieldName, bool master = true, bool useCache = true);
        Task<bool> IsTableFieldExistsAsync(Expression<Func<TEntity, object>> fieldExpression, bool master = true, bool useCache = true);
#endif

        #endregion
    }
}