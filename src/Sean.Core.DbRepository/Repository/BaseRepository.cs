using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using System.Transactions;
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

        #region Constructors
#if NETSTANDARD
        /// <summary>
        /// Single or clustered database.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="configName">Configuration ConnectionStrings name</param>
        protected BaseRepository(IConfiguration configuration = null, string configName = Constants.Master)
        {
            Factory = new DbFactory(configuration, configName);
        }
#else
        /// <summary>
        /// Single or clustered database.
        /// </summary>
        /// <param name="configName">Configuration ConnectionStrings name</param>
        protected BaseRepository(string configName = Constants.Master)
        {
            Factory = new DbFactory(configName);
        }
#endif
        /// <summary>
        /// Single or clustered database.
        /// </summary>
        /// <param name="connectionSettings"></param>
        protected BaseRepository(MultiConnectionSettings connectionSettings)
        {
            Factory = new DbFactory(connectionSettings);
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
        #endregion

        #region Asynchronous method
#if NETSTANDARD || NET45_OR_GREATER
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
                    if (!(await func(trans)))
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
#endif
        #endregion
    }
}
