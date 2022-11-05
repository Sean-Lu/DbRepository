using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using System.Transactions;
using Sean.Core.DbRepository.Extensions;
using Sean.Core.DbRepository.Util;
#if NETSTANDARD
using Microsoft.Extensions.Configuration;
#endif

namespace Sean.Core.DbRepository
{
    /// <summary>
    /// Database table base repository
    /// </summary>
    public abstract class BaseRepository : IBaseRepository
    {
        public DbFactory Factory { get; }

        public DatabaseType DbType => Factory.DbType;

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
        protected BaseRepository(IConfiguration configuration = null, string configName = Constants.Master)
        {
            Factory = new DbFactory(configuration, configName)
            {
                SqlMonitor = new DefaultSqlMonitor(OnSqlExecuting, OnSqlExecuted)
            };
        }
#else
        /// <summary>
        /// Single or clustered database.
        /// </summary>
        /// <param name="configName">Configuration ConnectionStrings name</param>
        protected BaseRepository(string configName = Constants.Master)
        {
            Factory = new DbFactory(configName)
            {
                SqlMonitor = new DefaultSqlMonitor(OnSqlExecuting, OnSqlExecuted)
            };
        }
#endif
        /// <summary>
        /// Single or clustered database.
        /// </summary>
        /// <param name="connectionSettings"></param>
        protected BaseRepository(MultiConnectionSettings connectionSettings)
        {
            Factory = new DbFactory(connectionSettings)
            {
                SqlMonitor = new DefaultSqlMonitor(OnSqlExecuting, OnSqlExecuted)
            };
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

        public virtual string TableName()
        {
            return null;
        }

        public virtual string CreateTableSql(string tableName)
        {
            return null;
        }

        public virtual void OnSqlExecuting(SqlExecutingContext context)
        {

        }

        public virtual void OnSqlExecuted(SqlExecutedContext context)
        {

        }

        public virtual IDbConnection OpenNewConnection(bool master)
        {
            return Factory.OpenNewConnection(master);
        }

        #region Synchronous method
        public virtual int Execute(string sql, object param = null, IDbTransaction transaction = null, bool master = true)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

            return new DefaultSqlWithParameter
            {
                Sql = sql,
                Parameter = param
            }.Execute(Factory, transaction, master);
        }
        public virtual IEnumerable<T> Query<T>(string sql, object param = null, IDbTransaction transaction = null, bool master = true)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

            return new DefaultSqlWithParameter
            {
                Sql = sql,
                Parameter = param
            }.Query<T>(Factory, transaction, master);
        }
        public virtual T QueryFirstOrDefault<T>(string sql, object param = null, IDbTransaction transaction = null, bool master = true)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

            return new DefaultSqlWithParameter
            {
                Sql = sql,
                Parameter = param
            }.QueryFirstOrDefault<T>(Factory, transaction, master);
        }
        public virtual T ExecuteScalar<T>(string sql, object param = null, IDbTransaction transaction = null, bool master = true)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

            return new DefaultSqlWithParameter
            {
                Sql = sql,
                Parameter = param
            }.ExecuteScalar<T>(Factory, transaction, master);
        }

        public virtual T Execute<T>(Func<IDbConnection, T> func, bool master = true, IDbTransaction transaction = null)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));

            if (transaction == null)
            {
                using (var connection = OpenNewConnection(master))
                {
                    return func(connection);
                }
            }

            return func(transaction.Connection);
        }

        public virtual T ExecuteTransaction<T>(Func<IDbTransaction, T> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));

            using (var connection = OpenNewConnection(true))
            {
                return ExecuteTransaction(connection, func);
            }
        }
        public virtual T ExecuteTransaction<T>(IDbConnection connection, Func<IDbTransaction, T> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));

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
        public virtual bool ExecuteAutoTransaction(Func<IDbTransaction, bool> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));

            using (var connection = OpenNewConnection(true))
            {
                return ExecuteAutoTransaction(connection, func);
            }
        }
        public virtual bool ExecuteAutoTransaction(IDbConnection connection, Func<IDbTransaction, bool> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));

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

        public virtual T ExecuteTransactionScope<T>(Func<TransactionScope, T> tranScope)
        {
            if (tranScope == null) throw new ArgumentNullException(nameof(tranScope));

            using (var trans = new TransactionScope())
            {
                return tranScope(trans);
            }
        }
        public virtual bool ExecuteAutoTransactionScope(Func<TransactionScope, bool> tranScope)
        {
            if (tranScope == null) throw new ArgumentNullException(nameof(tranScope));

            using (var trans = new TransactionScope())
            {
                var success = tranScope(trans);
                if (success)
                {
                    trans.Complete();
                }
                return success;
            }
        }
        #endregion

        #region Asynchronous method
#if NETSTANDARD || NET45_OR_GREATER
        public virtual async Task<int> ExecuteAsync(string sql, object param = null, IDbTransaction transaction = null, bool master = true)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

            return await new DefaultSqlWithParameter
            {
                Sql = sql,
                Parameter = param
            }.ExecuteAsync(Factory, transaction, master);
        }
        public virtual async Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null, IDbTransaction transaction = null, bool master = true)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

            return await new DefaultSqlWithParameter
            {
                Sql = sql,
                Parameter = param
            }.QueryAsync<T>(Factory, transaction, master);
        }
        public virtual async Task<T> QueryFirstOrDefaultAsync<T>(string sql, object param = null, IDbTransaction transaction = null, bool master = true)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

            return await new DefaultSqlWithParameter
            {
                Sql = sql,
                Parameter = param
            }.QueryFirstOrDefaultAsync<T>(Factory, transaction, master);
        }
        public virtual async Task<T> ExecuteScalarAsync<T>(string sql, object param = null, IDbTransaction transaction = null, bool master = true)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

            return await new DefaultSqlWithParameter
            {
                Sql = sql,
                Parameter = param
            }.ExecuteScalarAsync<T>(Factory, transaction, master);
        }

        public virtual async Task<T> ExecuteAsync<T>(Func<IDbConnection, Task<T>> func, bool master = true, IDbTransaction transaction = null)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));

            if (transaction == null)
            {
                using (var connection = OpenNewConnection(master))
                {
                    return await func(connection);
                }
            }

            return await func(transaction.Connection);
        }

        public virtual async Task<T> ExecuteTransactionAsync<T>(Func<IDbTransaction, Task<T>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));

            using (var connection = OpenNewConnection(true))
            {
                return await ExecuteTransactionAsync(connection, func);
            }
        }
        public virtual async Task<T> ExecuteTransactionAsync<T>(IDbConnection connection, Func<IDbTransaction, Task<T>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));

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
        public virtual async Task<bool> ExecuteAutoTransactionAsync(Func<IDbTransaction, Task<bool>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));

            using (var connection = OpenNewConnection(true))
            {
                return await ExecuteAutoTransactionAsync(connection, func);
            }
        }
        public virtual async Task<bool> ExecuteAutoTransactionAsync(IDbConnection connection, Func<IDbTransaction, Task<bool>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));

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
#endif

#if NETSTANDARD || NET451_OR_GREATER
        public virtual async Task<T> ExecuteTransactionScopeAsync<T>(Func<TransactionScope, Task<T>> tranScope)
        {
            if (tranScope == null) throw new ArgumentNullException(nameof(tranScope));

            using (var trans = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                return await tranScope(trans);
            }
        }
        public virtual async Task<bool> ExecuteAutoTransactionScopeAsync(Func<TransactionScope, Task<bool>> tranScope)
        {
            if (tranScope == null) throw new ArgumentNullException(nameof(tranScope));

            using (var trans = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var success = await tranScope(trans);
                if (success)
                {
                    trans.Complete();
                }
                return success;
            }
        }
#endif
        #endregion
    }
}
