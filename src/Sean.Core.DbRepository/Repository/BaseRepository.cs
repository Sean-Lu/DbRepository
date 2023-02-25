using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using System.Transactions;
using Sean.Core.DbRepository.Extensions;
#if NETSTANDARD
using Microsoft.Extensions.Configuration;
#endif

namespace Sean.Core.DbRepository
{
    /// <summary>
    /// Database table base repository.
    /// </summary>
    public abstract class BaseRepository : IBaseRepository
    {
        public DbFactory Factory { get; }

        public DatabaseType DbType => Factory.DbType;

        public ISqlMonitor SqlMonitor => Factory.SqlMonitor;

        public int? BulkCountLimit { get; set; }

        public int? CommandTimeout
        {
            get => Factory.CommandTimeout;
            set => Factory.CommandTimeout = value;
        }

        #region Constructors
#if NETSTANDARD
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

        public virtual string CreateTableSql(string tableName)
        {
            throw new NotImplementedException();
        }

        public virtual void AutoCreateTable(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(tableName));

            if (!IsTableExists(tableName, master: true, useCache: true))// Using master database.
            {
                var sql = CreateTableSql(tableName);
                if (!string.IsNullOrWhiteSpace(sql))
                {
#if NET45
                    using (var tranScope = new TransactionScope(TransactionScopeOption.Suppress))
#else
                    using (var tranScope = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
#endif
                    {
                        Execute(sql, master: true);
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

        public virtual DbConnection OpenNewConnection(bool master = true)
        {
            return Factory.OpenNewConnection(master);
        }

        #region Synchronous method
        public virtual int Execute(string sql, object param = null, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

            return Execute(new DefaultSqlCommand
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

            return Factory.ExecuteNonQuery(sqlCommand);
        }

        public virtual IEnumerable<T> Query<T>(string sql, object param = null, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

            return Query<T>(new DefaultSqlCommand
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

            return Get<T>(new DefaultSqlCommand
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

            return ExecuteScalar<T>(new DefaultSqlCommand
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

            return ExecuteScalar(new DefaultSqlCommand
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

            return ExecuteDataTable(new DefaultSqlCommand
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

            return ExecuteDataSet(new DefaultSqlCommand
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
                return func(transaction);
            }

            var isInternalConnection = false;
            try
            {
                if (connection == null)
                {
                    connection = OpenNewConnection(true);
                    isInternalConnection = true;
                }

                using (var trans = connection.BeginTransaction())
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
                }
            }
            finally
            {
                if (isInternalConnection)
                {
                    connection?.Dispose();
                }
            }
        }
        public virtual bool ExecuteAutoTransaction(Func<IDbTransaction, bool> func, IDbTransaction transaction = null, IDbConnection connection = null)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));

            if (transaction != null)
            {
                return func(transaction);
            }

            var isInternalConnection = false;
            try
            {
                if (connection == null)
                {
                    connection = OpenNewConnection(true);
                    isInternalConnection = true;
                }

                using (var trans = connection.BeginTransaction())
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
                }
            }
            finally
            {
                if (isInternalConnection)
                {
                    connection?.Dispose();
                }
            }
        }

        public virtual bool IsTableExists(string tableName, bool master = true, bool useCache = true)
        {
            return this.IsTableExists(tableName, (sql, connection) => ExecuteScalar<int>(sql, connection: connection) > 0, master, useCache);
        }

        public virtual bool IsTableFieldExists(string tableName, string fieldName, bool master = true, bool useCache = true)
        {
            return this.IsTableFieldExists(tableName, fieldName, (sql, connection) => ExecuteScalar<int>(sql, connection: connection) > 0, master, useCache);
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

            Execute($"ALTER TABLE {tableName} ADD COLUMN {fieldName} {fieldType};", master: master);
        }

        public virtual int DeleteAll(string tableName, IDbTransaction transaction = null)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(tableName));

            return Execute($"DELETE FROM {tableName};", transaction: transaction);
        }
        #endregion

        #region Asynchronous method
#if NETSTANDARD || NET45_OR_GREATER
        public virtual async Task<int> ExecuteAsync(string sql, object param = null, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

            return await ExecuteAsync(new DefaultSqlCommand
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

            return await Factory.ExecuteNonQueryAsync(sqlCommand);
        }

        public virtual async Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null, bool master = true, IDbTransaction transaction = null, IDbConnection connection = null)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

            return await QueryAsync<T>(new DefaultSqlCommand
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

            return await GetAsync<T>(new DefaultSqlCommand
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

            return await ExecuteScalarAsync<T>(new DefaultSqlCommand
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

            return await ExecuteScalarAsync(new DefaultSqlCommand
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

            return await ExecuteDataTableAsync(new DefaultSqlCommand
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

            return await ExecuteDataSetAsync(new DefaultSqlCommand
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
                return await func(transaction);
            }

            var isInternalConnection = false;
            try
            {
                if (connection == null)
                {
                    connection = OpenNewConnection(true);
                    isInternalConnection = true;
                }

                using (var trans = connection.BeginTransaction())
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
                }
            }
            finally
            {
                if (isInternalConnection)
                {
                    connection?.Dispose();
                }
            }
        }
        public virtual async Task<bool> ExecuteAutoTransactionAsync(Func<IDbTransaction, Task<bool>> func, IDbTransaction transaction = null, IDbConnection connection = null)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));

            if (transaction != null)
            {
                return await func(transaction);
            }

            var isInternalConnection = false;
            try
            {
                if (connection == null)
                {
                    connection = OpenNewConnection(true);
                    isInternalConnection = true;
                }

                using (var trans = connection.BeginTransaction())
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
                }
            }
            finally
            {
                if (isInternalConnection)
                {
                    connection?.Dispose();
                }
            }
        }

        public virtual async Task<bool> IsTableExistsAsync(string tableName, bool master = true, bool useCache = true)
        {
            return await this.IsTableExistsAsync(tableName, async (sql, connection) => await ExecuteScalarAsync<int>(sql, connection: connection) > 0, master, useCache);
        }

        public virtual async Task<bool> IsTableFieldExistsAsync(string tableName, string fieldName, bool master = true, bool useCache = true)
        {
            return await this.IsTableFieldExistsAsync(tableName, fieldName, async (sql, connection) => await ExecuteScalarAsync<int>(sql, connection: connection) > 0, master, useCache);
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

            await ExecuteAsync($"ALTER TABLE {tableName} ADD COLUMN {fieldName} {fieldType};", master: master);
        }

        public virtual async Task<int> DeleteAllAsync(string tableName, IDbTransaction transaction = null)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(tableName));

            return await ExecuteAsync($"DELETE FROM {tableName};", transaction: transaction);
        }
#endif
        #endregion
    }
}
