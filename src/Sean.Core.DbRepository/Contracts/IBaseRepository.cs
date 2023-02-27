using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Sean.Core.DbRepository;

/// <summary>
/// Database table base repository.
/// </summary>
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

    ISqlMonitor SqlMonitor { get; }

    /// <summary>
    /// The limit on the number of entities when executing database bulk operations.
    /// </summary>
    int? BulkCountLimit { get; set; }

    /// <summary>
    /// Number of seconds before command execution timeout.
    /// </summary>
    int? CommandTimeout { get; set; }

    /// <summary>
    /// Table name.
    /// </summary>
    /// <returns></returns>
    string TableName();

    /// <summary>
    /// Gets the SQL of database table creation.
    /// </summary>
    /// <param name="tableName">The table name.</param>
    /// <returns></returns>
    string CreateTableSql(string tableName);

    /// <summary>
    /// The database table is automatically created if it does not exist. The <see cref="CreateTableSql"/> method needs to be implemented.
    /// </summary>
    /// <param name="tableName">The table name.</param>
    void AutoCreateTable(string tableName);

    /// <summary>
    /// Create and open a new connection
    /// </summary>
    /// <param name="master">true: master database, false: slave database.</param>
    /// <returns></returns>
    DbConnection OpenNewConnection(bool master = true);

    #region Synchronous method
    int Execute(string sql, object param = null, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null);
    int Execute(ISqlCommand sqlCommand);

    IEnumerable<T> Query<T>(string sql, object param = null, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null);
    IEnumerable<T> Query<T>(ISqlCommand sqlCommand);

    T Get<T>(string sql, object param = null, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null);
    T Get<T>(ISqlCommand sqlCommand);

    T ExecuteScalar<T>(string sql, object param = null, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null);
    T ExecuteScalar<T>(ISqlCommand sqlCommand);

    object ExecuteScalar(string sql, object param = null, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null);
    object ExecuteScalar(ISqlCommand sqlCommand);

    DataTable ExecuteDataTable(string sql, object param = null, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null);
    DataTable ExecuteDataTable(ISqlCommand sqlCommand);

    DataSet ExecuteDataSet(string sql, object param = null, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null);
    DataSet ExecuteDataSet(ISqlCommand sqlCommand);

    IDataReader ExecuteReader(string sql, object param = null, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null);
    IDataReader ExecuteReader(ISqlCommand sqlCommand);

    /// <summary>
    /// Execute using DbConnection.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="func"></param>
    /// <param name="master">true: master database, false: slave database.</param>
    /// <param name="transaction">The transaction to use for this command.</param>
    /// <returns></returns>
    T Execute<T>(Func<IDbConnection, T> func, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null);

    /// <summary>
    /// Execute using DbTransaction.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="func"></param>
    /// <returns></returns>
    T ExecuteTransaction<T>(Func<IDbTransaction, T> func, IDbTransaction transaction = null, IDbConnection connection = null);
    /// <summary>
    /// Execute using DbTransaction, automatically commit or rollback the transaction.
    /// </summary>
    /// <param name="func"></param>
    /// <returns></returns>
    bool ExecuteAutoTransaction(Func<IDbTransaction, bool> func, IDbTransaction transaction = null, IDbConnection connection = null);

    /// <summary>
    /// Whether the specified table exists.
    /// </summary>
    /// <param name="tableName">Table name.</param>
    /// <param name="master">true: master database, false: slave database.</param>
    /// <param name="useCache">Whether to use cache.</param>
    /// <returns></returns>
    bool IsTableExists(string tableName, bool master = true, bool useCache = true);

    /// <summary>
    /// Whether the specified table field exists.
    /// </summary>
    /// <param name="tableName">Table name.</param>
    /// <param name="fieldName">Table field name.</param>
    /// <param name="master">true: master database, false: slave database.</param>
    /// <param name="useCache">Whether to use cache.</param>
    /// <returns></returns>
    bool IsTableFieldExists(string tableName, string fieldName, bool master = true, bool useCache = true);

    /// <summary>
    /// ALTER TABLE {tableName} ADD COLUMN {fieldName} {fieldType};
    /// </summary>
    /// <param name="tableName">Table name.</param>
    /// <param name="fieldName">Table field name.</param>
    /// <param name="fieldType">Table field type.</param>
    /// <param name="master">true: master database, false: slave database.</param>
    void AddTableField(string tableName, string fieldName, string fieldType, bool master = true);

    /// <summary>
    /// DELETE FROM {tableName};
    /// </summary>
    /// <param name="tableName">Table name.</param>
    /// <returns></returns>
    int DeleteAll(string tableName, IDbTransaction transaction = null);
    #endregion

    #region Asynchronous method
#if NETSTANDARD || NET45_OR_GREATER
    Task<int> ExecuteAsync(string sql, object param = null, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null);
    Task<int> ExecuteAsync(ISqlCommand sqlCommand);

    Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null);
    Task<IEnumerable<T>> QueryAsync<T>(ISqlCommand sqlCommand);

    Task<T> GetAsync<T>(string sql, object param = null, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null);
    Task<T> GetAsync<T>(ISqlCommand sqlCommand);

    Task<T> ExecuteScalarAsync<T>(string sql, object param = null, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null);
    Task<T> ExecuteScalarAsync<T>(ISqlCommand sqlCommand);

    Task<object> ExecuteScalarAsync(string sql, object param = null, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null);
    Task<object> ExecuteScalarAsync(ISqlCommand sqlCommand);

    Task<DataTable> ExecuteDataTableAsync(string sql, object param = null, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null);
    Task<DataTable> ExecuteDataTableAsync(ISqlCommand sqlCommand);

    Task<DataSet> ExecuteDataSetAsync(string sql, object param = null, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null);
    Task<DataSet> ExecuteDataSetAsync(ISqlCommand sqlCommand);

    Task<IDataReader> ExecuteReaderAsync(string sql, object param = null, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null);
    Task<IDataReader> ExecuteReaderAsync(ISqlCommand sqlCommand);

    /// <summary>
    /// Execute asynchronously using DbConnection.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="func"></param>
    /// <param name="master">true: master database, false: slave database.</param>
    /// <param name="transaction">The transaction to use for this command.</param>
    /// <returns></returns>
    Task<T> ExecuteAsync<T>(Func<IDbConnection, Task<T>> func, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null);

    /// <summary>
    /// Execute asynchronously using DbTransaction.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="func"></param>
    /// <returns></returns>
    Task<T> ExecuteTransactionAsync<T>(Func<IDbTransaction, Task<T>> func, IDbTransaction transaction = null, IDbConnection connection = null);
    /// <summary>
    /// Execute asynchronously using DbTransaction, automatically commit or rollback the transaction.
    /// </summary>
    /// <param name="func"></param>
    /// <returns></returns>
    Task<bool> ExecuteAutoTransactionAsync(Func<IDbTransaction, Task<bool>> func, IDbTransaction transaction = null, IDbConnection connection = null);

    /// <summary>
    /// Whether the specified table exists.
    /// </summary>
    /// <param name="tableName">Table name.</param>
    /// <param name="master">true: master database, false: slave database.</param>
    /// <param name="useCache">Whether to use cache.</param>
    /// <returns></returns>
    Task<bool> IsTableExistsAsync(string tableName, bool master = true, bool useCache = true);

    /// <summary>
    /// Whether the specified table field exists.
    /// </summary>
    /// <param name="tableName">Table name.</param>
    /// <param name="fieldName">Table field name.</param>
    /// <param name="master">true: master database, false: slave database.</param>
    /// <param name="useCache">Whether to use cache.</param>
    /// <returns></returns>
    Task<bool> IsTableFieldExistsAsync(string tableName, string fieldName, bool master = true, bool useCache = true);

    /// <summary>
    /// ALTER TABLE {tableName} ADD COLUMN {fieldName} {fieldType};
    /// </summary>
    /// <param name="tableName">Table name.</param>
    /// <param name="fieldName">Table field name.</param>
    /// <param name="fieldType">Table field type.</param>
    /// <param name="master">true: master database, false: slave database.</param>
    /// <returns></returns>
    Task AddTableFieldAsync(string tableName, string fieldName, string fieldType, bool master = true);

    /// <summary>
    /// DELETE FROM {tableName};
    /// </summary>
    /// <param name="tableName">Table name.</param>
    /// <returns></returns>
    Task<int> DeleteAllAsync(string tableName, IDbTransaction transaction = null);
#endif

    #endregion
}

/// <summary>
/// Database table entity base repository.
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public interface IBaseRepository<TEntity> : IBaseRepository where TEntity : class
{
    /// <summary>
    /// Name of the master table.
    /// </summary>
    string MainTableName { get; }

    #region Synchronous method
    /// <summary>
    /// Insert entity.
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
    /// Bulk insert entities.
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
    /// Insert or update entity.
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
    /// Bulk insert or update entities.
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
    /// Delete by entity primary key.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="transaction">The transaction to use for this command.</param>
    /// <returns>Whether the command is executed successfully.</returns>
    bool Delete(TEntity entity, IDbTransaction transaction = null);
    /// <summary>
    /// Bulk delete by entity primary key.
    /// </summary>
    /// <param name="entities"></param>
    /// <param name="transaction"></param>
    /// <returns></returns>
    bool Delete(IEnumerable<TEntity> entities, IDbTransaction transaction = null);
    /// <summary>
    /// Delete by <paramref name="whereExpression"/>.
    /// </summary>
    /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
    /// <param name="transaction">The transaction to use for this command.</param>
    /// <returns>The number of rows affected.</returns>
    int Delete(Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null);
    /// <summary>
    /// Delete all data.
    /// </summary>
    /// <returns></returns>
    int DeleteAll(IDbTransaction transaction = null);

    /// <summary>
    /// Update entity.
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
    /// Bulk update entities.
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
    /// Increments the value of numeric field.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="value"></param>
    /// <param name="fieldExpression"></param>
    /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
    /// <param name="transaction">The transaction to use for this command.</param>
    /// <returns>Whether the command is executed successfully.</returns>
    bool Increment<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null) where TValue : struct;
    /// <summary>
    /// Decrements the value of numeric field.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="value"></param>
    /// <param name="fieldExpression"></param>
    /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
    /// <param name="transaction">The transaction to use for this command.</param>
    /// <returns>Whether the command is executed successfully.</returns>
    bool Decrement<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null) where TValue : struct;

    /// <summary>
    /// Save entity to the database by checking entity state.
    /// </summary>
    /// <typeparam name="TEntityState"></typeparam>
    /// <param name="entity"></param>
    /// <param name="returnAutoIncrementId"></param>
    /// <param name="transaction"></param>
    /// <returns></returns>
    bool Save<TEntityState>(TEntityState entity, bool returnAutoIncrementId = false, IDbTransaction transaction = null) where TEntityState : EntityStateBase, TEntity;
    /// <summary>
    /// Save entities to the database by checking entity state.
    /// </summary>
    /// <typeparam name="TEntityState"></typeparam>
    /// <param name="entities"></param>
    /// <param name="returnAutoIncrementId"></param>
    /// <param name="transaction"></param>
    /// <returns></returns>
    bool Save<TEntityState>(IEnumerable<TEntityState> entities, bool returnAutoIncrementId = false, IDbTransaction transaction = null) where TEntityState : EntityStateBase, TEntity;

    /// <summary>
    /// Query entities.
    /// </summary>
    /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
    /// <param name="fieldExpression">The fields to query. If the value is null, all fields of the entity are queried. Example: 
    /// <para>1. Single field: entity => entity.Status</para>
    /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
    /// </param>
    /// <param name="orderBy">SQL ORDER BY condition.</param>
    /// <param name="pageIndex">The current page index for paging query, the minimum value is 1.</param>
    /// <param name="pageSize">The page size for paging query.</param>
    /// <param name="master">true: master database, false: slave database.</param>
    /// <returns></returns>
    IEnumerable<TEntity> Query(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? pageIndex = null, int? pageSize = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true);
    /// <summary>
    /// Query entities.
    /// </summary>
    /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
    /// <param name="fieldExpression">The fields to query. If the value is null, all fields of the entity are queried. Example: 
    /// <para>1. Single field: entity => entity.Status</para>
    /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
    /// </param>
    /// <param name="orderBy">SQL ORDER BY condition.</param>
    /// <param name="offset">Offset to use for this query.</param>
    /// <param name="rows">The number of rows queried.</param>
    /// <param name="master">true: master database, false: slave database.</param>
    /// <returns></returns>
    IEnumerable<TEntity> QueryOffset(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? offset = null, int? rows = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true);

    /// <summary>
    /// Query single entity.
    /// </summary>
    /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
    /// <param name="fieldExpression">The fields to query. If the value is null, all fields of the entity are queried. Example: 
    /// <para>1. Single field: entity => entity.Status</para>
    /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
    /// </param>
    /// <param name="master">true: master database, false: slave database.</param>
    /// <returns></returns>
    TEntity Get(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true);

    /// <summary>
    /// Gets the count that satisfy the <paramref name="whereExpression"/>.
    /// </summary>
    /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
    /// <param name="master">true: master database, false: slave database.</param>
    /// <returns></returns>
    int Count(Expression<Func<TEntity, bool>> whereExpression, bool master = true);

    /// <summary>
    /// Whether the data exists that satisfy the <paramref name="whereExpression"/>.
    /// </summary>
    /// <param name="whereExpression"></param>
    /// <param name="master"></param>
    /// <returns></returns>
    bool Exists(Expression<Func<TEntity, bool>> whereExpression, bool master = true);

    /// <summary>
    /// Whether the table exists.
    /// </summary>
    /// <param name="master"></param>
    /// <param name="useCache"></param>
    /// <returns></returns>
    bool IsTableExists(bool master = true, bool useCache = true);

    /// <summary>
    /// Whether the table field exists.
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <param name="master"></param>
    /// <param name="useCache"></param>
    /// <returns></returns>
    bool IsTableFieldExists(Expression<Func<TEntity, object>> fieldExpression, bool master = true, bool useCache = true);

    /// <summary>
    /// Add table field if it does not exist.
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <param name="fieldType"></param>
    /// <param name="master"></param>
    void AddTableField(Expression<Func<TEntity, object>> fieldExpression, string fieldType, bool master = true);
    #endregion

    #region Asynchronous method
#if NETSTANDARD || NET45_OR_GREATER
    /// <summary>
    /// Insert entity.
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
    /// Bulk insert entities.
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
    /// Insert or update entity.
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
    /// Bulk Insert or update entities.
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
    /// Delete by entity primary key.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="transaction">The transaction to use for this command.</param>
    /// <returns>Whether the command is executed successfully.</returns>
    Task<bool> DeleteAsync(TEntity entity, IDbTransaction transaction = null);
    /// <summary>
    /// Bulk delete by entity primary key.
    /// </summary>
    /// <param name="entities"></param>
    /// <param name="transaction"></param>
    /// <returns></returns>
    Task<bool> DeleteAsync(IEnumerable<TEntity> entities, IDbTransaction transaction = null);
    /// <summary>
    /// Delete by <paramref name="whereExpression"/>.
    /// </summary>
    /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
    /// <param name="transaction">The transaction to use for this command.</param>
    /// <returns>The number of rows affected.</returns>
    Task<int> DeleteAsync(Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null);
    /// <summary>
    /// Delete all data.
    /// </summary>
    /// <returns></returns>
    Task<int> DeleteAllAsync(IDbTransaction transaction = null);

    /// <summary>
    /// Update entity.
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
    /// Bulk update entities.
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
    /// Increments the value of numeric field.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="value"></param>
    /// <param name="fieldExpression"></param>
    /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
    /// <param name="transaction">The transaction to use for this command.</param>
    /// <returns>Whether the command is executed successfully.</returns>
    Task<bool> IncrementAsync<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null) where TValue : struct;
    /// <summary>
    /// Decrements the value of numeric field.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="value"></param>
    /// <param name="fieldExpression"></param>
    /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
    /// <param name="transaction">The transaction to use for this command.</param>
    /// <returns>Whether the command is executed successfully.</returns>
    Task<bool> DecrementAsync<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null) where TValue : struct;

    /// <summary>
    /// Save entity to the database by checking entity state.
    /// </summary>
    /// <typeparam name="TEntityState"></typeparam>
    /// <param name="entity"></param>
    /// <param name="returnAutoIncrementId"></param>
    /// <param name="transaction"></param>
    /// <returns></returns>
    Task<bool> SaveAsync<TEntityState>(TEntityState entity, bool returnAutoIncrementId = false, IDbTransaction transaction = null) where TEntityState : EntityStateBase, TEntity;
    /// <summary>
    /// Save entities to the database by checking entity state.
    /// </summary>
    /// <typeparam name="TEntityState"></typeparam>
    /// <param name="entities"></param>
    /// <param name="returnAutoIncrementId"></param>
    /// <param name="transaction"></param>
    /// <returns></returns>
    Task<bool> SaveAsync<TEntityState>(IEnumerable<TEntityState> entities, bool returnAutoIncrementId = false, IDbTransaction transaction = null) where TEntityState : EntityStateBase, TEntity;

    /// <summary>
    /// Query entities.
    /// </summary>
    /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
    /// <param name="fieldExpression">The fields to query. If the value is null, all fields of the entity are queried. Example: 
    /// <para>1. Single field: entity => entity.Status</para>
    /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
    /// </param>
    /// <param name="orderBy">SQL ORDER BY condition.</param>
    /// <param name="pageIndex">The current page index for paging query, the minimum value is 1.</param>
    /// <param name="pageSize">The page size for paging query.</param>
    /// <param name="master">true: master database, false: slave database.</param>
    /// <returns></returns>
    Task<IEnumerable<TEntity>> QueryAsync(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? pageIndex = null, int? pageSize = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true);
    /// <summary>
    /// Query entities.
    /// </summary>
    /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
    /// <param name="fieldExpression">The fields to query. If the value is null, all fields of the entity are queried. Example: 
    /// <para>1. Single field: entity => entity.Status</para>
    /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
    /// </param>
    /// <param name="orderBy">SQL ORDER BY condition.</param>
    /// <param name="offset">Offset to use for this query.</param>
    /// <param name="rows">The number of rows queried.</param>
    /// <param name="master">true: master database, false: slave database.</param>
    /// <returns></returns>
    Task<IEnumerable<TEntity>> QueryOffsetAsync(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? offset = null, int? rows = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true);

    /// <summary>
    /// Query single entity.
    /// </summary>
    /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
    /// <param name="fieldExpression">The fields to query. If the value is null, all fields of the entity are queried. Example: 
    /// <para>1. Single field: entity => entity.Status</para>
    /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
    /// </param>
    /// <param name="master">true: master database, false: slave database.</param>
    /// <returns></returns>
    Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true);

    /// <summary>
    /// Gets the count that satisfy the <paramref name="whereExpression"/>.
    /// </summary>
    /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
    /// <param name="master">true: master database, false: slave database.</param>
    /// <returns></returns>
    Task<int> CountAsync(Expression<Func<TEntity, bool>> whereExpression, bool master = true);

    /// <summary>
    /// Whether the data exists that satisfy the <paramref name="whereExpression"/>.
    /// </summary>
    /// <param name="whereExpression"></param>
    /// <param name="master"></param>
    /// <returns></returns>
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> whereExpression, bool master = true);

    /// <summary>
    /// Whether the table exists.
    /// </summary>
    /// <param name="master"></param>
    /// <param name="useCache"></param>
    /// <returns></returns>
    Task<bool> IsTableExistsAsync(bool master = true, bool useCache = true);

    /// <summary>
    /// Whether the table field exists.
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <param name="master"></param>
    /// <param name="useCache"></param>
    /// <returns></returns>
    Task<bool> IsTableFieldExistsAsync(Expression<Func<TEntity, object>> fieldExpression, bool master = true, bool useCache = true);

    /// <summary>
    /// Add table field if it does not exist.
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <param name="fieldType"></param>
    /// <param name="master"></param>
    /// <returns></returns>
    Task AddTableFieldAsync(Expression<Func<TEntity, object>> fieldExpression, string fieldType, bool master = true);
#endif
    #endregion
}