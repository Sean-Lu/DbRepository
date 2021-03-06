using System;
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
    /// Database table base repository
    /// </summary>
    public abstract class BaseRepository : IBaseRepository
    {
        /// <summary>
        /// Database factory
        /// </summary>
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

        /// <summary>
        /// 表名
        /// </summary>
        /// <returns></returns>
        public virtual string TableName()
        {
            return null;
        }

        /// <summary>
        /// 返回创建表的SQL语句
        /// </summary>
        /// <param name="tableName">表名称</param>
        /// <returns></returns>
        public virtual string CreateTableSql(string tableName)
        {
            return null;
        }

        /// <summary>
        /// 输出执行的SQL语句
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        public virtual void OutputExecutedSql(string sql, object param)
        {

        }

        #region Synchronous method
        /// <summary>
        /// 执行
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="transaction">事务</param>
        /// <returns></returns>
        public virtual T Execute<T>(Func<IDbConnection, T> func, bool master = true, IDbTransaction transaction = null)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));

            if (transaction == null)
            {
                using (var connection = Factory.OpenConnection(master))
                {
                    return func(connection);
                }
            }

            return func(transaction.Connection);
        }

        /// <summary>
        /// 执行事务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <returns></returns>
        public virtual T ExecuteTransaction<T>(Func<IDbTransaction, T> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));

            using (var connection = Factory.OpenConnection())
            {
                return ExecuteTransaction(connection, func);
            }
        }
        /// <summary>
        /// 执行事务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="func"></param>
        /// <returns></returns>
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

        /// <summary>
        /// <para><see cref="DbTransaction"/>、<see cref="TransactionScope"/>的区别：</para>
        /// <para><see cref="DbTransaction"/>：每个<see cref="DbTransaction"/>是基于每个<see cref="DbConnection"/>的。这种设计对于跨越多个程序集或者多个方法的事务行为来说，不是非常好，需要把事务和数据库连接作为参数传入。</para>
        /// <para><see cref="TransactionScope"/>：<see cref="TransactionScope"/>是基于当前线程的，在当前线程中，调用<see cref="Transaction.Current"/>方法可以看到当前事务的信息，调用<see cref="TransactionScope.Complete"/>方法提交事务。</para>
        /// </summary>
        /// <param name="toDoInTransactionScope"></param>
        /// <returns></returns>
        public virtual T ExecuteTransactionScope<T>(Func<TransactionScope, T> toDoInTransactionScope)
        {
            if (toDoInTransactionScope == null) throw new ArgumentNullException(nameof(toDoInTransactionScope));

            using (var trans = new TransactionScope())
            {
                return toDoInTransactionScope(trans);
            }
        }
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
        public virtual async Task<T> ExecuteAsync<T>(Func<IDbConnection, Task<T>> func, bool master = true, IDbTransaction transaction = null)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));

            if (transaction == null)
            {
                using (var connection = Factory.OpenConnection(master))
                {
                    return await func(connection);
                }
            }

            return await func(transaction.Connection);
        }

        /// <summary>
        /// 异步执行事务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <returns></returns>
        public virtual async Task<T> ExecuteTransactionAsync<T>(Func<IDbTransaction, Task<T>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));

            using (var connection = Factory.OpenConnection())
            {
                return await ExecuteTransactionAsync(connection, func);
            }
        }
        /// <summary>
        /// 异步执行事务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="func"></param>
        /// <returns></returns>
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
#endif

#if NETSTANDARD || NET451_OR_GREATER
        /// <summary>
        /// <para><see cref="DbTransaction"/>、<see cref="TransactionScope"/>的区别：</para>
        /// <para><see cref="DbTransaction"/>：每个<see cref="DbTransaction"/>是基于每个<see cref="DbConnection"/>的。这种设计对于跨越多个程序集或者多个方法的事务行为来说，不是非常好，需要把事务和数据库连接作为参数传入。</para>
        /// <para><see cref="TransactionScope"/>：<see cref="TransactionScope"/>是基于当前线程的，在当前线程中，调用<see cref="Transaction.Current"/>方法可以看到当前事务的信息，调用<see cref="TransactionScope.Complete"/>方法提交事务。</para>
        /// </summary>
        /// <param name="toDoInTransactionScope"></param>
        /// <returns></returns>
        public virtual async Task<T> ExecuteTransactionScopeAsync<T>(Func<TransactionScope, Task<T>> toDoInTransactionScope)
        {
            if (toDoInTransactionScope == null) throw new ArgumentNullException(nameof(toDoInTransactionScope));

            using (var trans = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                return await toDoInTransactionScope(trans);
            }
        }
#endif
        #endregion
    }
}
