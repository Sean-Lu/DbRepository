using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using System.Transactions;
using Sean.Core.DbRepository.CodeFirst;
using Sean.Core.DbRepository.Extensions;
using Sean.Core.DbRepository.Util;
using Sean.Utility.Extensions;

#if NETSTANDARD || NET5_0_OR_GREATER
using Microsoft.Extensions.Configuration;
#endif

#if NETFRAMEWORK
using System.Data.Odbc;
using System.Data.OleDb;
#endif

namespace Sean.Core.DbRepository;

public abstract class BaseRepository : IBaseRepository
{
    public DbFactory Factory { get; }

    public DatabaseType DbType
    {
        get => Factory.DbType;
        set => Factory.DbType = value;
    }

    protected ISqlMonitor SqlMonitor => Factory.SqlMonitor;

    /// <summary>
    /// The limit on the number of entities when executing database bulk operations.
    /// <para>批量实体数据限制</para>
    /// </summary>
    protected int? BulkEntityCount { get; set; }

    /// <summary>
    /// Number of seconds before command execution timeout.
    /// <para>命令执行超时时间（单位：秒）</para>
    /// </summary>
    protected int? CommandTimeout
    {
        get => Factory.CommandTimeout;
        set => Factory.CommandTimeout = value;
    }

    #region Constructors
#if NETSTANDARD || NET5_0_OR_GREATER
    /// <summary>
    /// Single or clustered database.
    /// </summary>
    /// <param name="configuration">Configuration</param>
    /// <param name="configName">Configuration ConnectionStrings name</param>
    protected BaseRepository(IConfiguration configuration, string configName = Constants.Master)
    {
        Factory = new DbFactory(configuration, configName);
        Factory.SqlMonitor.SqlExecuting += OnSqlExecuting;
        Factory.SqlMonitor.SqlExecuted += OnSqlExecuted;
    }
#else
    /// <summary>
    /// Single or clustered database.
    /// </summary>
    /// <param name="configName">Configuration ConnectionStrings name</param>
    protected BaseRepository(string configName = Constants.Master)
    {
        Factory = new DbFactory(configName);
        Factory.SqlMonitor.SqlExecuting += OnSqlExecuting;
        Factory.SqlMonitor.SqlExecuted += OnSqlExecuted;
    }
#endif
    /// <summary>
    /// Single or clustered database.
    /// </summary>
    /// <param name="connectionSettings"></param>
    protected BaseRepository(MultiConnectionSettings connectionSettings)
    {
        Factory = new DbFactory(connectionSettings);
        Factory.SqlMonitor.SqlExecuting += OnSqlExecuting;
        Factory.SqlMonitor.SqlExecuted += OnSqlExecuted;
    }
    /// <summary>
    /// Single database.
    /// </summary>
    /// <param name="connString"></param>
    /// <param name="type"></param>
    protected BaseRepository(string connString, DatabaseType type) : this(new MultiConnectionSettings(ConnectionStringOptions.Create(connString, type)))
    {

    }
    /// <summary>
    /// Single database.
    /// </summary>
    /// <param name="connString"></param>
    /// <param name="factory"></param>
    protected BaseRepository(string connString, DbProviderFactory factory) : this(new MultiConnectionSettings(ConnectionStringOptions.Create(connString, factory)))
    {

    }
    /// <summary>
    /// Single database.
    /// </summary>
    /// <param name="connString"></param>
    /// <param name="providerName"></param>
    protected BaseRepository(string connString, string providerName) : this(new MultiConnectionSettings(ConnectionStringOptions.Create(connString, providerName)))
    {

    }
    #endregion

    public virtual string TableName()
    {
        return null;
    }

    /// <summary>
    /// Gets the SQL of database table creation.
    /// <para>获取建表SQL语句</para>
    /// </summary>
    /// <param name="tableName">The table name.</param>
    /// <returns></returns>
    protected virtual IEnumerable<string> GetCreateTableSql(string tableName)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// The database table is automatically created if it does not exist. The <see cref="GetCreateTableSql"/> method needs to be implemented.
    /// <para>自动创建表（如果表不存在），必须要实现<see cref="GetCreateTableSql"/>方法。</para>
    /// </summary>
    /// <param name="tableName">The table name.</param>
    protected virtual void AutoCreateTable(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(tableName));

        if (!IsTableExists(tableName, master: true, useCache: true))// Using master database.
        {
            var sqlList = GetCreateTableSql(tableName);
            if (sqlList != null && sqlList.Any())
            {
#if NET45
                using (var tranScope = new TransactionScope(TransactionScopeOption.Suppress))
#else
                using (var tranScope = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
#endif
                {
                    foreach (var sql in sqlList)
                    {
                        Execute(sql, master: true);
                    }
                }
            }
        }
    }

    protected virtual void OnSqlExecuting(SqlExecutingContext context)
    {

    }

    protected virtual void OnSqlExecuted(SqlExecutedContext context)
    {

    }

    /// <summary>
    /// Create and open a new connection
    /// </summary>
    /// <param name="master">true: master database, false: slave database.</param>
    /// <returns></returns>
    protected virtual DbConnection OpenNewConnection(bool master = true)
    {
        return Factory.OpenNewConnection(master);
    }

    #region Synchronous method
    public virtual int Execute(string sql, object param = null, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

        return Execute(new DefaultSqlCommand(DbType)
        {
            Sql = sql,
            Parameter = param,
            Master = master,
            Transaction = transaction,
            Connection = connection,
            CommandTimeout = CommandTimeout
        });
    }
    public virtual int Execute(ISqlCommand sqlCommand)
    {
        if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

        var result = Factory.ExecuteNonQuery(sqlCommand);
        if (DbType == DatabaseType.ClickHouse && result < 1)
        {
            return 1;// ClickHouse: Asynchronous execution
        }
        return result;
    }

    public virtual IEnumerable<T> Query<T>(string sql, object param = null, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

        return Query<T>(new DefaultSqlCommand(DbType)
        {
            Sql = sql,
            Parameter = param,
            Master = master,
            Transaction = transaction,
            Connection = connection,
            CommandTimeout = CommandTimeout
        });
    }
    public virtual IEnumerable<T> Query<T>(ISqlCommand sqlCommand)
    {
        if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

        return Factory.Query<T>(sqlCommand);
    }

    public virtual T Get<T>(string sql, object param = null, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

        return Get<T>(new DefaultSqlCommand(DbType)
        {
            Sql = sql,
            Parameter = param,
            Master = master,
            Transaction = transaction,
            Connection = connection,
            CommandTimeout = CommandTimeout
        });
    }
    public virtual T Get<T>(ISqlCommand sqlCommand)
    {
        if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

        return Factory.Get<T>(sqlCommand);
    }

    public virtual T ExecuteScalar<T>(string sql, object param = null, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

        return ExecuteScalar<T>(new DefaultSqlCommand(DbType)
        {
            Sql = sql,
            Parameter = param,
            Master = master,
            Transaction = transaction,
            Connection = connection,
            CommandTimeout = CommandTimeout
        });
    }
    public virtual T ExecuteScalar<T>(ISqlCommand sqlCommand)
    {
        if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

        return Factory.ExecuteScalar<T>(sqlCommand);
    }

    public virtual object ExecuteScalar(string sql, object param = null, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

        return ExecuteScalar(new DefaultSqlCommand(DbType)
        {
            Sql = sql,
            Parameter = param,
            Master = master,
            Transaction = transaction,
            Connection = connection,
            CommandTimeout = CommandTimeout
        });
    }
    public virtual object ExecuteScalar(ISqlCommand sqlCommand)
    {
        if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

        return Factory.ExecuteScalar(sqlCommand);
    }

    public virtual DataTable ExecuteDataTable(string sql, object param = null, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

        return ExecuteDataTable(new DefaultSqlCommand(DbType)
        {
            Sql = sql,
            Parameter = param,
            Master = master,
            Transaction = transaction,
            Connection = connection,
            CommandTimeout = CommandTimeout
        });
    }
    public virtual DataTable ExecuteDataTable(ISqlCommand sqlCommand)
    {
        if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

        return Factory.ExecuteDataTable(sqlCommand);
    }

    public virtual DataSet ExecuteDataSet(string sql, object param = null, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

        return ExecuteDataSet(new DefaultSqlCommand(DbType)
        {
            Sql = sql,
            Parameter = param,
            Master = master,
            Transaction = transaction,
            Connection = connection,
            CommandTimeout = CommandTimeout
        });
    }
    public virtual DataSet ExecuteDataSet(ISqlCommand sqlCommand)
    {
        if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

        return Factory.ExecuteDataSet(sqlCommand);
    }

    public virtual IDataReader ExecuteReader(string sql, object param = null, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

        return ExecuteReader(new DefaultSqlCommand(DbType)
        {
            Sql = sql,
            Parameter = param,
            Master = master,
            Transaction = transaction,
            Connection = connection,
            CommandTimeout = CommandTimeout
        });
    }
    public virtual IDataReader ExecuteReader(ISqlCommand sqlCommand)
    {
        if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

        return Factory.ExecuteReader(sqlCommand);
    }

    public virtual T Execute<T>(Func<IDbConnection, T> func, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null)
    {
        if (func == null) throw new ArgumentNullException(nameof(func));

        if (transaction?.Connection != null)
        {
            return func(transaction.Connection);
        }

        if (connection != null)
        {
            return func(connection);
        }

        using (connection = OpenNewConnection(master))
        {
            return func(connection);
        }
    }

    public virtual T ExecuteTransaction<T>(Func<IDbTransaction, T> func, IDbTransaction transaction = null, IDbConnection connection = null)
    {
        if (func == null) throw new ArgumentNullException(nameof(func));

        if (transaction != null)
        {
            return SynchronousWriteUtil.UseDatabaseLock(transaction.Connection, () => func(transaction), transaction);
        }

        return Execute(conn =>
        {
            using (var trans = conn.BeginTransaction())
            {
                return SynchronousWriteUtil.UseDatabaseLock(conn, () =>
                {
                    try
                    {
                        return func(trans);
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }, trans);
            }
        }, true, connection: connection);
    }
    public virtual bool ExecuteAutoTransaction(Func<IDbTransaction, bool> func, IDbTransaction transaction = null, IDbConnection connection = null)
    {
        if (func == null) throw new ArgumentNullException(nameof(func));

        if (transaction != null)
        {
            return SynchronousWriteUtil.UseDatabaseLock(transaction.Connection, () => func(transaction), transaction);
        }

        return Execute(conn =>
        {
            using (var trans = conn.BeginTransaction())
            {
                return SynchronousWriteUtil.UseDatabaseLock(conn, () =>
                {
                    try
                    {
                        if (!func(trans))
                        {
                            trans.Rollback();
                            return false;
                        }

                        trans.Commit();
                        return true;
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }, trans);
            }
        }, true, connection: connection);
    }

    public virtual bool IsTableExists(string tableName, bool master = true, bool useCache = true)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            return false;
        }

        string connectionString = Factory.ConnectionSettings.GetConnectionString(master);
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
        using (var connection = Factory.OpenNewConnection(connectionString))
        {
            exists = DbContextConfiguration.Options.IsTableExists?.Invoke(DbType, connection, tableName) ??
                     connection switch
                     {
#if NETFRAMEWORK
                         OleDbConnection oleDbConnection => oleDbConnection.IsTableExists(tableName),
                         OdbcConnection odbcConnection => odbcConnection.IsTableExists(tableName),
#endif
                         _ => null
                     };

            if (!exists.HasValue)
            {
                var sql = DbType.GetSqlForTableExists(connection, tableName);
                exists = ExecuteScalar<int>(sql, connection: connection) > 0;
            }
        }

        if (useCache && exists.GetValueOrDefault())
        {
            TableInfoCache.AddTable(connectionString, master, tableName);
        }
        return exists.GetValueOrDefault();
    }

    public virtual bool IsTableFieldExists(string tableName, string fieldName, bool master = true, bool useCache = true)
    {
        if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(fieldName))
        {
            return false;
        }

        string connectionString = Factory.ConnectionSettings.GetConnectionString(master);
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
        using (var connection = Factory.OpenNewConnection(connectionString))
        {
            exists = DbContextConfiguration.Options.IsTableFieldExists?.Invoke(DbType, connection, tableName, fieldName) ??
                     connection switch
                     {
#if NETFRAMEWORK
                         OleDbConnection oleDbConnection => oleDbConnection.IsTableFieldExists(tableName, fieldName),
                         OdbcConnection odbcConnection => odbcConnection.IsTableFieldExists(tableName, fieldName),
#endif
                         _ => null
                     };

            if (!exists.HasValue)
            {
                var sql = DbType.GetSqlForTableFieldExists(connection, tableName, fieldName);
                exists = ExecuteScalar<int>(sql, connection: connection) > 0;
            }
        }

        if (useCache && exists.GetValueOrDefault())
        {
            TableInfoCache.AddTableField(connectionString, master, tableName, fieldName);
        }
        return exists.GetValueOrDefault();
    }

    public virtual void AddTableField(string tableName, string fieldName, string fieldType, bool master = true)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(tableName));
        if (string.IsNullOrWhiteSpace(fieldName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(fieldName));

        if (IsTableFieldExists(tableName, fieldName, master))
        {
            return;
        }

        Execute($"ALTER TABLE {DbType.MarkAsTableOrFieldName(tableName)} ADD {DbType.MarkAsTableOrFieldName(fieldName)} {fieldType}", master: master);
    }

    public virtual int DeleteAll(string tableName, IDbTransaction transaction = null)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(tableName));

        return Execute($"DELETE FROM {DbType.MarkAsTableOrFieldName(tableName)}", transaction: transaction);
    }

    public virtual int DropTable(params string[] tableNames)
    {
        return Execute($"DROP TABLE {string.Join(", ", tableNames.Select(tableName => DbType.MarkAsTableOrFieldName(tableName)).ToList())}");
    }
    #endregion

    #region Asynchronous method
    public virtual async Task<int> ExecuteAsync(string sql, object param = null, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

        return await ExecuteAsync(new DefaultSqlCommand(DbType)
        {
            Sql = sql,
            Parameter = param,
            Master = master,
            Transaction = transaction,
            Connection = connection,
            CommandTimeout = CommandTimeout
        });
    }
    public virtual async Task<int> ExecuteAsync(ISqlCommand sqlCommand)
    {
        if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

        var result = await Factory.ExecuteNonQueryAsync(sqlCommand);
        if (DbType == DatabaseType.ClickHouse && result < 1)
        {
            return 1;// ClickHouse: Asynchronous execution
        }
        return result;
    }

    public virtual async Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

        return await QueryAsync<T>(new DefaultSqlCommand(DbType)
        {
            Sql = sql,
            Parameter = param,
            Master = master,
            Transaction = transaction,
            Connection = connection,
            CommandTimeout = CommandTimeout
        });
    }
    public virtual async Task<IEnumerable<T>> QueryAsync<T>(ISqlCommand sqlCommand)
    {
        if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

        return await Factory.QueryAsync<T>(sqlCommand);
    }

    public virtual async Task<T> GetAsync<T>(string sql, object param = null, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

        return await GetAsync<T>(new DefaultSqlCommand(DbType)
        {
            Sql = sql,
            Parameter = param,
            Master = master,
            Transaction = transaction,
            Connection = connection,
            CommandTimeout = CommandTimeout
        });
    }
    public virtual async Task<T> GetAsync<T>(ISqlCommand sqlCommand)
    {
        if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

        return await Factory.GetAsync<T>(sqlCommand);
    }

    public virtual async Task<T> ExecuteScalarAsync<T>(string sql, object param = null, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

        return await ExecuteScalarAsync<T>(new DefaultSqlCommand(DbType)
        {
            Sql = sql,
            Parameter = param,
            Master = master,
            Transaction = transaction,
            Connection = connection,
            CommandTimeout = CommandTimeout
        });
    }
    public virtual async Task<T> ExecuteScalarAsync<T>(ISqlCommand sqlCommand)
    {
        if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

        return await Factory.ExecuteScalarAsync<T>(sqlCommand);
    }

    public virtual async Task<object> ExecuteScalarAsync(string sql, object param = null, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

        return await ExecuteScalarAsync(new DefaultSqlCommand(DbType)
        {
            Sql = sql,
            Parameter = param,
            Master = master,
            Transaction = transaction,
            Connection = connection,
            CommandTimeout = CommandTimeout
        });
    }
    public virtual async Task<object> ExecuteScalarAsync(ISqlCommand sqlCommand)
    {
        if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

        return await Factory.ExecuteScalarAsync(sqlCommand);
    }

    public virtual async Task<DataTable> ExecuteDataTableAsync(string sql, object param = null, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

        return await ExecuteDataTableAsync(new DefaultSqlCommand(DbType)
        {
            Sql = sql,
            Parameter = param,
            Master = master,
            Transaction = transaction,
            Connection = connection,
            CommandTimeout = CommandTimeout
        });
    }
    public virtual async Task<DataTable> ExecuteDataTableAsync(ISqlCommand sqlCommand)
    {
        if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

        return await Factory.ExecuteDataTableAsync(sqlCommand);
    }

    public virtual async Task<DataSet> ExecuteDataSetAsync(string sql, object param = null, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

        return await ExecuteDataSetAsync(new DefaultSqlCommand(DbType)
        {
            Sql = sql,
            Parameter = param,
            Master = master,
            Transaction = transaction,
            Connection = connection,
            CommandTimeout = CommandTimeout
        });
    }
    public virtual async Task<DataSet> ExecuteDataSetAsync(ISqlCommand sqlCommand)
    {
        if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

        return await Factory.ExecuteDataSetAsync(sqlCommand);
    }

    public virtual async Task<IDataReader> ExecuteReaderAsync(string sql, object param = null, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

        return await ExecuteReaderAsync(new DefaultSqlCommand(DbType)
        {
            Sql = sql,
            Parameter = param,
            Master = master,
            Transaction = transaction,
            Connection = connection,
            CommandTimeout = CommandTimeout
        });
    }
    public virtual async Task<IDataReader> ExecuteReaderAsync(ISqlCommand sqlCommand)
    {
        if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

        return await Factory.ExecuteReaderAsync(sqlCommand);
    }

    public virtual async Task<T> ExecuteAsync<T>(Func<IDbConnection, Task<T>> func, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null)
    {
        if (func == null) throw new ArgumentNullException(nameof(func));

        if (transaction?.Connection != null)
        {
            return await func(transaction.Connection);
        }

        if (connection != null)
        {
            return await func(connection);
        }

        using (connection = OpenNewConnection(master))
        {
            return await func(connection);
        }
    }

    public virtual async Task<T> ExecuteTransactionAsync<T>(Func<IDbTransaction, Task<T>> func, IDbTransaction transaction = null, IDbConnection connection = null)
    {
        if (func == null) throw new ArgumentNullException(nameof(func));

        if (transaction != null)
        {
            return await SynchronousWriteUtil.UseDatabaseLockAsync(transaction.Connection, async () => await func(transaction), transaction);
        }

        return await ExecuteAsync(async conn =>
        {
            using (var trans = conn.BeginTransaction())
            {
                return await SynchronousWriteUtil.UseDatabaseLockAsync(conn, async () =>
                {
                    try
                    {
                        return await func(trans);
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }, trans);
            }
        }, true, connection: connection);
    }
    public virtual async Task<bool> ExecuteAutoTransactionAsync(Func<IDbTransaction, Task<bool>> func, IDbTransaction transaction = null, IDbConnection connection = null)
    {
        if (func == null) throw new ArgumentNullException(nameof(func));

        if (transaction != null)
        {
            return await SynchronousWriteUtil.UseDatabaseLockAsync(transaction.Connection, async () => await func(transaction), transaction);
        }

        return await ExecuteAsync(async conn =>
        {
            using (var trans = conn.BeginTransaction())
            {
                return await SynchronousWriteUtil.UseDatabaseLockAsync(conn, async () =>
                {
                    try
                    {
                        if (!await func(trans))
                        {
                            trans.Rollback();
                            return false;
                        }

                        trans.Commit();
                        return true;
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }, trans);
            }
        }, true, connection: connection);
    }

    public virtual async Task<bool> IsTableExistsAsync(string tableName, bool master = true, bool useCache = true)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            return false;
        }

        string connectionString = Factory.ConnectionSettings.GetConnectionString(master);
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
        using (var connection = Factory.OpenNewConnection(connectionString))
        {
            exists = DbContextConfiguration.Options.IsTableExists?.Invoke(DbType, connection, tableName) ??
                     connection switch
                     {
#if NETFRAMEWORK
                         OleDbConnection oleDbConnection => oleDbConnection.IsTableExists(tableName),
                         OdbcConnection odbcConnection => odbcConnection.IsTableExists(tableName),
#endif
                         _ => null
                     };

            if (!exists.HasValue)
            {
                var sql = DbType.GetSqlForTableExists(connection, tableName);
                exists = await ExecuteScalarAsync<int>(sql, connection: connection) > 0;
            }
        }

        if (useCache && exists.GetValueOrDefault())
        {
            TableInfoCache.AddTable(connectionString, master, tableName);
        }
        return exists.GetValueOrDefault();
    }

    public virtual async Task<bool> IsTableFieldExistsAsync(string tableName, string fieldName, bool master = true, bool useCache = true)
    {
        if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(fieldName))
        {
            return false;
        }

        string connectionString = Factory.ConnectionSettings.GetConnectionString(master);
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
        using (var connection = Factory.OpenNewConnection(connectionString))
        {
            exists = DbContextConfiguration.Options.IsTableFieldExists?.Invoke(DbType, connection, tableName, fieldName) ??
                     connection switch
                     {
#if NETFRAMEWORK
                         OleDbConnection oleDbConnection => oleDbConnection.IsTableFieldExists(tableName, fieldName),
                         OdbcConnection odbcConnection => odbcConnection.IsTableFieldExists(tableName, fieldName),
#endif
                         _ => null
                     };

            if (!exists.HasValue)
            {
                var sql = DbType.GetSqlForTableFieldExists(connection, tableName, fieldName);
                exists = await ExecuteScalarAsync<int>(sql, connection: connection) > 0;
            }
        }

        if (useCache && exists.GetValueOrDefault())
        {
            TableInfoCache.AddTableField(connectionString, master, tableName, fieldName);
        }
        return exists.GetValueOrDefault();
    }

    public virtual async Task AddTableFieldAsync(string tableName, string fieldName, string fieldType, bool master = true)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(tableName));
        if (string.IsNullOrWhiteSpace(fieldName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(fieldName));

        if (await IsTableFieldExistsAsync(tableName, fieldName, master))
        {
            return;
        }

        await ExecuteAsync($"ALTER TABLE {DbType.MarkAsTableOrFieldName(tableName)} ADD {DbType.MarkAsTableOrFieldName(fieldName)} {fieldType}", master: master);
    }

    public virtual async Task<int> DeleteAllAsync(string tableName, IDbTransaction transaction = null)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(tableName));

        return await ExecuteAsync($"DELETE FROM {DbType.MarkAsTableOrFieldName(tableName)}", transaction: transaction);
    }

    public virtual async Task<int> DropTableAsync(params string[] tableNames)
    {
        return await ExecuteAsync($"DROP TABLE {string.Join(", ", tableNames.Select(tableName => DbType.MarkAsTableOrFieldName(tableName)).ToList())}");
    }
    #endregion
}

public abstract class BaseRepository<TEntity> : BaseRepository, IBaseRepository<TEntity> where TEntity : class
{
    #region Constructors
#if NETSTANDARD || NET5_0_OR_GREATER
    /// <summary>
    /// Single or clustered database.
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="configName">Configuration ConnectionStrings name</param>
    protected BaseRepository(IConfiguration configuration, string configName = Constants.Master) : base(configuration, configName)
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
    protected BaseRepository(string connString, DatabaseType type) : base(connString, type)
    {

    }
    /// <summary>
    /// Single database.
    /// </summary>
    /// <param name="connString"></param>
    /// <param name="factory"></param>
    protected BaseRepository(string connString, DbProviderFactory factory) : base(connString, factory)
    {

    }
    /// <summary>
    /// Single database.
    /// </summary>
    /// <param name="connString"></param>
    /// <param name="providerName"></param>
    protected BaseRepository(string connString, string providerName) : base(connString, providerName)
    {

    }
    #endregion

    public override string TableName()
    {
        return typeof(TEntity).GetEntityInfo().TableName;
    }

    protected override IEnumerable<string> GetCreateTableSql(string tableName)
    {
        ISqlGenerator sqlGenerator = SqlGeneratorFactory.GetSqlGenerator(DbType);
        return sqlGenerator?.GetCreateTableSql<TEntity>(false, _ => tableName);
    }

    #region Synchronous method
    public virtual bool Add(TEntity entity, bool returnAutoIncrementId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
    {
        if (entity == null) return false;

        PropertyInfo keyIdentityProperty;
        if (returnAutoIncrementId && (keyIdentityProperty = typeof(TEntity).GetEntityInfo().FieldInfos.FirstOrDefault(c => c.IsPrimaryKey && c.IsIdentityField)?.Property) != null)
        {
            switch (DbType)
            {
                case DatabaseType.MsAccess:
                case DatabaseType.Informix:
                case DatabaseType.ShenTong:
                case DatabaseType.Xugu:
                    {
                        return Execute(connection =>
                        {
                            var sqlCommandInsert = this.CreateInsertableBuilder()
                                .InsertFields(fieldExpression)
                                .SetParameter(entity)
                                .Build();
                            sqlCommandInsert.Connection = connection;
                            sqlCommandInsert.Transaction = transaction;
                            sqlCommandInsert.CommandTimeout = CommandTimeout;
                            var addResult = Execute(sqlCommandInsert) > 0;
                            if (!addResult)
                            {
                                return false;
                            }

                            string returnIdSql = null;
                            switch (DbType)
                            {
                                case DatabaseType.MsAccess:
                                    {
                                        returnIdSql = "SELECT @@IDENTITY AS Id";
                                        break;
                                    }
                                case DatabaseType.Informix:
                                    {
                                        returnIdSql = $"SELECT dbinfo('sqlca.sqlerrd1') AS Id FROM systables WHERE tabname='{TableName()}' AND tabtype='T'";
                                        break;
                                    }
                                case DatabaseType.ShenTong:
                                    {
                                        returnIdSql = "SELECT LAST_INSERT_ID() AS Id";
                                        break;
                                    }
                                case DatabaseType.Xugu:
                                    {
                                        returnIdSql = $"SELECT MAX({DbType.MarkAsTableOrFieldName(keyIdentityProperty.GetFieldName(typeof(TEntity).GetEntityInfo().NamingConvention))}) FROM {DbType.MarkAsTableOrFieldName(TableName())}";
                                        break;
                                    }
                            }

                            var sqlCommandReturnId = new DefaultSqlCommand(DbType)
                            {
                                Sql = returnIdSql,
                                Connection = connection,
                                Transaction = transaction,
                                CommandTimeout = CommandTimeout
                            };
                            var id = ExecuteScalar<long>(sqlCommandReturnId);
                            if (id < 1)
                            {
                                return false;
                            }

                            keyIdentityProperty.SetValue(entity, id, null);
                            return true;
                        }, true, transaction);
                    }
                case DatabaseType.Oracle:
                    {
                        var sqlCommandReturnId = this.CreateInsertableBuilder()
                            .InsertFields(fieldExpression)
                            .ReturnAutoIncrementId()
                            .OutputParameter(entity, keyIdentityProperty)
                            .SetParameter(entity)
                            .Build();
                        sqlCommandReturnId.Master = true;
                        sqlCommandReturnId.Transaction = transaction;
                        sqlCommandReturnId.CommandTimeout = CommandTimeout;
                        return Execute(sqlCommandReturnId) > 0;
                    }
                default:
                    {
                        var sqlCommandReturnId = this.CreateInsertableBuilder()
                            .InsertFields(fieldExpression)
                            .ReturnAutoIncrementId()
                            .SetParameter(entity)
                            .Build();
                        sqlCommandReturnId.Master = true;
                        sqlCommandReturnId.Transaction = transaction;
                        sqlCommandReturnId.CommandTimeout = CommandTimeout;
                        var id = ExecuteScalar<long>(sqlCommandReturnId);
                        if (id < 1)
                        {
                            return false;
                        }

                        keyIdentityProperty.SetValue(entity, id, null);
                        return true;
                    }
            }
        }

        var sqlCommand = this.CreateInsertableBuilder()
            .InsertFields(fieldExpression)
            .SetParameter(entity)
            .Build();
        sqlCommand.Master = true;
        sqlCommand.Transaction = transaction;
        sqlCommand.CommandTimeout = CommandTimeout;
        return Execute(sqlCommand) > 0;
    }
    public virtual bool Add(IEnumerable<TEntity> entities, bool returnAutoIncrementId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
    {
        if (entities == null || !entities.Any()) return false;

        if (returnAutoIncrementId && typeof(TEntity).GetEntityInfo().FieldInfos.FirstOrDefault(c => c.IsPrimaryKey && c.IsIdentityField)?.Property != null)
        {
            //if (transaction?.Connection == null)
            //{
            //    return ExecuteAutoTransaction(trans =>
            //    {
            //        foreach (var entity in entities)
            //        {
            //            if (!Add(entity, returnAutoIncrementId, fieldExpression, trans))
            //            {
            //                return false;
            //            }
            //        }
            //        return true;
            //    });
            //}

            foreach (var entity in entities)
            {
                if (!Add(entity, returnAutoIncrementId, fieldExpression, transaction))
                {
                    return false;
                }
            }

            return true;
        }

        var bulkCountLimit = BulkEntityCount ?? DbContextConfiguration.Options.BulkEntityCount;
        if (bulkCountLimit.HasValue && entities.Count() > bulkCountLimit.Value)
        {
            return entities.PagingExecute(bulkCountLimit.Value, (pageNumber, models) =>
            {
                var sqlCommand = this.CreateInsertableBuilder()
                    .InsertFields(fieldExpression)
                    .SetParameter(models)
                    .Build();
                sqlCommand.Master = true;
                sqlCommand.Transaction = transaction;
                sqlCommand.CommandTimeout = CommandTimeout;
                return Execute(sqlCommand) > 0;
            });
        }

        var sqlCommand = this.CreateInsertableBuilder()
            .InsertFields(fieldExpression)
            .SetParameter(entities)
            .Build();
        sqlCommand.Master = true;
        sqlCommand.Transaction = transaction;
        sqlCommand.CommandTimeout = CommandTimeout;
        return Execute(sqlCommand) > 0;
    }

    public virtual bool AddOrUpdate(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
    {
        if (entity == null) return false;

        switch (DbType)
        {
            case DatabaseType.MySql:
            case DatabaseType.MariaDB:
            case DatabaseType.TiDB:
            case DatabaseType.OceanBase:
            case DatabaseType.SQLite:
                {
                    var sqlCommand = this.CreateReplaceableBuilder()
                        .InsertFields(fieldExpression)
                        .SetParameter(entity)
                        .Build();
                    sqlCommand.Master = true;
                    sqlCommand.Transaction = transaction;
                    sqlCommand.CommandTimeout = CommandTimeout;
                    return Execute(sqlCommand) > 0;
                }
            default:
                {
                    var pkFields = typeof(TEntity).GetEntityInfo().FieldInfos.Where(c => c.IsPrimaryKey).Select(c => c.FieldName).ToList();
                    if (pkFields == null || !pkFields.Any()) throw new Exception($"The entity class '{typeof(TEntity).Name}' does not define a primary key field.");

                    ICountable<TEntity> countableBuilder = this.CreateCountableBuilder();
                    pkFields.ForEach(pkField => countableBuilder.WhereField(entity1 => pkField, SqlOperation.Equal));
                    countableBuilder.SetParameter(entity);
                    ISqlCommand sqlCommand = countableBuilder.Build();
                    sqlCommand.Master = true;
                    sqlCommand.CommandTimeout = CommandTimeout;
                    if (ExecuteScalar<int>(sqlCommand) < 1)
                    {
                        // INSERT
                        return Add(entity, false, fieldExpression, transaction);
                    }

                    //if (transaction?.Connection == null)
                    //{
                    //    return ExecuteAutoTransaction(trans =>
                    //    {
                    //        // DELETE && INSERT
                    //        return Delete(entity, trans) && Add(entity, false, fieldExpression, trans);
                    //    });
                    //}

                    // DELETE && INSERT
                    return Delete(entity, transaction) && Add(entity, false, fieldExpression, transaction);
                }
        }
    }
    public virtual bool AddOrUpdate(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
    {
        if (entities == null || !entities.Any()) return false;

        switch (DbType)
        {
            case DatabaseType.MySql:
            case DatabaseType.MariaDB:
            case DatabaseType.TiDB:
            case DatabaseType.OceanBase:
            case DatabaseType.SQLite:
                {
                    var bulkCountLimit = BulkEntityCount ?? DbContextConfiguration.Options.BulkEntityCount;
                    if (bulkCountLimit.HasValue && entities.Count() > bulkCountLimit.Value)
                    {
                        return entities.PagingExecute(bulkCountLimit.Value, (pageNumber, models) =>
                        {
                            var sqlCommand = this.CreateReplaceableBuilder()
                                .InsertFields(fieldExpression)
                                .SetParameter(models)
                                .Build();
                            sqlCommand.Master = true;
                            sqlCommand.Transaction = transaction;
                            sqlCommand.CommandTimeout = CommandTimeout;
                            return Execute(sqlCommand) > 0;
                        });
                    }

                    var sqlCommand = this.CreateReplaceableBuilder()
                        .InsertFields(fieldExpression)
                        .SetParameter(entities)
                        .Build();
                    sqlCommand.Master = true;
                    sqlCommand.Transaction = transaction;
                    sqlCommand.CommandTimeout = CommandTimeout;
                    return Execute(sqlCommand) > 0;
                }
            default:
                {
                    //if (transaction?.Connection == null)
                    //{
                    //    return ExecuteAutoTransaction(trans =>
                    //    {
                    //        foreach (var entity in entities)
                    //        {
                    //            if (!AddOrUpdate(entity, fieldExpression, trans))
                    //            {
                    //                return false;
                    //            }
                    //        }
                    //        return true;
                    //    });
                    //}

                    foreach (var entity in entities)
                    {
                        if (!AddOrUpdate(entity, fieldExpression, transaction))
                        {
                            return false;
                        }
                    }

                    return true;
                }
        }
    }

    public virtual bool Delete(TEntity entity, IDbTransaction transaction = null)
    {
        var sqlCommand = this.CreateDeleteableBuilder()
            .SetParameter(entity)
            .Build();
        sqlCommand.Master = true;
        sqlCommand.Transaction = transaction;
        sqlCommand.CommandTimeout = CommandTimeout;
        return Execute(sqlCommand) > 0;
    }
    public virtual bool Delete(IEnumerable<TEntity> entities, IDbTransaction transaction = null)
    {
        if (entities == null || !entities.Any()) return false;

        foreach (var entity in entities)
        {
            if (!Delete(entity, transaction))
            {
                return false;
            }
        }

        return true;
    }
    public virtual int Delete(Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null)
    {
        var sqlCommand = this.CreateDeleteableBuilder()
            .Where(whereExpression)
            .Build();
        sqlCommand.Master = true;
        sqlCommand.Transaction = transaction;
        sqlCommand.CommandTimeout = CommandTimeout;
        return Execute(sqlCommand);
    }
    public virtual int DeleteAll(IDbTransaction transaction = null)
    {
        return DeleteAll(TableName(), transaction);
    }

    public virtual int Update(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, Expression<Func<TEntity, bool>> whereExpression = null, IDbTransaction transaction = null)
    {
        var sqlCommand = this.CreateUpdateableBuilder()
            .UpdateFields(fieldExpression)
            .Where(whereExpression)
            .SetParameter(entity)
            .Build();
        sqlCommand.Master = true;
        sqlCommand.Transaction = transaction;
        sqlCommand.CommandTimeout = CommandTimeout;
        return Execute(sqlCommand);
    }
    public virtual bool Update(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
    {
        if (entities == null || !entities.Any()) return false;

        //if (transaction?.Connection == null)
        //{
        //    return ExecuteAutoTransaction(trans =>
        //    {
        //        foreach (var entity in entities)
        //        {
        //            if (!(Update(entity, fieldExpression, null, trans) > 0))
        //            {
        //                return false;
        //            }
        //        }
        //        return true;
        //    });
        //}

        foreach (var entity in entities)
        {
            if (!(Update(entity, fieldExpression, null, transaction) > 0))
            {
                return false;
            }
        }

        return true;
    }

    public virtual bool Increment<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null) where TValue : struct
    {
        var sqlCommand = this.CreateUpdateableBuilder()
            .IncrementFields(fieldExpression, value)
            .Where(whereExpression)
            .Build();
        sqlCommand.Master = true;
        sqlCommand.Transaction = transaction;
        sqlCommand.CommandTimeout = CommandTimeout;
        return Execute(sqlCommand) > 0;
    }
    public virtual bool Decrement<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null) where TValue : struct
    {
        var sqlCommand = this.CreateUpdateableBuilder()
            .DecrementFields(fieldExpression, value)
            .Where(whereExpression)
            .Build();
        sqlCommand.Master = true;
        sqlCommand.Transaction = transaction;
        sqlCommand.CommandTimeout = CommandTimeout;
        return Execute(sqlCommand) > 0;
    }

    public virtual bool Save<TEntityState>(TEntityState entity, bool returnAutoIncrementId = false, IDbTransaction transaction = null) where TEntityState : TEntity, IEntityStateBase
    {
        if (entity == null)
        {
            return false;
        }

        if (entity.EntityState == EntityStateType.Unchanged)
        {
            return true;
        }

        switch (entity.EntityState)
        {
            case EntityStateType.Added:
                if (!Add(entity, returnAutoIncrementId, transaction: transaction))
                {
                    return false;
                }
                break;
            case EntityStateType.Modified:
                if (Update(entity, transaction: transaction) < 1)
                {
                    return false;
                }
                break;
            case EntityStateType.Deleted:
                if (!Delete(entity, transaction: transaction))
                {
                    return false;
                }
                break;
            default:
                throw new NotImplementedException($"EntityState: {entity.EntityState}");
        }

        if (transaction == null)
        {
            entity.ResetEntityState();
        }

        return true;
    }
    public virtual bool Save<TEntityState>(IEnumerable<TEntityState> entities, bool returnAutoIncrementId = false, IDbTransaction transaction = null) where TEntityState : TEntity, IEntityStateBase
    {
        if (entities == null || !entities.Any())
        {
            return false;
        }

        if (entities.All(c => c.EntityState == EntityStateType.Unchanged))
        {
            return true;
        }

        var result = ExecuteAutoTransaction(trans =>
        {
            var addList = entities.Where(t => t.EntityState == EntityStateType.Added).Select(c => (TEntity)c).ToList();
            if (addList.Any())
            {
                if (!Add(addList, returnAutoIncrementId, transaction: trans))
                {
                    return false;
                }
            }

            var updateList = entities.Where(t => t.EntityState == EntityStateType.Modified).Select(c => (TEntity)c).ToList();
            if (updateList.Any())
            {
                if (!Update(updateList, transaction: trans))
                {
                    return false;
                }
            }

            var deleteList = entities.Where(t => t.EntityState == EntityStateType.Deleted).Select(c => (TEntity)c).ToList();
            if (deleteList.Any())
            {
                if (!Delete(deleteList, transaction: trans))
                {
                    return false;
                }
            }

            return true;
        }, transaction);

        if (result && transaction == null)
        {
            entities.ResetEntityState();
        }

        return result;
    }

    public virtual PageQueryResult<TEntity> PageQuery(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy, int pageNumber, int pageSize, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true)
    {
        var result = new PageQueryResult<TEntity>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Total = Count(whereExpression, master),
            List = Query(whereExpression, orderBy, pageNumber, pageSize, fieldExpression, master)?.ToList()
        };
        return result;
    }

    public virtual IEnumerable<TEntity> Query(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? pageNumber = null, int? pageSize = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true)
    {
        var sqlCommand = this.CreateQueryableBuilder()
            .SelectFields(fieldExpression)
            .Where(whereExpression)
            .OrderBy(orderBy)
            .Page(pageNumber, pageSize)
            .Build();
        sqlCommand.Master = master;
        sqlCommand.CommandTimeout = CommandTimeout;
        return Query<TEntity>(sqlCommand);
    }
    public virtual IEnumerable<TEntity> QueryOffset(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? offset = null, int? rows = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true)
    {
        var sqlCommand = this.CreateQueryableBuilder()
            .SelectFields(fieldExpression)
            .Where(whereExpression)
            .OrderBy(orderBy)
            .Offset(offset, rows)
            .Build();
        sqlCommand.Master = master;
        sqlCommand.CommandTimeout = CommandTimeout;
        return Query<TEntity>(sqlCommand);
    }

    public virtual TEntity Get(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true)
    {
        var sqlCommand = this.CreateQueryableBuilder()
            .SelectFields(fieldExpression)
            .Where(whereExpression)
            .Build();
        sqlCommand.Master = master;
        sqlCommand.CommandTimeout = CommandTimeout;
        return Get<TEntity>(sqlCommand);
    }

    public virtual int Count(Expression<Func<TEntity, bool>> whereExpression, bool master = true)
    {
        var sqlCommand = this.CreateCountableBuilder()
            .Where(whereExpression)
            .Build();
        sqlCommand.Master = master;
        sqlCommand.CommandTimeout = CommandTimeout;
        return ExecuteScalar<int>(sqlCommand);
    }

    public virtual bool Exists(Expression<Func<TEntity, bool>> whereExpression, bool master = true)
    {
        return Count(whereExpression, master) > 0;
    }

    public virtual bool IsTableExists(bool master = true, bool useCache = true)
    {
        return IsTableExists(TableName(), master, useCache);
    }

    public virtual bool IsTableFieldExists(Expression<Func<TEntity, object>> fieldExpression, bool master = true, bool useCache = true)
    {
        var fieldNames = fieldExpression.GetFieldNames();
        if (fieldNames == null || fieldNames.Count < 1)
        {
            return false;
        }

        var tableName = TableName();
        foreach (var fieldName in fieldNames)
        {
            if (!IsTableFieldExists(tableName, fieldName, master, useCache))
            {
                return false;
            }
        }
        return true;
    }

    public virtual void AddTableField(Expression<Func<TEntity, object>> fieldExpression, string fieldType, bool master = true)
    {
        var fieldNames = fieldExpression.GetFieldNames();
        if (fieldNames == null || fieldNames.Count < 1)
        {
            return;
        }

        var tableName = TableName();
        foreach (var fieldName in fieldNames)
        {
            AddTableField(tableName, fieldName, fieldType, master);
        }
    }
    #endregion

    #region Asynchronous method
    public virtual async Task<bool> AddAsync(TEntity entity, bool returnAutoIncrementId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
    {
        if (entity == null) return false;

        PropertyInfo keyIdentityProperty;
        if (returnAutoIncrementId && (keyIdentityProperty = typeof(TEntity).GetEntityInfo().FieldInfos.FirstOrDefault(c => c.IsPrimaryKey && c.IsIdentityField)?.Property) != null)
        {
            switch (DbType)
            {
                case DatabaseType.MsAccess:
                case DatabaseType.Informix:
                case DatabaseType.ShenTong:
                case DatabaseType.Xugu:
                    {
                        return await ExecuteAsync(async connection =>
                        {
                            var sqlCommandInsert = this.CreateInsertableBuilder()
                                .InsertFields(fieldExpression)
                                .SetParameter(entity)
                                .Build();
                            sqlCommandInsert.Connection = connection;
                            sqlCommandInsert.Transaction = transaction;
                            sqlCommandInsert.CommandTimeout = CommandTimeout;
                            var addResult = await ExecuteAsync(sqlCommandInsert) > 0;
                            if (!addResult)
                            {
                                return false;
                            }

                            string returnIdSql = null;
                            switch (DbType)
                            {
                                case DatabaseType.MsAccess:
                                    {
                                        returnIdSql = "SELECT @@IDENTITY AS Id";
                                        break;
                                    }
                                case DatabaseType.Informix:
                                    {
                                        returnIdSql = $"SELECT dbinfo('sqlca.sqlerrd1') AS Id FROM systables WHERE tabname='{TableName()}' AND tabtype='T'";
                                        break;
                                    }
                                case DatabaseType.ShenTong:
                                    {
                                        returnIdSql = "SELECT LAST_INSERT_ID() AS Id";
                                        break;
                                    }
                                case DatabaseType.Xugu:
                                    {
                                        returnIdSql = $"SELECT MAX({DbType.MarkAsTableOrFieldName(keyIdentityProperty.GetFieldName(typeof(TEntity).GetEntityInfo().NamingConvention))}) FROM {DbType.MarkAsTableOrFieldName(TableName())}";
                                        break;
                                    }
                            }

                            var sqlCommandReturnId = new DefaultSqlCommand(DbType)
                            {
                                Sql = returnIdSql,
                                Connection = connection,
                                Transaction = transaction,
                                CommandTimeout = CommandTimeout
                            };
                            var id = await ExecuteScalarAsync<long>(sqlCommandReturnId);
                            if (id < 1)
                            {
                                return false;
                            }

                            keyIdentityProperty.SetValue(entity, id, null);
                            return true;
                        }, true, transaction);
                    }
                case DatabaseType.Oracle:
                    {
                        var sqlCommandReturnId = this.CreateInsertableBuilder()
                            .InsertFields(fieldExpression)
                            .ReturnAutoIncrementId()
                            .OutputParameter(entity, keyIdentityProperty)
                            .SetParameter(entity)
                            .Build();
                        sqlCommandReturnId.Master = true;
                        sqlCommandReturnId.Transaction = transaction;
                        sqlCommandReturnId.CommandTimeout = CommandTimeout;
                        return await ExecuteAsync(sqlCommandReturnId) > 0;
                    }
                default:
                    {
                        var sqlCommandReturnId = this.CreateInsertableBuilder()
                            .InsertFields(fieldExpression)
                            .ReturnAutoIncrementId()
                            .SetParameter(entity)
                            .Build();
                        sqlCommandReturnId.Master = true;
                        sqlCommandReturnId.Transaction = transaction;
                        sqlCommandReturnId.CommandTimeout = CommandTimeout;
                        var id = await ExecuteScalarAsync<long>(sqlCommandReturnId);
                        if (id < 1)
                        {
                            return false;
                        }

                        keyIdentityProperty.SetValue(entity, id, null);
                        return true;
                    }
            }
        }

        var sqlCommand = this.CreateInsertableBuilder()
            .InsertFields(fieldExpression)
            .SetParameter(entity)
            .Build();
        sqlCommand.Master = true;
        sqlCommand.Transaction = transaction;
        sqlCommand.CommandTimeout = CommandTimeout;
        return await ExecuteAsync(sqlCommand) > 0;
    }
    public virtual async Task<bool> AddAsync(IEnumerable<TEntity> entities, bool returnAutoIncrementId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
    {
        if (entities == null || !entities.Any()) return false;

        if (returnAutoIncrementId && typeof(TEntity).GetEntityInfo().FieldInfos.FirstOrDefault(c => c.IsPrimaryKey && c.IsIdentityField)?.Property != null)
        {
            //if (transaction?.Connection == null)
            //{
            //    return await ExecuteAutoTransactionAsync(async trans =>
            //    {
            //        foreach (var entity in entities)
            //        {
            //            if (!await AddAsync(entity, returnAutoIncrementId, fieldExpression, trans))
            //            {
            //                return false;
            //            }
            //        }
            //        return true;
            //    });
            //}

            foreach (var entity in entities)
            {
                if (!await AddAsync(entity, returnAutoIncrementId, fieldExpression, transaction))
                {
                    return false;
                }
            }

            return true;
        }

        var bulkCountLimit = BulkEntityCount ?? DbContextConfiguration.Options.BulkEntityCount;
        if (bulkCountLimit.HasValue && entities.Count() > bulkCountLimit.Value)
        {
            return await entities.PagingExecuteAsync(bulkCountLimit.Value, async (pageNumber, models) =>
            {
                var sqlCommand = this.CreateInsertableBuilder()
                    .InsertFields(fieldExpression)
                    .SetParameter(models)
                    .Build();
                sqlCommand.Master = true;
                sqlCommand.Transaction = transaction;
                sqlCommand.CommandTimeout = CommandTimeout;
                return await ExecuteAsync(sqlCommand) > 0;
            });
        }

        var sqlCommand = this.CreateInsertableBuilder()
            .InsertFields(fieldExpression)
            .SetParameter(entities)
            .Build();
        sqlCommand.Master = true;
        sqlCommand.Transaction = transaction;
        sqlCommand.CommandTimeout = CommandTimeout;
        return await ExecuteAsync(sqlCommand) > 0;
    }

    public virtual async Task<bool> AddOrUpdateAsync(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
    {
        if (entity == null) return false;

        switch (DbType)
        {
            case DatabaseType.MySql:
            case DatabaseType.MariaDB:
            case DatabaseType.TiDB:
            case DatabaseType.OceanBase:
            case DatabaseType.SQLite:
                {
                    var sqlCommand = this.CreateReplaceableBuilder()
                        .InsertFields(fieldExpression)
                        .SetParameter(entity)
                        .Build();
                    sqlCommand.Master = true;
                    sqlCommand.Transaction = transaction;
                    sqlCommand.CommandTimeout = CommandTimeout;
                    return await ExecuteAsync(sqlCommand) > 0;
                }
            default:
                {
                    var pkFields = typeof(TEntity).GetEntityInfo().FieldInfos.Where(c => c.IsPrimaryKey).Select(c => c.FieldName).ToList();
                    if (pkFields == null || !pkFields.Any()) throw new Exception($"The entity class '{typeof(TEntity).Name}' does not define a primary key field.");

                    ICountable<TEntity> countableBuilder = this.CreateCountableBuilder();
                    pkFields.ForEach(pkField => countableBuilder.WhereField(entity1 => pkField, SqlOperation.Equal));
                    countableBuilder.SetParameter(entity);
                    ISqlCommand sqlCommand = countableBuilder.Build();
                    sqlCommand.Master = true;
                    sqlCommand.CommandTimeout = CommandTimeout;
                    if (await ExecuteScalarAsync<int>(sqlCommand) < 1)
                    {
                        // INSERT
                        return await AddAsync(entity, false, fieldExpression, transaction);
                    }

                    //if (transaction?.Connection == null)
                    //{
                    //    return await ExecuteAutoTransactionAsync(async trans =>
                    //    {
                    //        // DELETE && INSERT
                    //        return await DeleteAsync(entity, trans) && await AddAsync(entity, false, fieldExpression, trans);
                    //    });
                    //}

                    // DELETE && INSERT
                    return await DeleteAsync(entity, transaction) && await AddAsync(entity, false, fieldExpression, transaction);
                }
        }
    }
    public virtual async Task<bool> AddOrUpdateAsync(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
    {
        if (entities == null || !entities.Any()) return false;

        switch (DbType)
        {
            case DatabaseType.MySql:
            case DatabaseType.MariaDB:
            case DatabaseType.TiDB:
            case DatabaseType.OceanBase:
            case DatabaseType.SQLite:
                {
                    var bulkCountLimit = BulkEntityCount ?? DbContextConfiguration.Options.BulkEntityCount;
                    if (bulkCountLimit.HasValue && entities.Count() > bulkCountLimit.Value)
                    {
                        return await entities.PagingExecuteAsync(bulkCountLimit.Value, async (pageNumber, models) =>
                        {
                            var sqlCommand = this.CreateReplaceableBuilder()
                                .InsertFields(fieldExpression)
                                .SetParameter(models)
                                .Build();
                            sqlCommand.Master = true;
                            sqlCommand.Transaction = transaction;
                            sqlCommand.CommandTimeout = CommandTimeout;
                            return await ExecuteAsync(sqlCommand) > 0;
                        });
                    }

                    var sqlCommand = this.CreateReplaceableBuilder()
                        .InsertFields(fieldExpression)
                        .SetParameter(entities)
                        .Build();
                    sqlCommand.Master = true;
                    sqlCommand.Transaction = transaction;
                    sqlCommand.CommandTimeout = CommandTimeout;
                    return await ExecuteAsync(sqlCommand) > 0;
                }
            default:
                {
                    //if (transaction?.Connection == null)
                    //{
                    //    return await ExecuteAutoTransactionAsync(async trans =>
                    //    {
                    //        foreach (var entity in entities)
                    //        {
                    //            if (!await AddOrUpdateAsync(entity, fieldExpression, trans))
                    //            {
                    //                return false;
                    //            }
                    //        }
                    //        return true;
                    //    });
                    //}

                    foreach (var entity in entities)
                    {
                        if (!await AddOrUpdateAsync(entity, fieldExpression, transaction))
                        {
                            return false;
                        }
                    }

                    return true;
                }
        }
    }

    public virtual async Task<bool> DeleteAsync(TEntity entity, IDbTransaction transaction = null)
    {
        var sqlCommand = this.CreateDeleteableBuilder()
            .SetParameter(entity)
            .Build();
        sqlCommand.Master = true;
        sqlCommand.Transaction = transaction;
        sqlCommand.CommandTimeout = CommandTimeout;
        return await ExecuteAsync(sqlCommand) > 0;
    }
    public virtual async Task<bool> DeleteAsync(IEnumerable<TEntity> entities, IDbTransaction transaction = null)
    {
        if (entities == null || !entities.Any()) return false;

        foreach (var entity in entities)
        {
            if (!await DeleteAsync(entity, transaction))
            {
                return false;
            }
        }

        return true;
    }
    public virtual async Task<int> DeleteAsync(Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null)
    {
        var sqlCommand = this.CreateDeleteableBuilder()
            .Where(whereExpression)
            .Build();
        sqlCommand.Master = true;
        sqlCommand.Transaction = transaction;
        sqlCommand.CommandTimeout = CommandTimeout;
        return await ExecuteAsync(sqlCommand);
    }
    public virtual async Task<int> DeleteAllAsync(IDbTransaction transaction = null)
    {
        return await DeleteAllAsync(TableName(), transaction);
    }

    public virtual async Task<int> UpdateAsync(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, Expression<Func<TEntity, bool>> whereExpression = null, IDbTransaction transaction = null)
    {
        var sqlCommand = this.CreateUpdateableBuilder()
            .UpdateFields(fieldExpression)
            .Where(whereExpression)
            .SetParameter(entity)
            .Build();
        sqlCommand.Master = true;
        sqlCommand.Transaction = transaction;
        sqlCommand.CommandTimeout = CommandTimeout;
        return await ExecuteAsync(sqlCommand);
    }
    public virtual async Task<bool> UpdateAsync(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
    {
        if (entities == null || !entities.Any()) return false;

        //if (transaction?.Connection == null)
        //{
        //    return await ExecuteAutoTransactionAsync(async trans =>
        //    {
        //        foreach (var entity in entities)
        //        {
        //            if (!(await UpdateAsync(entity, fieldExpression, null, trans) > 0))
        //            {
        //                return false;
        //            }
        //        }
        //        return true;
        //    });
        //}

        foreach (var entity in entities)
        {
            if (!(await UpdateAsync(entity, fieldExpression, null, transaction) > 0))
            {
                return false;
            }
        }

        return true;
    }

    public virtual async Task<bool> IncrementAsync<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null) where TValue : struct
    {
        var sqlCommand = this.CreateUpdateableBuilder()
            .IncrementFields(fieldExpression, value)
            .Where(whereExpression)
            .Build();
        sqlCommand.Master = true;
        sqlCommand.Transaction = transaction;
        sqlCommand.CommandTimeout = CommandTimeout;
        return await ExecuteAsync(sqlCommand) > 0;
    }
    public virtual async Task<bool> DecrementAsync<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null) where TValue : struct
    {
        var sqlCommand = this.CreateUpdateableBuilder()
            .DecrementFields(fieldExpression, value)
            .Where(whereExpression)
            .Build();
        sqlCommand.Master = true;
        sqlCommand.Transaction = transaction;
        sqlCommand.CommandTimeout = CommandTimeout;
        return await ExecuteAsync(sqlCommand) > 0;
    }

    public virtual async Task<bool> SaveAsync<TEntityState>(TEntityState entity, bool returnAutoIncrementId = false, IDbTransaction transaction = null) where TEntityState : TEntity, IEntityStateBase
    {
        if (entity == null)
        {
            return false;
        }

        if (entity.EntityState == EntityStateType.Unchanged)
        {
            return true;
        }

        switch (entity.EntityState)
        {
            case EntityStateType.Added:
                if (!await AddAsync(entity, returnAutoIncrementId, transaction: transaction))
                {
                    return false;
                }
                break;
            case EntityStateType.Modified:
                if (await UpdateAsync(entity, transaction: transaction) < 1)
                {
                    return false;
                }
                break;
            case EntityStateType.Deleted:
                if (!await DeleteAsync(entity, transaction: transaction))
                {
                    return false;
                }
                break;
            default:
                throw new NotImplementedException($"EntityState: {entity.EntityState}");
        }

        if (transaction == null)
        {
            entity.ResetEntityState();
        }

        return true;
    }
    public virtual async Task<bool> SaveAsync<TEntityState>(IEnumerable<TEntityState> entities, bool returnAutoIncrementId = false, IDbTransaction transaction = null) where TEntityState : TEntity, IEntityStateBase
    {
        if (entities == null || !entities.Any())
        {
            return false;
        }

        if (entities.All(c => c.EntityState == EntityStateType.Unchanged))
        {
            return true;
        }

        var result = await ExecuteAutoTransactionAsync(async trans =>
        {
            var addList = entities.Where(t => t.EntityState == EntityStateType.Added).Select(c => (TEntity)c).ToList();
            if (addList.Any())
            {
                if (!await AddAsync(addList, returnAutoIncrementId, transaction: trans))
                {
                    return false;
                }
            }

            var updateList = entities.Where(t => t.EntityState == EntityStateType.Modified).Select(c => (TEntity)c).ToList();
            if (updateList.Any())
            {
                if (!await UpdateAsync(updateList, transaction: trans))
                {
                    return false;
                }
            }

            var deleteList = entities.Where(t => t.EntityState == EntityStateType.Deleted).Select(c => (TEntity)c).ToList();
            if (deleteList.Any())
            {
                if (!await DeleteAsync(deleteList, transaction: trans))
                {
                    return false;
                }
            }

            return true;
        }, transaction);

        if (result && transaction == null)
        {
            entities.ResetEntityState();
        }

        return result;
    }

    public virtual async Task<PageQueryResult<TEntity>> PageQueryAsync(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy, int pageNumber, int pageSize, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true)
    {
        var result = new PageQueryResult<TEntity>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Total = await CountAsync(whereExpression, master),
            List = (await QueryAsync(whereExpression, orderBy, pageNumber, pageSize, fieldExpression, master))?.ToList()
        };
        return result;
    }

    public virtual async Task<IEnumerable<TEntity>> QueryAsync(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? pageNumber = null, int? pageSize = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true)
    {
        var sqlCommand = this.CreateQueryableBuilder()
            .SelectFields(fieldExpression)
            .Where(whereExpression)
            .OrderBy(orderBy)
            .Page(pageNumber, pageSize)
            .Build();
        sqlCommand.Master = master;
        sqlCommand.CommandTimeout = CommandTimeout;
        return await QueryAsync<TEntity>(sqlCommand);
    }
    public virtual async Task<IEnumerable<TEntity>> QueryOffsetAsync(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? offset = null, int? rows = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true)
    {
        var sqlCommand = this.CreateQueryableBuilder()
            .SelectFields(fieldExpression)
            .Where(whereExpression)
            .OrderBy(orderBy)
            .Offset(offset, rows)
            .Build();
        sqlCommand.Master = master;
        sqlCommand.CommandTimeout = CommandTimeout;
        return await QueryAsync<TEntity>(sqlCommand);
    }

    public virtual async Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true)
    {
        var sqlCommand = this.CreateQueryableBuilder()
            .SelectFields(fieldExpression)
            .Where(whereExpression)
            .Build();
        sqlCommand.Master = master;
        sqlCommand.CommandTimeout = CommandTimeout;
        return await GetAsync<TEntity>(sqlCommand);
    }

    public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>> whereExpression, bool master = true)
    {
        var sqlCommand = this.CreateCountableBuilder()
            .Where(whereExpression)
            .Build();
        sqlCommand.Master = master;
        sqlCommand.CommandTimeout = CommandTimeout;
        return await ExecuteScalarAsync<int>(sqlCommand);
    }

    public virtual async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> whereExpression, bool master = true)
    {
        return await CountAsync(whereExpression, master) > 0;
    }

    public virtual async Task<bool> IsTableExistsAsync(bool master = true, bool useCache = true)
    {
        return await IsTableExistsAsync(TableName(), master, useCache);
    }

    public virtual async Task<bool> IsTableFieldExistsAsync(Expression<Func<TEntity, object>> fieldExpression, bool master = true, bool useCache = true)
    {
        var fieldNames = fieldExpression.GetFieldNames();
        if (fieldNames == null || fieldNames.Count < 1)
        {
            return false;
        }

        var tableName = TableName();
        foreach (var fieldName in fieldNames)
        {
            if (!await IsTableFieldExistsAsync(tableName, fieldName, master, useCache))
            {
                return false;
            }
        }
        return true;
    }

    public virtual async Task AddTableFieldAsync(Expression<Func<TEntity, object>> fieldExpression, string fieldType, bool master = true)
    {
        var fieldNames = fieldExpression.GetFieldNames();
        if (fieldNames == null || fieldNames.Count < 1)
        {
            return;
        }

        var tableName = TableName();
        foreach (var fieldName in fieldNames)
        {
            await AddTableFieldAsync(tableName, fieldName, fieldType, master);
        }
    }
    #endregion
}