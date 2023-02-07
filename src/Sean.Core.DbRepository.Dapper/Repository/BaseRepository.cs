using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Sean.Core.DbRepository.Dapper.Extensions;
using Sean.Core.DbRepository.Extensions;
#if NETSTANDARD
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
#if NETSTANDARD
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

            return Execute(connection => connection.Execute(sqlCommand, this), sqlCommand.Master, sqlCommand.Transaction);
        }
        public override IEnumerable<T> Query<T>(ISqlCommand sqlCommand)
        {
            if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

            return Execute(connection => connection.Query<T>(sqlCommand, this), sqlCommand.Master, sqlCommand.Transaction);
        }
        public override T Get<T>(ISqlCommand sqlCommand)
        {
            if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

            return Execute(connection => connection.Get<T>(sqlCommand, this), sqlCommand.Master, sqlCommand.Transaction);
        }
        public override T ExecuteScalar<T>(ISqlCommand sqlCommand)
        {
            if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

            return Execute(connection => connection.ExecuteScalar<T>(sqlCommand, this), sqlCommand.Master, sqlCommand.Transaction);
        }
        public override object ExecuteScalar(ISqlCommand sqlCommand)
        {
            if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

            return Execute(connection => connection.ExecuteScalar(sqlCommand, this), sqlCommand.Master, sqlCommand.Transaction);
        }

        public override bool IsTableExists(string tableName, bool master = true, bool useCache = true)
        {
            return this.IsTableExists(tableName, (sql, connection) => connection.ExecuteScalar<int>(new DefaultSqlCommand { Sql = sql, CommandTimeout = CommandTimeout }, this) > 0, master, useCache);
        }

        public override bool IsTableFieldExists(string tableName, string fieldName, bool master = true, bool useCache = true)
        {
            return this.IsTableFieldExists(tableName, fieldName, (sql, connection) => connection.ExecuteScalar<int>(new DefaultSqlCommand { Sql = sql, CommandTimeout = CommandTimeout }, this) > 0, master, useCache);
        }
        #endregion

        #region Asynchronous method
#if NETSTANDARD || NET45_OR_GREATER
        public override async Task<int> ExecuteAsync(ISqlCommand sqlCommand)
        {
            if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

            return await ExecuteAsync(async connection => await connection.ExecuteAsync(sqlCommand, this), sqlCommand.Master, sqlCommand.Transaction);
        }
        public override async Task<IEnumerable<T>> QueryAsync<T>(ISqlCommand sqlCommand)
        {
            if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

            return await ExecuteAsync(async connection => await connection.QueryAsync<T>(sqlCommand, this), sqlCommand.Master, sqlCommand.Transaction);
        }
        public override async Task<T> GetAsync<T>(ISqlCommand sqlCommand)
        {
            if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

            return await ExecuteAsync(async connection => await connection.GetAsync<T>(sqlCommand, this), sqlCommand.Master, sqlCommand.Transaction);
        }
        public override async Task<T> ExecuteScalarAsync<T>(ISqlCommand sqlCommand)
        {
            if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

            return await ExecuteAsync(async connection => await connection.ExecuteScalarAsync<T>(sqlCommand, this), sqlCommand.Master, sqlCommand.Transaction);
        }
        public override async Task<object> ExecuteScalarAsync(ISqlCommand sqlCommand)
        {
            if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

            return await ExecuteAsync(async connection => await connection.ExecuteScalarAsync(sqlCommand, this), sqlCommand.Master, sqlCommand.Transaction);
        }

        public override async Task<bool> IsTableExistsAsync(string tableName, bool master = true, bool useCache = true)
        {
            return await this.IsTableExistsAsync(tableName, async (sql, connection) => await connection.ExecuteScalarAsync<int>(new DefaultSqlCommand { Sql = sql, CommandTimeout = CommandTimeout }, this) > 0, master, useCache);
        }

        public override async Task<bool> IsTableFieldExistsAsync(string tableName, string fieldName, bool master = true, bool useCache = true)
        {
            return await this.IsTableFieldExistsAsync(tableName, fieldName, async (sql, connection) => await connection.ExecuteScalarAsync<int>(new DefaultSqlCommand { Sql = sql, CommandTimeout = CommandTimeout }, this) > 0, master, useCache);
        }
#endif
        #endregion
    }
}
