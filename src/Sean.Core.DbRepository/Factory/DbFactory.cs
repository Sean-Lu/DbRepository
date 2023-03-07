using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
#if NETSTANDARD || NET5_0_OR_GREATER
using Microsoft.Extensions.Configuration;
#endif
using Sean.Core.DbRepository.Extensions;
using Sean.Core.DbRepository.Util;
using Sean.Utility.Contracts;
using Sean.Utility.Format;

namespace Sean.Core.DbRepository
{
    /// <summary>
    /// Database factory
    /// </summary>
    public class DbFactory
    {
        /// <summary>
        /// The limit on the number of entities when executing database bulk operations. The default value is 1000.
        /// </summary>
        public static int? BulkCountLimit { get; set; } = 1000;
        /// <summary>
        /// The time (in seconds) to wait for the command to execute. The default value is 30 seconds.
        /// <para>等待命令执行所需的时间（以秒为单位）。 默认值为 30 秒。</para>
        /// </summary>
        public static int? DefaultCommandTimeout { get; set; }
        /// <summary>
        /// Whether the entity class property names are case-sensitive when mapping database table fields. The default value is false.
        /// <para>映射数据库表字段时，实体类属性名是否区分大小写。默认值：false。</para>
        /// </summary>
        public static bool CaseSensitive { get; set; } = false;
        /// <summary>
        /// <see cref="DbProviderFactory"/> configuration file path.
        /// <para>数据库提供者工厂的配置文件路径</para>
        /// </summary>
        public static string ProviderFactoryConfigurationPath { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $@"dllconfigs\{Assembly.GetExecutingAssembly().GetName().Name}.dll.config");

        public static IJsonSerializer JsonSerializer { get; set; } = JsonHelper.Serializer;

        public static event Action<SqlExecutingContext> OnSqlExecuting;
        public static event Action<SqlExecutedContext> OnSqlExecuted;

        /// <summary>
        /// <see cref="DbProviderFactory"/>
        /// </summary>
        public DbProviderFactory ProviderFactory
        {
            get => _providerFactory;
            set => OnDbProviderFactoryChanged(value);
        }
        /// <summary>
        /// Database type
        /// <para>数据库类型</para>
        /// </summary>
        public DatabaseType DbType
        {
            get => _dbType;
            set => OnDatabaseTypeChanged(value);
        }
        /// <summary>
        /// Database connection configuration.
        /// <para>数据库连接配置</para>
        /// </summary>
        public MultiConnectionSettings ConnectionSettings
        {
            get => _connectionSettings;
            set => OnConnectionStringChanged(value);
        }

        public ISqlMonitor SqlMonitor { get; }

        /// <summary>
        /// The time (in seconds) to wait for the command to execute. The default value is 30 seconds.
        /// <para>等待命令执行所需的时间（以秒为单位）。 默认值为 30 秒。</para>
        /// </summary>
        public int? CommandTimeout
        {
            get => _commandTimeout ?? DefaultCommandTimeout;
            set => _commandTimeout = value;
        }

        private DbProviderFactory _providerFactory;
        private DatabaseType _dbType;
        private MultiConnectionSettings _connectionSettings;
        private int? _commandTimeout;

        #region Constructors
#if NETSTANDARD || NET5_0_OR_GREATER
        /// <summary>
        /// Single or clustered database.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="configName">Configuration ConnectionStrings name</param>
        public DbFactory(IConfiguration configuration, string configName = Constants.Master)
        {
            if (string.IsNullOrWhiteSpace(configName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(configName));

            ConnectionSettings = new MultiConnectionSettings(configuration, configName);

            SqlMonitor = new DefaultSqlMonitor(OnSqlExecuting, OnSqlExecuted);
        }
#else
        /// <summary>
        /// Single or clustered database.
        /// </summary>
        /// <param name="configName">Configuration ConnectionStrings name</param>
        public DbFactory(string configName = Constants.Master)
        {
            if (string.IsNullOrWhiteSpace(configName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(configName));

            ConnectionSettings = new MultiConnectionSettings(configName);

            SqlMonitor = new DefaultSqlMonitor(OnSqlExecuting, OnSqlExecuted);
        }
#endif
        /// <summary>
        /// Single or clustered database.
        /// </summary>
        /// <param name="connectionSettings"></param>
        public DbFactory(MultiConnectionSettings connectionSettings)
        {
            ConnectionSettings = connectionSettings ?? throw new ArgumentNullException(nameof(connectionSettings));

            SqlMonitor = new DefaultSqlMonitor(OnSqlExecuting, OnSqlExecuted);
        }
        /// <summary>
        /// Single database.
        /// </summary>
        /// <param name="connString"></param>
        /// <param name="type"></param>
        public DbFactory(string connString, DatabaseType type) : this(new MultiConnectionSettings(ConnectionStringOptions.Create(connString, type)))
        {

        }
        /// <summary>
        /// Single database.
        /// </summary>
        /// <param name="connString"></param>
        /// <param name="factory"></param>
        public DbFactory(string connString, DbProviderFactory factory) : this(new MultiConnectionSettings(ConnectionStringOptions.Create(connString, factory)))
        {

        }
        /// <summary>
        /// Single database.
        /// </summary>
        /// <param name="connString"></param>
        /// <param name="providerName"></param>
        public DbFactory(string connString, string providerName) : this(new MultiConnectionSettings(ConnectionStringOptions.Create(connString, providerName)))
        {

        }
        #endregion

        #region ExecuteNonQuery
        /// <summary>
        /// Execute INSERT or DELETE or UPDATE operation.
        /// <para>执行 INSERT 或 DELETE 或 UPDATE 操作。</para>
        /// </summary>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <param name="commandType">Command type</param>
        /// <param name="master">true: master database, false: slave database.</param>
        /// <returns>Returns the number of affected rows.</returns>
        public int ExecuteNonQuery(string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text, bool master = true)
        {
            using (var connection = CreateConnection(master))
            {
                return ExecuteNonQuery(connection, commandText, parameters, commandType);
            }
        }
        /// <summary>
        /// Execute INSERT or DELETE or UPDATE operation.
        /// <para>执行 INSERT 或 DELETE 或 UPDATE 操作。</para>
        /// </summary>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <param name="commandType">Command type</param>
        /// <returns>Returns the number of affected rows.</returns>
        public int ExecuteNonQuery(string connectionString, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            using (var connection = CreateConnection(connectionString))
            {
                return ExecuteNonQuery(connection, commandText, parameters, commandType);
            }
        }
        /// <summary>
        /// Execute INSERT or DELETE or UPDATE operation.
        /// <para>执行 INSERT 或 DELETE 或 UPDATE 操作。</para>
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns>Returns the number of affected rows.</returns>
        public int ExecuteNonQuery(IDbConnection connection, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            using (var command = CreateDbCommand(null, connection, commandType, commandText, parameters))
            {
                return command.ExecuteNonQuery(SqlMonitor);
            }
        }
        /// <summary>
        /// Execute INSERT or DELETE or UPDATE operation.
        /// <para>执行 INSERT 或 DELETE 或 UPDATE 操作。</para>
        /// </summary>
        /// <param name="transaction">Database transaction</param>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns>Returns the number of affected rows.</returns>
        public int ExecuteNonQuery(IDbTransaction transaction, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));

            using (var command = CreateDbCommand(transaction, null, commandType, commandText, parameters))
            {
                return command.ExecuteNonQuery(SqlMonitor);
            }
        }
        /// <summary>
        /// Execute INSERT or DELETE or UPDATE operation.
        /// <para>执行 INSERT 或 DELETE 或 UPDATE 操作。</para>
        /// </summary>
        /// <param name="sqlCommand"></param>
        /// <returns>Returns the number of affected rows.</returns>
        public int ExecuteNonQuery(ISqlCommand sqlCommand)
        {
            return ExecuteSqlCommand(sqlCommand, (command, _) => command.ExecuteNonQuery(SqlMonitor));
        }

        /// <summary>
        /// Execute INSERT or DELETE or UPDATE operation.
        /// <para>执行 INSERT 或 DELETE 或 UPDATE 操作。</para>
        /// </summary>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <param name="commandType">Command type</param>
        /// <param name="master">true: master database, false: slave database.</param>
        /// <returns>Returns the number of affected rows.</returns>
        public async Task<int> ExecuteNonQueryAsync(string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text, bool master = true)
        {
            using (var connection = CreateConnection(master))
            {
                return await ExecuteNonQueryAsync(connection, commandText, parameters, commandType);
            }
        }
        /// <summary>
        /// Execute INSERT or DELETE or UPDATE operation.
        /// <para>执行 INSERT 或 DELETE 或 UPDATE 操作。</para>
        /// </summary>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <param name="commandType">Command type</param>
        /// <returns>Returns the number of affected rows.</returns>
        public async Task<int> ExecuteNonQueryAsync(string connectionString, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            using (var connection = CreateConnection(connectionString))
            {
                return await ExecuteNonQueryAsync(connection, commandText, parameters, commandType);
            }
        }
        /// <summary>
        /// Execute INSERT or DELETE or UPDATE operation.
        /// <para>执行 INSERT 或 DELETE 或 UPDATE 操作。</para>
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns>Returns the number of affected rows.</returns>
        public async Task<int> ExecuteNonQueryAsync(IDbConnection connection, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            using (var command = CreateDbCommand(null, connection, commandType, commandText, parameters))
            {
                return await command.ExecuteNonQueryAsync(SqlMonitor);
            }
        }
        /// <summary>
        /// Execute INSERT or DELETE or UPDATE operation.
        /// <para>执行 INSERT 或 DELETE 或 UPDATE 操作。</para>
        /// </summary>
        /// <param name="transaction">Database transaction</param>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns>Returns the number of affected rows.</returns>
        public async Task<int> ExecuteNonQueryAsync(IDbTransaction transaction, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));

            using (var command = CreateDbCommand(transaction, null, commandType, commandText, parameters))
            {
                return await command.ExecuteNonQueryAsync(SqlMonitor);
            }
        }
        /// <summary>
        /// Execute INSERT or DELETE or UPDATE operation.
        /// <para>执行 INSERT 或 DELETE 或 UPDATE 操作。</para>
        /// </summary>
        /// <param name="sqlCommand"></param>
        /// <returns>Returns the number of affected rows.</returns>
        public async Task<int> ExecuteNonQueryAsync(ISqlCommand sqlCommand)
        {
            return await ExecuteSqlCommandAsync(sqlCommand, async (command, _) => await command.ExecuteNonQueryAsync(SqlMonitor));
        }
        #endregion

        #region ExecuteDataTable
        /// <summary>   
        /// Execute the query.
        /// <para>执行查询</para>
        /// </summary>   
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <param name="master">true: master database, false: slave database.</param>
        /// <returns></returns>
        public DataTable ExecuteDataTable(string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text, bool master = true)
        {
            using (var connection = CreateConnection(master))
            {
                return ExecuteDataTable(connection, commandText, parameters, commandType);
            }
        }
        /// <summary>   
        /// Execute the query.
        /// <para>执行查询</para>
        /// </summary>   
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns></returns>
        public DataTable ExecuteDataTable(string connectionString, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            using (var connection = CreateConnection(connectionString))
            {
                return ExecuteDataTable(connection, commandText, parameters, commandType);
            }
        }
        /// <summary>   
        /// Execute the query.
        /// <para>执行查询</para>
        /// </summary>   
        /// <param name="connection">Database connection</param>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns></returns>
        public DataTable ExecuteDataTable(IDbConnection connection, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            using (var command = CreateDbCommand(null, connection, commandType, commandText, parameters))
            {
                //using (var adapter = _providerFactory.CreateDataAdapter())
                {
                    return command.ExecuteDataTable(SqlMonitor/*, adapter*/);
                }
            }
        }
        /// <summary>   
        /// Execute the query.
        /// <para>执行查询</para>
        /// </summary>   
        /// <param name="transaction">Database transaction</param>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns></returns>
        public DataTable ExecuteDataTable(IDbTransaction transaction, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));

            using (var command = CreateDbCommand(transaction, null, commandType, commandText, parameters))
            {
                //using (var adapter = _providerFactory.CreateDataAdapter())
                {
                    return command.ExecuteDataTable(SqlMonitor/*, adapter*/);
                }
            }
        }
        /// <summary>
        /// Execute the query.
        /// <para>执行查询</para>
        /// </summary>
        /// <param name="sqlCommand"></param>
        /// <returns></returns>
        public DataTable ExecuteDataTable(ISqlCommand sqlCommand)
        {
            return ExecuteSqlCommand(sqlCommand, (command, _) =>
            {
                //using (var adapter = _providerFactory.CreateDataAdapter())
                {
                    return command.ExecuteDataTable(SqlMonitor/*, adapter*/);
                }
            });
        }

        /// <summary>   
        /// Execute the query.
        /// <para>执行查询</para>
        /// </summary>   
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <param name="master">true: master database, false: slave database.</param>
        /// <returns></returns>
        public async Task<DataTable> ExecuteDataTableAsync(string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text, bool master = true)
        {
            using (var connection = CreateConnection(master))
            {
                return await ExecuteDataTableAsync(connection, commandText, parameters, commandType);
            }
        }
        /// <summary>   
        /// Execute the query.
        /// <para>执行查询</para>
        /// </summary>   
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns></returns>
        public async Task<DataTable> ExecuteDataTableAsync(string connectionString, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            using (var connection = CreateConnection(connectionString))
            {
                return await ExecuteDataTableAsync(connection, commandText, parameters, commandType);
            }
        }
        /// <summary>   
        /// Execute the query.
        /// <para>执行查询</para>
        /// </summary>   
        /// <param name="connection">Database connection</param>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns></returns>
        public async Task<DataTable> ExecuteDataTableAsync(IDbConnection connection, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            using (var command = CreateDbCommand(null, connection, commandType, commandText, parameters))
            {
                //using (var adapter = _providerFactory.CreateDataAdapter())
                {
                    return await command.ExecuteDataTableAsync(SqlMonitor/*, adapter*/);
                }
            }
        }
        /// <summary>   
        /// Execute the query.
        /// <para>执行查询</para>
        /// </summary>   
        /// <param name="transaction">Database transaction</param>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns></returns>
        public async Task<DataTable> ExecuteDataTableAsync(IDbTransaction transaction, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));

            using (var command = CreateDbCommand(transaction, null, commandType, commandText, parameters))
            {
                //using (var adapter = _providerFactory.CreateDataAdapter())
                {
                    return await command.ExecuteDataTableAsync(SqlMonitor/*, adapter*/);
                }
            }
        }
        /// <summary>
        /// Execute the query.
        /// <para>执行查询</para>
        /// </summary>
        /// <param name="sqlCommand"></param>
        /// <returns></returns>
        public async Task<DataTable> ExecuteDataTableAsync(ISqlCommand sqlCommand)
        {
            return await ExecuteSqlCommandAsync(sqlCommand, async (command, _) =>
            {
                //using (var adapter = _providerFactory.CreateDataAdapter())
                {
                    return await command.ExecuteDataTableAsync(SqlMonitor/*, adapter*/);
                }
            });
        }
        #endregion

        #region ExecuteDataSet
        /// <summary>   
        /// Execute the query.
        /// <para>执行查询</para>
        /// </summary>   
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <param name="master">true: master database, false: slave database.</param>
        /// <returns></returns>
        public DataSet ExecuteDataSet(string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text, bool master = true)
        {
            using (var connection = CreateConnection(master))
            {
                return ExecuteDataSet(connection, commandText, parameters, commandType);
            }
        }
        /// <summary>   
        /// Execute the query.
        /// <para>执行查询</para>
        /// </summary>   
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns></returns>
        public DataSet ExecuteDataSet(string connectionString, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            using (var connection = CreateConnection(connectionString))
            {
                return ExecuteDataSet(connection, commandText, parameters, commandType);
            }
        }
        /// <summary>   
        /// Execute the query.
        /// <para>执行查询</para>
        /// </summary>   
        /// <param name="connection">Database connection</param>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>   
        /// <param name="parameters">Input parameters</param>
        /// <returns></returns>
        public DataSet ExecuteDataSet(IDbConnection connection, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            using (var command = CreateDbCommand(null, connection, commandType, commandText, parameters))
            {
                //using (var adapter = _providerFactory.CreateDataAdapter())
                {
                    return command.ExecuteDataSet(SqlMonitor/*, adapter*/);
                }
            }
        }
        /// <summary>   
        /// Execute the query.
        /// <para>执行查询</para>
        /// </summary>   
        /// <param name="transaction">Database transaction</param>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>   
        /// <param name="parameters">Input parameters</param>
        /// <returns></returns>
        public DataSet ExecuteDataSet(IDbTransaction transaction, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));

            using (var command = CreateDbCommand(transaction, null, commandType, commandText, parameters))
            {
                //using (var adapter = _providerFactory.CreateDataAdapter())
                {
                    return command.ExecuteDataSet(SqlMonitor/*, adapter*/);
                }
            }
        }
        /// <summary>
        /// Execute the query.
        /// <para>执行查询</para>
        /// </summary>
        /// <param name="sqlCommand"></param>
        /// <returns></returns>
        public DataSet ExecuteDataSet(ISqlCommand sqlCommand)
        {
            return ExecuteSqlCommand(sqlCommand, (command, _) =>
            {
                //using (var adapter = _providerFactory.CreateDataAdapter())
                {
                    return command.ExecuteDataSet(SqlMonitor/*, adapter*/);
                }
            });
        }

        /// <summary>   
        /// Execute the query.
        /// <para>执行查询</para>
        /// </summary>   
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <param name="master">true: master database, false: slave database.</param>
        /// <returns></returns>
        public async Task<DataSet> ExecuteDataSetAsync(string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text, bool master = true)
        {
            using (var connection = CreateConnection(master))
            {
                return await ExecuteDataSetAsync(connection, commandText, parameters, commandType);
            }
        }
        /// <summary>   
        /// Execute the query.
        /// <para>执行查询</para>
        /// </summary>   
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns></returns>
        public async Task<DataSet> ExecuteDataSetAsync(string connectionString, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            using (var connection = CreateConnection(connectionString))
            {
                return await ExecuteDataSetAsync(connection, commandText, parameters, commandType);
            }
        }
        /// <summary>   
        /// Execute the query.
        /// <para>执行查询</para>
        /// </summary>   
        /// <param name="connection">Database connection</param>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>   
        /// <param name="parameters">Input parameters</param>
        /// <returns></returns>
        public async Task<DataSet> ExecuteDataSetAsync(IDbConnection connection, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            using (var command = CreateDbCommand(null, connection, commandType, commandText, parameters))
            {
                //using (var adapter = _providerFactory.CreateDataAdapter())
                {
                    return await command.ExecuteDataSetAsync(SqlMonitor/*, adapter*/);
                }
            }
        }
        /// <summary>   
        /// Execute the query.
        /// <para>执行查询</para>
        /// </summary>   
        /// <param name="transaction">Database transaction</param>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>   
        /// <param name="parameters">Input parameters</param>
        /// <returns></returns>
        public async Task<DataSet> ExecuteDataSetAsync(IDbTransaction transaction, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));

            using (var command = CreateDbCommand(transaction, null, commandType, commandText, parameters))
            {
                //using (var adapter = _providerFactory.CreateDataAdapter())
                {
                    return await command.ExecuteDataSetAsync(SqlMonitor/*, adapter*/);
                }
            }
        }
        /// <summary>
        /// Execute the query.
        /// <para>执行查询</para>
        /// </summary>
        /// <param name="sqlCommand"></param>
        /// <returns></returns>
        public async Task<DataSet> ExecuteDataSetAsync(ISqlCommand sqlCommand)
        {
            return await ExecuteSqlCommandAsync(sqlCommand, async (command, _) =>
            {
                //using (var adapter = _providerFactory.CreateDataAdapter())
                {
                    return await command.ExecuteDataSetAsync(SqlMonitor/*, adapter*/);
                }
            });
        }
        #endregion

        #region ExecuteReader
        /// <summary>   
        /// Execute the query.
        /// <para>执行查询</para>
        /// </summary>   
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <param name="master">true: master database, false: slave database.</param>
        /// <returns></returns>
        public DbDataReader ExecuteReader(string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text, bool master = true)
        {
            var connection = CreateConnection(master);
            using (var command = CreateDbCommand(null, connection, commandType, commandText, parameters))
            {
                return command.ExecuteReader(CommandBehavior.CloseConnection, SqlMonitor);
            }
        }
        /// <summary>   
        /// Execute the query.
        /// <para>执行查询</para>
        /// </summary>   
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns></returns>
        public DbDataReader ExecuteReader(string connectionString, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            var connection = CreateConnection(connectionString);
            using (var command = CreateDbCommand(null, connection, commandType, commandText, parameters))
            {
                return command.ExecuteReader(CommandBehavior.CloseConnection, SqlMonitor);
            }
        }
        /// <summary>   
        /// Execute the query.
        /// <para>执行查询</para>
        /// </summary>   
        /// <param name="connection">Database connection</param>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns></returns>
        public DbDataReader ExecuteReader(IDbConnection connection, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            using (var command = CreateDbCommand(null, connection, commandType, commandText, parameters))
            {
                return command.ExecuteReader(CommandBehavior.Default, SqlMonitor);
            }
        }
        /// <summary>   
        /// Execute the query.
        /// <para>执行查询</para>
        /// </summary>   
        /// <param name="transaction">Database transaction</param>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns></returns>
        public DbDataReader ExecuteReader(IDbTransaction transaction, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));

            using (var command = CreateDbCommand(transaction, null, commandType, commandText, parameters))
            {
                return command.ExecuteReader(CommandBehavior.Default, SqlMonitor);
            }
        }
        /// <summary>
        /// Execute the query.
        /// <para>执行查询</para>
        /// </summary>
        /// <param name="sqlCommand"></param>
        /// <returns></returns>
        public DbDataReader ExecuteReader(ISqlCommand sqlCommand)
        {
            return ExecuteSqlCommand(sqlCommand, (command, isInternalConnection) => command.ExecuteReader(isInternalConnection ? CommandBehavior.CloseConnection : CommandBehavior.Default, SqlMonitor), false);
        }

        /// <summary>   
        /// Execute the query.
        /// <para>执行查询</para>
        /// </summary>   
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <param name="master">true: master database, false: slave database.</param>
        /// <returns></returns>
        public async Task<DbDataReader> ExecuteReaderAsync(string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text, bool master = true)
        {
            var connection = CreateConnection(master);
            using (var command = CreateDbCommand(null, connection, commandType, commandText, parameters))
            {
                return await command.ExecuteReaderAsync(CommandBehavior.CloseConnection, SqlMonitor);
            }
        }
        /// <summary>   
        /// Execute the query.
        /// <para>执行查询</para>
        /// </summary>   
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns></returns>
        public async Task<DbDataReader> ExecuteReaderAsync(string connectionString, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            var connection = CreateConnection(connectionString);
            using (var command = CreateDbCommand(null, connection, commandType, commandText, parameters))
            {
                return await command.ExecuteReaderAsync(CommandBehavior.CloseConnection, SqlMonitor);
            }
        }
        /// <summary>   
        /// Execute the query.
        /// <para>执行查询</para>
        /// </summary>   
        /// <param name="connection">Database connection</param>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns></returns>
        public async Task<DbDataReader> ExecuteReaderAsync(IDbConnection connection, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            using (var command = CreateDbCommand(null, connection, commandType, commandText, parameters))
            {
                return await command.ExecuteReaderAsync(CommandBehavior.Default, SqlMonitor);
            }
        }
        /// <summary>   
        /// Execute the query.
        /// <para>执行查询</para>
        /// </summary>   
        /// <param name="transaction">Database transaction</param>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns></returns>
        public async Task<DbDataReader> ExecuteReaderAsync(IDbTransaction transaction, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));

            using (var command = CreateDbCommand(transaction, null, commandType, commandText, parameters))
            {
                return await command.ExecuteReaderAsync(CommandBehavior.Default, SqlMonitor);
            }
        }
        /// <summary>
        /// Execute the query.
        /// <para>执行查询</para>
        /// </summary>
        /// <param name="sqlCommand"></param>
        /// <returns></returns>
        public async Task<DbDataReader> ExecuteReaderAsync(ISqlCommand sqlCommand)
        {
            return await ExecuteSqlCommandAsync(sqlCommand, async (command, isInternalConnection) => await command.ExecuteReaderAsync(isInternalConnection ? CommandBehavior.CloseConnection : CommandBehavior.Default, SqlMonitor), false);
        }
        #endregion

        #region ExecuteScalar
        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.
        /// <para>执行查询，并返回查询所返回的结果集中第一行的第一列。 所有其他的列和行将被忽略。</para>
        /// </summary>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <param name="master">true: master database, false: slave database.</param>
        /// <returns></returns>
        public object ExecuteScalar(string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text, bool master = true)
        {
            using (var connection = CreateConnection(master))
            {
                return ExecuteScalar(connection, commandText, parameters, commandType);
            }
        }
        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.
        /// <para>执行查询，并返回查询所返回的结果集中第一行的第一列。 所有其他的列和行将被忽略。</para>
        /// </summary>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns></returns>
        public object ExecuteScalar(string connectionString, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            using (var connection = CreateConnection(connectionString))
            {
                return ExecuteScalar(connection, commandText, parameters, commandType);
            }
        }
        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.
        /// <para>执行查询，并返回查询所返回的结果集中第一行的第一列。 所有其他的列和行将被忽略。</para>
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="commandType">Command type</param>
        /// <param name="sql">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns></returns>
        public object ExecuteScalar(IDbConnection connection, string sql, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            using (var command = CreateDbCommand(null, connection, commandType, sql, parameters))
            {
                return command.ExecuteScalar(SqlMonitor);
            }
        }
        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.
        /// <para>执行查询，并返回查询所返回的结果集中第一行的第一列。 所有其他的列和行将被忽略。</para>
        /// </summary>
        /// <param name="transaction">Database transaction</param>
        /// <param name="commandType">Command type</param>
        /// <param name="sql">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns></returns>
        public object ExecuteScalar(IDbTransaction transaction, string sql, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));

            using (var command = CreateDbCommand(transaction, null, commandType, sql, parameters))
            {
                return command.ExecuteScalar(SqlMonitor);
            }
        }
        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.
        /// <para>执行查询，并返回查询所返回的结果集中第一行的第一列。 所有其他的列和行将被忽略。</para>
        /// </summary>
        /// <param name="sqlCommand"></param>
        /// <returns></returns>
        public object ExecuteScalar(ISqlCommand sqlCommand)
        {
            return ExecuteSqlCommand(sqlCommand, (command, _) => command.ExecuteScalar(SqlMonitor));
        }

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.
        /// <para>执行查询，并返回查询所返回的结果集中第一行的第一列。 所有其他的列和行将被忽略。</para>
        /// </summary>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <param name="master">true: master database, false: slave database.</param>
        /// <returns></returns>
        public T ExecuteScalar<T>(string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text, bool master = true)
        {
            using (var connection = CreateConnection(master))
            {
                return ExecuteScalar<T>(connection, commandText, parameters, commandType);
            }
        }
        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.
        /// <para>执行查询，并返回查询所返回的结果集中第一行的第一列。 所有其他的列和行将被忽略。</para>
        /// </summary>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns></returns>
        public T ExecuteScalar<T>(string connectionString, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            using (var connection = CreateConnection(connectionString))
            {
                return ExecuteScalar<T>(connection, commandText, parameters, commandType);
            }
        }
        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.
        /// <para>执行查询，并返回查询所返回的结果集中第一行的第一列。 所有其他的列和行将被忽略。</para>
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns></returns>
        public T ExecuteScalar<T>(IDbConnection connection, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            var obj = ExecuteScalar(connection, commandText, parameters, commandType);
            return ObjectConvert.ChangeType<T>(obj);
        }
        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.
        /// <para>执行查询，并返回查询所返回的结果集中第一行的第一列。 所有其他的列和行将被忽略。</para>
        /// </summary>
        /// <param name="transaction">Database transaction</param>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns></returns>
        public T ExecuteScalar<T>(IDbTransaction transaction, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            var obj = ExecuteScalar(transaction, commandText, parameters, commandType);
            return ObjectConvert.ChangeType<T>(obj);
        }
        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.
        /// <para>执行查询，并返回查询所返回的结果集中第一行的第一列。 所有其他的列和行将被忽略。</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sqlCommand"></param>
        /// <returns></returns>
        public T ExecuteScalar<T>(ISqlCommand sqlCommand)
        {
            var obj = ExecuteScalar(sqlCommand);
            return ObjectConvert.ChangeType<T>(obj);
        }

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.
        /// <para>执行查询，并返回查询所返回的结果集中第一行的第一列。 所有其他的列和行将被忽略。</para>
        /// </summary>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <param name="master">true: master database, false: slave database.</param>
        /// <returns></returns>
        public async Task<object> ExecuteScalarAsync(string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text, bool master = true)
        {
            using (var connection = CreateConnection(master))
            {
                return await ExecuteScalarAsync(connection, commandText, parameters, commandType);
            }
        }
        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.
        /// <para>执行查询，并返回查询所返回的结果集中第一行的第一列。 所有其他的列和行将被忽略。</para>
        /// </summary>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns></returns>
        public async Task<object> ExecuteScalarAsync(string connectionString, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            using (var connection = CreateConnection(connectionString))
            {
                return await ExecuteScalarAsync(connection, commandText, parameters, commandType);
            }
        }
        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.
        /// <para>执行查询，并返回查询所返回的结果集中第一行的第一列。 所有其他的列和行将被忽略。</para>
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="commandType">Command type</param>
        /// <param name="sql">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns></returns>
        public async Task<object> ExecuteScalarAsync(IDbConnection connection, string sql, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            using (var command = CreateDbCommand(null, connection, commandType, sql, parameters))
            {
                return await command.ExecuteScalarAsync(SqlMonitor);
            }
        }
        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.
        /// <para>执行查询，并返回查询所返回的结果集中第一行的第一列。 所有其他的列和行将被忽略。</para>
        /// </summary>
        /// <param name="transaction">Database transaction</param>
        /// <param name="commandType">Command type</param>
        /// <param name="sql">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns></returns>
        public async Task<object> ExecuteScalarAsync(IDbTransaction transaction, string sql, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));

            using (var command = CreateDbCommand(transaction, null, commandType, sql, parameters))
            {
                return await command.ExecuteScalarAsync(SqlMonitor);
            }
        }
        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.
        /// <para>执行查询，并返回查询所返回的结果集中第一行的第一列。 所有其他的列和行将被忽略。</para>
        /// </summary>
        /// <param name="sqlCommand"></param>
        /// <returns></returns>
        public async Task<object> ExecuteScalarAsync(ISqlCommand sqlCommand)
        {
            return await ExecuteSqlCommandAsync(sqlCommand, async (command, _) => await command.ExecuteScalarAsync(SqlMonitor));
        }

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.
        /// <para>执行查询，并返回查询所返回的结果集中第一行的第一列。 所有其他的列和行将被忽略。</para>
        /// </summary>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <param name="master">true: master database, false: slave database.</param>
        /// <returns></returns>
        public async Task<T> ExecuteScalarAsync<T>(string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text, bool master = true)
        {
            using (var connection = CreateConnection(master))
            {
                return await ExecuteScalarAsync<T>(connection, commandText, parameters, commandType);
            }
        }
        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.
        /// <para>执行查询，并返回查询所返回的结果集中第一行的第一列。 所有其他的列和行将被忽略。</para>
        /// </summary>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns></returns>
        public async Task<T> ExecuteScalarAsync<T>(string connectionString, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            using (var connection = CreateConnection(connectionString))
            {
                return await ExecuteScalarAsync<T>(connection, commandText, parameters, commandType);
            }
        }
        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.
        /// <para>执行查询，并返回查询所返回的结果集中第一行的第一列。 所有其他的列和行将被忽略。</para>
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns></returns>
        public async Task<T> ExecuteScalarAsync<T>(IDbConnection connection, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            var obj = await ExecuteScalarAsync(connection, commandText, parameters, commandType);
            return ObjectConvert.ChangeType<T>(obj);
        }
        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.
        /// <para>执行查询，并返回查询所返回的结果集中第一行的第一列。 所有其他的列和行将被忽略。</para>
        /// </summary>
        /// <param name="transaction">Database transaction</param>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns></returns>
        public async Task<T> ExecuteScalarAsync<T>(IDbTransaction transaction, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            var obj = await ExecuteScalarAsync(transaction, commandText, parameters, commandType);
            return ObjectConvert.ChangeType<T>(obj);
        }
        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.
        /// <para>执行查询，并返回查询所返回的结果集中第一行的第一列。 所有其他的列和行将被忽略。</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sqlCommand"></param>
        /// <returns></returns>
        public async Task<T> ExecuteScalarAsync<T>(ISqlCommand sqlCommand)
        {
            var obj = await ExecuteScalarAsync(sqlCommand);
            return ObjectConvert.ChangeType<T>(obj);
        }
        #endregion

        #region Query<T>
        /// <summary>
        /// Query entities.
        /// </summary>
        /// <typeparam name="T">Returned entity type</typeparam>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <param name="master">true: master database, false: slave database.</param>
        /// <returns>Returns a collection of entity after query.</returns>
        public List<T> Query<T>(string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text, bool master = true)
        {
            using (var connection = CreateConnection(master))
            {
                return Query<T>(connection, commandText, parameters, commandType);
            }
        }
        /// <summary>
        /// Query entities.
        /// </summary>
        /// <typeparam name="T">Returned entity type</typeparam>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns>Returns a collection of entity after query.</returns>
        public List<T> Query<T>(string connectionString, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            using (var connection = CreateConnection(connectionString))
            {
                return Query<T>(connection, commandText, parameters, commandType);
            }
        }
        /// <summary>
        /// Query entities.
        /// </summary>
        /// <typeparam name="T">Returned entity type</typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>   
        /// <returns>Returns a collection of entity after query.</returns>
        public List<T> Query<T>(IDbConnection connection, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            //var table = ExecuteDataTable(connection, commandText, parameters, commandType);
            //return table?.ToList<T>(CaseSensitive);

            using (var dataReader = ExecuteReader(connection, commandText, parameters, commandType))
            {
                return dataReader.GetList<T>(CaseSensitive);
            }
        }
        /// <summary>
        /// Query entities.
        /// </summary>
        /// <typeparam name="T">Returned entity type</typeparam>
        /// <param name="transaction">Database transaction</param>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>   
        /// <returns>Returns a collection of entity after query.</returns>
        public List<T> Query<T>(IDbTransaction transaction, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));

            //var table = ExecuteDataTable(transaction, commandText, parameters, commandType);
            //return table?.ToList<T>(CaseSensitive);

            using (var dataReader = ExecuteReader(transaction, commandText, parameters, commandType))
            {
                return dataReader.GetList<T>(CaseSensitive);
            }
        }
        /// <summary>
        /// Query entities.
        /// </summary>
        /// <typeparam name="T">Returned entity type</typeparam>
        /// <param name="sqlCommand"></param>
        /// <returns>Returns a collection of entity after query.</returns>
        public List<T> Query<T>(ISqlCommand sqlCommand)
        {
            //var table = ExecuteDataTable(sqlCommand);
            //return table?.ToList<T>(CaseSensitive);

            using (var dataReader = ExecuteReader(sqlCommand))
            {
                return dataReader.GetList<T>(CaseSensitive);
            }
        }

        /// <summary>
        /// Query entities.
        /// </summary>
        /// <typeparam name="T">Returned entity type</typeparam>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <param name="master">true: master database, false: slave database.</param>
        /// <returns>Returns a collection of entity after query.</returns>
        public async Task<List<T>> QueryAsync<T>(string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text, bool master = true)
        {
            using (var connection = CreateConnection(master))
            {
                return await QueryAsync<T>(connection, commandText, parameters, commandType);
            }
        }
        /// <summary>
        /// Query entities.
        /// </summary>
        /// <typeparam name="T">Returned entity type</typeparam>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns>Returns a collection of entity after query.</returns>
        public async Task<List<T>> QueryAsync<T>(string connectionString, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            using (var connection = CreateConnection(connectionString))
            {
                return await QueryAsync<T>(connection, commandText, parameters, commandType);
            }
        }
        /// <summary>
        /// Query entities.
        /// </summary>
        /// <typeparam name="T">Returned entity type</typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>   
        /// <returns>Returns a collection of entity after query.</returns>
        public async Task<List<T>> QueryAsync<T>(IDbConnection connection, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            //var table = ExecuteDataTable(connection, commandText, parameters, commandType);
            //return table?.ToList<T>(CaseSensitive);

            using (var dataReader = await ExecuteReaderAsync(connection, commandText, parameters, commandType))
            {
                return await dataReader.GetListAsync<T>(CaseSensitive);
            }
        }
        /// <summary>
        /// Query entities.
        /// </summary>
        /// <typeparam name="T">Returned entity type</typeparam>
        /// <param name="transaction">Database transaction</param>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>   
        /// <returns>Returns a collection of entity after query.</returns>
        public async Task<List<T>> QueryAsync<T>(IDbTransaction transaction, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));

            //var table = ExecuteDataTable(transaction, commandText, parameters, commandType);
            //return table?.ToList<T>(CaseSensitive);

            using (var dataReader = await ExecuteReaderAsync(transaction, commandText, parameters, commandType))
            {
                return await dataReader.GetListAsync<T>(CaseSensitive);
            }
        }
        /// <summary>
        /// Query entities.
        /// </summary>
        /// <typeparam name="T">Returned entity type</typeparam>
        /// <param name="sqlCommand"></param>
        /// <returns>Returns a collection of entity after query.</returns>
        public async Task<List<T>> QueryAsync<T>(ISqlCommand sqlCommand)
        {
            //var table = await ExecuteDataTableAsync(sqlCommand);
            //return table?.ToList<T>(CaseSensitive);

            using (var dataReader = await ExecuteReaderAsync(sqlCommand))
            {
                return await dataReader.GetListAsync<T>(CaseSensitive);
            }
        }
        #endregion

        #region Get<T>
        /// <summary>
        /// Query single entity.
        /// </summary>
        /// <typeparam name="T">Returned entity type</typeparam>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <param name="master">true: master database, false: slave database.</param>
        /// <returns>Returns a single entity after query.</returns>
        public T Get<T>(string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text, bool master = true)
        {
            using (var connection = CreateConnection(master))
            {
                return Get<T>(connection, commandText, parameters, commandType);
            }
        }
        /// <summary>
        /// Query single entity.
        /// </summary>
        /// <typeparam name="T">Returned entity type</typeparam>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns>Returns a single entity after query.</returns>
        public T Get<T>(string connectionString, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            using (var connection = CreateConnection(connectionString))
            {
                return Get<T>(connection, commandText, parameters, commandType);
            }
        }
        /// <summary>
        /// Query single entity.
        /// </summary>
        /// <typeparam name="T">Returned entity type</typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>   
        /// <returns>Returns a single entity after query.</returns>
        public T Get<T>(IDbConnection connection, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            using (var dataReader = ExecuteReader(connection, commandText, parameters, commandType))
            {
                return dataReader.Get<T>(CaseSensitive);
            }
        }
        /// <summary>
        /// Query single entity.
        /// </summary>
        /// <typeparam name="T">Returned entity type</typeparam>
        /// <param name="transaction">Database transaction</param>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>   
        /// <returns>Returns a single entity after query.</returns>
        public T Get<T>(IDbTransaction transaction, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));

            using (var dataReader = ExecuteReader(transaction, commandText, parameters, commandType))
            {
                return dataReader.Get<T>(CaseSensitive);
            }
        }
        /// <summary>
        /// Query single entity.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sqlCommand"></param>
        /// <returns>Returns a single entity after query.</returns>
        public T Get<T>(ISqlCommand sqlCommand)
        {
            using (var dataReader = ExecuteReader(sqlCommand))
            {
                return dataReader.Get<T>(CaseSensitive);
            }
        }

        /// <summary>
        /// Query single entity.
        /// </summary>
        /// <typeparam name="T">Returned entity type</typeparam>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <param name="master">true: master database, false: slave database.</param>
        /// <returns>Returns a single entity after query.</returns>
        public async Task<T> GetAsync<T>(string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text, bool master = true)
        {
            using (var connection = CreateConnection(master))
            {
                return await GetAsync<T>(connection, commandText, parameters, commandType);
            }
        }
        /// <summary>
        /// Query single entity.
        /// </summary>
        /// <typeparam name="T">Returned entity type</typeparam>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns>Returns a single entity after query.</returns>
        public async Task<T> GetAsync<T>(string connectionString, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            using (var connection = CreateConnection(connectionString))
            {
                return await GetAsync<T>(connection, commandText, parameters, commandType);
            }
        }
        /// <summary>
        /// Query single entity.
        /// </summary>
        /// <typeparam name="T">Returned entity type</typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>   
        /// <returns>Returns a single entity after query.</returns>
        public async Task<T> GetAsync<T>(IDbConnection connection, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            using (var dataReader = await ExecuteReaderAsync(connection, commandText, parameters, commandType))
            {
                return await dataReader.GetAsync<T>(CaseSensitive);
            }
        }
        /// <summary>
        /// Query single entity.
        /// </summary>
        /// <typeparam name="T">Returned entity type</typeparam>
        /// <param name="transaction">Database transaction</param>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>   
        /// <returns>Returns a single entity after query.</returns>
        public async Task<T> GetAsync<T>(IDbTransaction transaction, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));

            using (var dataReader = await ExecuteReaderAsync(transaction, commandText, parameters, commandType))
            {
                return await dataReader.GetAsync<T>(CaseSensitive);
            }
        }
        /// <summary>
        /// Query single entity.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sqlCommand"></param>
        /// <returns>Returns a single entity after query.</returns>
        public async Task<T> GetAsync<T>(ISqlCommand sqlCommand)
        {
            using (var dataReader = await ExecuteReaderAsync(sqlCommand))
            {
                return await dataReader.GetAsync<T>(CaseSensitive);
            }
        }
        #endregion

        #region DbConnection
        /// <summary>
        /// Create a new connection without connection string.
        /// </summary>
        /// <returns></returns>
        public DbConnection CreateEmptyConnection()
        {
            return _providerFactory.CreateConnection();
        }

        /// <summary>
        /// Create a new connection with connection string.
        /// </summary>
        /// <param name="master">true: master database, false: slave database.</param>
        /// <returns></returns>
        public DbConnection CreateConnection(bool master = true)
        {
            return CreateConnection(_connectionSettings?.GetConnectionString(master));
        }
        /// <summary>
        /// Create a new connection with connection string.
        /// </summary>
        /// <param name="connectionString">Database connection string</param>
        /// <returns></returns>
        public DbConnection CreateConnection(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(connectionString));

            var connection = _providerFactory.CreateConnection();
            if (connection != null)
            {
                connection.ConnectionString = connectionString;
            }
            return connection;
        }

        /// <summary>
        /// Create and open a new connection.
        /// </summary>
        /// <param name="master">true: master database, false: slave database.</param>
        /// <returns></returns>
        public DbConnection OpenNewConnection(bool master = true)
        {
            var connection = CreateConnection(master);
            OpenConnection(connection);
            return connection;
        }
        /// <summary>
        /// Create and open a new connection.
        /// </summary>
        /// <param name="connectionString">Database connection string</param>
        /// <returns></returns>
        public DbConnection OpenNewConnection(string connectionString)
        {
            var connection = CreateConnection(connectionString);
            OpenConnection(connection);
            return connection;
        }

        /// <summary>
        /// Open the connection if the database connection is not opened.
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <returns></returns>
        public void OpenConnection(IDbConnection connection)
        {
            if (connection == null)
            {
                return;
            }

            if ((connection.State & ConnectionState.Open) != ConnectionState.Open)
            {
                connection.Open();
            }
        }

        /// <summary>
        /// Close the connection if the database connection is not closed.
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <returns></returns>
        public void CloseConnection(IDbConnection connection, bool disposeConnection = true)
        {
            if (connection == null)
            {
                return;
            }

            if (connection.State != ConnectionState.Closed)
            {
                connection.Close();
            }

            if (disposeConnection)
            {
                connection.Dispose();
            }
        }
        #endregion

        #region Private method
        private DbCommand CreateDbCommand(ISqlCommand sqlCommand)
        {
            return CreateDbCommand(sqlCommand.Transaction, sqlCommand.Connection, sqlCommand.CommandType, sqlCommand.Sql, SqlParameterUtil.ConvertToDbParameters(sqlCommand.Parameter, _providerFactory.CreateParameter), sqlCommand.CommandTimeout);
        }
        private DbCommand CreateDbCommand(IDbTransaction transaction, IDbConnection connection, CommandType commandType, string commandText, IEnumerable<DbParameter> parameters, int? commandTimeout = null)
        {
            IDbCommand command = _providerFactory.CreateCommand() ?? throw new Exception("Failed to create DbCommand.");
            command.Transaction = transaction;
            command.Connection = connection ?? transaction?.Connection;
            command.CommandType = commandType;
            command.CommandText = commandText;

            if (commandTimeout.HasValue)
            {
                command.CommandTimeout = commandTimeout.Value;
            }
            else if (CommandTimeout.HasValue)
            {
                command.CommandTimeout = CommandTimeout.Value;
            }

            if (parameters != null && parameters.Any())
            {
                foreach (var parameter in parameters)
                {
                    command.Parameters.Add(parameter);
                }
            }

            OpenConnection(command.Connection);

            return (DbCommand)command;
        }

        #region ExecuteSqlCommand
        private T ExecuteSqlCommand<T>(ISqlCommand sqlCommand, Func<DbCommand, bool, T> func, bool autoCloseInternalConnection = true)
        {
            if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

            if (func == null)
            {
                return default;
            }

            var useInternalConnection = false;
            using (var command = CreateDbCommand(sqlCommand))
            {
                DbConnection connection = null;
                try
                {
                    if (command.Connection == null)
                    {
                        useInternalConnection = true;
                        connection = OpenNewConnection(sqlCommand.Master);
                        command.Connection = connection;
                    }

                    return func(command, useInternalConnection);
                }
                finally
                {
                    if (useInternalConnection && autoCloseInternalConnection)
                    {
                        connection?.Dispose();
                    }
                }
            }
        }

        private async Task<T> ExecuteSqlCommandAsync<T>(ISqlCommand sqlCommand, Func<DbCommand, bool, Task<T>> func, bool autoCloseInternalConnection = true)
        {
            if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

            if (func == null)
            {
                return default;
            }

            var useInternalConnection = false;
            using (var command = CreateDbCommand(sqlCommand))
            {
                DbConnection connection = null;
                try
                {
                    if (command.Connection == null)
                    {
                        useInternalConnection = true;
                        connection = OpenNewConnection(sqlCommand.Master);
                        command.Connection = connection;
                    }

                    return await func(command, useInternalConnection);
                }
                finally
                {
                    if (useInternalConnection && autoCloseInternalConnection)
                    {
                        connection?.Dispose();
                    }
                }
            }
        }
        #endregion

        private void OnDbProviderFactoryChanged(DbProviderFactory providerFactory)
        {
            if (_providerFactory == providerFactory)
            {
                return;
            }

            _providerFactory = providerFactory;
            _dbType = _providerFactory.GetDatabaseType();
        }
        private void OnDatabaseTypeChanged(DatabaseType dbType)
        {
            if (_dbType == dbType)
            {
                return;
            }

            _dbType = dbType;
            _providerFactory = _dbType.GetDbProviderFactory();
        }
        private void OnConnectionStringChanged(MultiConnectionSettings connectionSettings)
        {
            _connectionSettings = connectionSettings ?? throw new ArgumentNullException(nameof(connectionSettings));

            if (_connectionSettings.IsEmpty)
            {
                _dbType = DatabaseType.Unknown;
                _providerFactory = null;
            }
            else
            {
                var providerFactory = _connectionSettings.ConnectionStrings.FirstOrDefault(c => c.ProviderFactory != null)?.ProviderFactory;
                if (providerFactory != null)// 1. 直接使用 DbProviderFactory
                {
                    ProviderFactory = providerFactory;
                }
                else
                {
                    var dbType = _connectionSettings.ConnectionStrings.FirstOrDefault(c => c.DbType != DatabaseType.Unknown)?.DbType;
                    if (dbType.HasValue && dbType.Value != DatabaseType.Unknown)// 2. 尝试通过 DatabaseType 获取 DbProviderFactory
                    {
                        DbType = dbType.Value;
                    }
                    else
                    {
                        var providerName = _connectionSettings.ConnectionStrings.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.ProviderName))?.ProviderName;
                        if (!string.IsNullOrWhiteSpace(providerName))// 3. 尝试通过 ProviderName 获取 DbProviderFactory
                        {
                            ProviderFactory = DbProviderFactoryManager.GetDbProviderFactory(providerName);
                        }
                    }
                }
            }
        }
        #endregion
    }
}
