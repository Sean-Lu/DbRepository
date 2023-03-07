using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Sean.Core.DbRepository.Dapper.Extensions;
#if NETSTANDARD || NET5_0_OR_GREATER
using Microsoft.Extensions.Configuration;
#endif

namespace Sean.Core.DbRepository.Dapper
{
    /// <summary>
    /// Database table entity base repository.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public abstract class BaseRepository<TEntity> : EntityBaseRepository<TEntity> where TEntity : class
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

        #region Synchronous method
        public override int Execute(ISqlCommand sqlCommand)
        {
            if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

            return Execute(connection => connection.Execute(sqlCommand, SqlMonitor), sqlCommand.Master, sqlCommand.Transaction, sqlCommand.Connection);
        }
        public override IEnumerable<T> Query<T>(ISqlCommand sqlCommand)
        {
            if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

            return Execute(connection => connection.Query<T>(sqlCommand, SqlMonitor), sqlCommand.Master, sqlCommand.Transaction, sqlCommand.Connection);
        }
        public override T Get<T>(ISqlCommand sqlCommand)
        {
            if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

            return Execute(connection => connection.Get<T>(sqlCommand, SqlMonitor), sqlCommand.Master, sqlCommand.Transaction, sqlCommand.Connection);
        }
        public override T ExecuteScalar<T>(ISqlCommand sqlCommand)
        {
            if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

            return Execute(connection => connection.ExecuteScalar<T>(sqlCommand, SqlMonitor), sqlCommand.Master, sqlCommand.Transaction, sqlCommand.Connection);
        }
        public override object ExecuteScalar(ISqlCommand sqlCommand)
        {
            if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

            return Execute(connection => connection.ExecuteScalar(sqlCommand, SqlMonitor), sqlCommand.Master, sqlCommand.Transaction, sqlCommand.Connection);
        }
        public override DataTable ExecuteDataTable(ISqlCommand sqlCommand)
        {
            if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

            return Execute(connection => connection.ExecuteDataTable(sqlCommand, SqlMonitor), sqlCommand.Master, sqlCommand.Transaction, sqlCommand.Connection);
        }
        public override DataSet ExecuteDataSet(ISqlCommand sqlCommand)
        {
            if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

            return Execute(connection => connection.ExecuteDataSet(sqlCommand, SqlMonitor), sqlCommand.Master, sqlCommand.Transaction, sqlCommand.Connection);
        }
        public override IDataReader ExecuteReader(ISqlCommand sqlCommand)
        {
            if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

            return Execute(connection => connection.ExecuteReader(sqlCommand, SqlMonitor), sqlCommand.Master, sqlCommand.Transaction, sqlCommand.Connection);
        }
        #endregion

        #region Asynchronous method
        public override async Task<int> ExecuteAsync(ISqlCommand sqlCommand)
        {
            if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

            return await ExecuteAsync(async connection => await connection.ExecuteAsync(sqlCommand, SqlMonitor), sqlCommand.Master, sqlCommand.Transaction, sqlCommand.Connection);
        }
        public override async Task<IEnumerable<T>> QueryAsync<T>(ISqlCommand sqlCommand)
        {
            if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

            return await ExecuteAsync(async connection => await connection.QueryAsync<T>(sqlCommand, SqlMonitor), sqlCommand.Master, sqlCommand.Transaction, sqlCommand.Connection);
        }
        public override async Task<T> GetAsync<T>(ISqlCommand sqlCommand)
        {
            if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

            return await ExecuteAsync(async connection => await connection.GetAsync<T>(sqlCommand, SqlMonitor), sqlCommand.Master, sqlCommand.Transaction, sqlCommand.Connection);
        }
        public override async Task<T> ExecuteScalarAsync<T>(ISqlCommand sqlCommand)
        {
            if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

            return await ExecuteAsync(async connection => await connection.ExecuteScalarAsync<T>(sqlCommand, SqlMonitor), sqlCommand.Master, sqlCommand.Transaction, sqlCommand.Connection);
        }
        public override async Task<object> ExecuteScalarAsync(ISqlCommand sqlCommand)
        {
            if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

            return await ExecuteAsync(async connection => await connection.ExecuteScalarAsync(sqlCommand, SqlMonitor), sqlCommand.Master, sqlCommand.Transaction, sqlCommand.Connection);
        }
        public override async Task<DataTable> ExecuteDataTableAsync(ISqlCommand sqlCommand)
        {
            if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

            return await ExecuteAsync(async connection => await connection.ExecuteDataTableAsync(sqlCommand, SqlMonitor), sqlCommand.Master, sqlCommand.Transaction, sqlCommand.Connection);
        }
        public override async Task<DataSet> ExecuteDataSetAsync(ISqlCommand sqlCommand)
        {
            if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

            return await ExecuteAsync(async connection => await connection.ExecuteDataSetAsync(sqlCommand, SqlMonitor), sqlCommand.Master, sqlCommand.Transaction, sqlCommand.Connection);
        }
        public override async Task<IDataReader> ExecuteReaderAsync(ISqlCommand sqlCommand)
        {
            if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

            return await ExecuteAsync(async connection => await connection.ExecuteReaderAsync(sqlCommand, SqlMonitor), sqlCommand.Master, sqlCommand.Transaction, sqlCommand.Connection);
        }
        #endregion
    }
}
