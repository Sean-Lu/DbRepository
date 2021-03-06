using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
#if NETSTANDARD
using Microsoft.Extensions.Configuration;
#endif
using Sean.Core.DbRepository.Extensions;
using Sean.Utility.Format;

namespace Sean.Core.DbRepository
{
    /// <summary>
    /// Database factory
    /// </summary>
    public class DbFactory
    {
        /// <summary>
        /// 默认的执行超时时间（单位：秒）
        /// </summary>
        public static int? DefaultCommandTimeout { get; set; }
        /// <summary>
        /// 表字段映射匹配实体属性是否大小写敏感。默认值：false。
        /// </summary>
        public static bool CaseSensitive { get; set; } = false;

        /// <summary>
        /// <see cref="DbProviderFactory"/>
        /// </summary>
        public DbProviderFactory ProviderFactory
        {
            get => _providerFactory;
            set => OnDbProviderFactoryChanged(value);
        }
        /// <summary>
        /// 获取或设置数据库类型
        /// </summary>
        public DatabaseType DbType
        {
            get => _dbType;
            set => OnDatabaseTypeChanged(value);
        }
        /// <summary>
        /// 数据库连接配置（支持多数据库：主\从库）
        /// </summary>
        public MultiConnectionSettings ConnectionSettings
        {
            get => _connectionSettings;
            set => OnConnectionStringChanged(value);
        }

        private DbProviderFactory _providerFactory;
        private DatabaseType _dbType;
        private MultiConnectionSettings _connectionSettings;

        #region Constructors
#if NETSTANDARD
        /// <summary>
        /// Single or clustered database.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="configName">Configuration ConnectionStrings name</param>
        public DbFactory(IConfiguration configuration = null, string configName = Constants.Master)
        {
            if (string.IsNullOrWhiteSpace(configName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(configName));

            ConnectionSettings = new MultiConnectionSettings(configuration, configName);
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
        }
#endif
        /// <summary>
        /// Single or clustered database.
        /// </summary>
        /// <param name="connectionSettings"></param>
        public DbFactory(MultiConnectionSettings connectionSettings)
        {
            ConnectionSettings = connectionSettings ?? throw new ArgumentNullException(nameof(connectionSettings));
        }
        /// <summary>
        /// Single database.
        /// </summary>
        /// <param name="connString"></param>
        /// <param name="type"></param>
        public DbFactory(string connString, DatabaseType type) : this(new MultiConnectionSettings(new ConnectionStringOptions(connString, type)))
        {

        }
        /// <summary>
        /// Single database.
        /// </summary>
        /// <param name="connString"></param>
        /// <param name="factory"></param>
        public DbFactory(string connString, DbProviderFactory factory) : this(new MultiConnectionSettings(new ConnectionStringOptions(connString, factory)))
        {

        }
        /// <summary>
        /// Single database.
        /// </summary>
        /// <param name="connString"></param>
        /// <param name="providerName"></param>
        public DbFactory(string connString, string providerName) : this(new MultiConnectionSettings(new ConnectionStringOptions(connString, providerName)))
        {

        }
        #endregion

        #region ExecuteCommandInfo
        public T ExecuteCommandInfo<T>(DbCommandInfo commandInfo, Func<DbCommand, T> func)
        {
            if (commandInfo == null) throw new ArgumentNullException(nameof(commandInfo));
            if (func == null)
            {
                return default;
            }

            var shouldCloseConnection = false;
            if (commandInfo.Transaction == null && commandInfo.Connection == null)
            {
                commandInfo.Connection = CreateConnection();
                shouldCloseConnection = true;
            }

            try
            {
                using (var command = CreateDbCommand(commandInfo))
                {
                    return func(command);
                }
            }
            finally
            {
                if (shouldCloseConnection)
                {
                    CloseConnection(commandInfo.Connection);
                }
            }
        }
#if !NET40
        public async Task<T> ExecuteCommandInfoAsync<T>(DbCommandInfo commandInfo, Func<DbCommand, Task<T>> func)
        {
            if (commandInfo == null) throw new ArgumentNullException(nameof(commandInfo));
            if (func == null)
            {
                return default;
            }

            var shouldCloseConnection = false;
            if (commandInfo.Transaction == null && commandInfo.Connection == null)
            {
                commandInfo.Connection = CreateConnection();
                shouldCloseConnection = true;
            }

            try
            {
                using (var command = CreateDbCommand(commandInfo))
                {
                    return await func(command);
                }
            }
            finally
            {
                if (shouldCloseConnection)
                {
                    CloseConnection(commandInfo.Connection);
                }
            }
        }
#endif
        #endregion

        #region ExecuteNonQuery【增\删\改】
        /// <summary>   
        /// 执行 新增\删除\修改 操作，并返回受影响的行数
        /// </summary>   
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <param name="commandType">Command type</param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <returns></returns>
        public int ExecuteNonQuery(string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text, bool master = true)
        {
            using (var conn = CreateConnection(master))
            {
                return ExecuteNonQuery(conn, commandText, parameters, commandType);
            }
        }
        /// <summary>
        /// 执行 新增\删除\修改 操作，并返回受影响的行数
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns></returns>
        public int ExecuteNonQuery(IDbConnection connection, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            using (var command = CreateDbCommand(null, connection, commandType, commandText, parameters))
            {
                return command.ExecuteNonQuery();
            }
        }
        /// <summary>   
        /// 执行 新增\删除\修改 操作，并返回受影响的行数
        /// </summary>
        /// <param name="trans">Database transaction</param>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns></returns>
        public int ExecuteNonQuery(IDbTransaction trans, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            if (trans == null) throw new ArgumentNullException(nameof(trans));

            using (var command = CreateDbCommand(trans, null, commandType, commandText, parameters))
            {
                return command.ExecuteNonQuery();
            }
        }
        /// <summary>   
        /// 执行 新增\删除\修改 操作，并返回受影响的行数
        /// </summary>   
        /// <returns></returns>
        public int ExecuteNonQuery(DbCommandInfo commandInfo)
        {
            return ExecuteCommandInfo(commandInfo, command => command.ExecuteNonQuery());
        }
#if !NET40
        /// <summary>   
        /// 执行 新增\删除\修改 操作，并返回受影响的行数
        /// </summary>   
        /// <returns></returns>
        public async Task<int> ExecuteNonQueryAsync(DbCommandInfo commandInfo)
        {
            return await ExecuteCommandInfoAsync(commandInfo, async command => await command.ExecuteNonQueryAsync());
        }
#endif
        #endregion

        #region ExecuteDataTable【数据表】
        /// <summary>   
        /// 执行查询，并返回 <see cref="DataTable"/>
        /// </summary>   
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <returns></returns>
        public DataTable ExecuteDataTable(string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text, bool master = true)
        {
            using (var conn = CreateConnection(master))
            {
                return ExecuteDataTable(conn, commandText, parameters, commandType);
            }
        }
        /// <summary>   
        /// 执行查询，并返回 <see cref="DataTable"/>
        /// </summary>   
        /// <param name="connection">Database connection</param>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns></returns>
        public DataTable ExecuteDataTable(IDbConnection connection, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            var dataSet = ExecuteDataSet(connection, commandText, parameters, commandType);
            return dataSet != null && dataSet.Tables.Count > 0 ? dataSet.Tables[0] : null;
        }
        /// <summary>
        /// 执行查询，并返回 <see cref="DataTable"/>
        /// </summary>
        /// <param name="commandInfo"></param>
        /// <returns></returns>
        public DataTable ExecuteDataTable(DbCommandInfo commandInfo)
        {
            var dataSet = ExecuteDataSet(commandInfo);
            return dataSet != null && dataSet.Tables.Count > 0 ? dataSet.Tables[0] : null;
        }
#if !NET40
        /// <summary>
        /// 执行查询，并返回 <see cref="DataTable"/>
        /// </summary>
        /// <param name="commandInfo"></param>
        /// <returns></returns>
        public async Task<DataTable> ExecuteDataTableAsync(DbCommandInfo commandInfo)
        {
            var dataSet = await ExecuteDataSetAsync(commandInfo);
            return dataSet != null && dataSet.Tables.Count > 0 ? dataSet.Tables[0] : null;
        }
#endif
        #endregion

        #region ExecuteDataSet【数据集】
        /// <summary>   
        /// 执行查询，并返回 <see cref="DataSet"/>
        /// </summary>   
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <returns></returns>
        public DataSet ExecuteDataSet(string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text, bool master = true)
        {
            using (var conn = CreateConnection(master))
            {
                return ExecuteDataSet(conn, commandText, parameters, commandType);
            }
        }
        /// <summary>   
        /// 执行查询，并返回 <see cref="DataSet"/>
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
                using (var adapter = _providerFactory.CreateDataAdapter())
                {
                    if (adapter == null)
                    {
                        // 如果创建 DbDataAdapter 失败，则尝试通过 DbDataReader 读取器来获取数据
                        using (var reader = command.ExecuteReader(CommandBehavior.Default))
                        {
                            return reader.GetDataSet();
                        }
                    }
                    else
                    {
                        adapter.SelectCommand = command;
                        var result = new DataSet();
                        adapter.Fill(result);
                        return result;
                    }
                }
            }
        }
        /// <summary>
        /// 执行查询，并返回 <see cref="DataSet"/>
        /// </summary>
        /// <param name="commandInfo"></param>
        /// <returns></returns>
        public DataSet ExecuteDataSet(DbCommandInfo commandInfo)
        {
            return ExecuteCommandInfo(commandInfo, command =>
            {
                using (var adapter = _providerFactory.CreateDataAdapter())
                {
                    if (adapter == null)
                    {
                        // 如果创建 DbDataAdapter 失败，则尝试通过 DbDataReader 读取器来获取数据
                        using (var reader = command.ExecuteReader(CommandBehavior.Default))
                        {
                            return reader.GetDataSet();
                        }
                    }
                    else
                    {
                        adapter.SelectCommand = command;
                        var result = new DataSet();
                        adapter.Fill(result);
                        return result;
                    }
                }
            });
        }
#if !NET40
        /// <summary>
        /// 执行查询，并返回 <see cref="DataSet"/>
        /// </summary>
        /// <param name="commandInfo"></param>
        /// <returns></returns>
        public async Task<DataSet> ExecuteDataSetAsync(DbCommandInfo commandInfo)
        {
            return await ExecuteCommandInfoAsync(commandInfo, async command =>
            {
                using (var adapter = _providerFactory.CreateDataAdapter())
                {
                    if (adapter == null)
                    {
                        // 如果创建 DbDataAdapter 失败，则尝试通过 DbDataReader 读取器来获取数据
                        using (var reader = await command.ExecuteReaderAsync(CommandBehavior.Default))
                        {
                            return reader.GetDataSet();
                        }
                    }
                    else
                    {
                        adapter.SelectCommand = command;
                        var result = new DataSet();
                        adapter.Fill(result);
                        return result;
                    }
                }
            });
        }
#endif
        #endregion

        #region ExecuteReader【数据阅读器】
        /// <summary>   
        /// 执行查询，并返回 <see cref="DbDataReader"/>
        /// </summary>   
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <returns></returns>
        public IDataReader ExecuteReader(string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text, bool master = true)
        {
            var conn = CreateConnection(master);
            using (var command = CreateDbCommand(null, conn, commandType, commandText, parameters))
            {
                return command.ExecuteReader(CommandBehavior.CloseConnection);
            }
        }
        /// <summary>   
        /// 执行查询，并返回 <see cref="DbDataReader"/>
        /// </summary>   
        /// <param name="connection">Database connection</param>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns></returns>
        public IDataReader ExecuteReader(IDbConnection connection, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            using (var command = CreateDbCommand(null, connection, commandType, commandText, parameters))
            {
                return command.ExecuteReader(CommandBehavior.Default);
            }
        }
        /// <summary>
        /// 执行查询，并返回 <see cref="DbDataReader"/>
        /// </summary>
        /// <param name="commandInfo"></param>
        /// <returns></returns>
        public IDataReader ExecuteReader(DbCommandInfo commandInfo)
        {
            using (var command = CreateDbCommand(commandInfo))
            {
                return command.ExecuteReader(CommandBehavior.Default);
            }
        }
#if !NET40
        /// <summary>
        /// 执行查询，并返回 <see cref="DbDataReader"/>
        /// </summary>
        /// <param name="commandInfo"></param>
        /// <returns></returns>
        public async Task<IDataReader> ExecuteReaderAsync(DbCommandInfo commandInfo)
        {
            using (var command = CreateDbCommand(commandInfo))
            {
                return await command.ExecuteReaderAsync(CommandBehavior.Default);
            }
        }
#endif
        #endregion

        #region ExecuteScalar【返回结果集中的第一行第一列】
        /// <summary>
        /// 执行查询，并返回查询所返回的结果集中第一行的第一列。 所有其他的列和行将被忽略。
        /// </summary>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <returns></returns>
        public object ExecuteScalar(string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text, bool master = true)
        {
            using (var conn = CreateConnection(master))
            {
                return ExecuteScalar(conn, commandText, parameters, commandType);
            }
        }
        /// <summary>
        /// 执行查询，并返回查询所返回的结果集中第一行的第一列。 所有其他的列和行将被忽略。
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
                return command.ExecuteScalar();
            }
        }
        /// <summary>
        /// 执行查询，并返回查询所返回的结果集中第一行的第一列。 所有其他的列和行将被忽略。
        /// </summary>
        /// <param name="commandInfo"></param>
        /// <returns></returns>
        public object ExecuteScalar(DbCommandInfo commandInfo)
        {
            return ExecuteCommandInfo(commandInfo, command => command.ExecuteScalar());
        }

        /// <summary>
        /// 执行查询，并返回查询所返回的结果集中第一行的第一列。 所有其他的列和行将被忽略。
        /// </summary>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <returns></returns>
        public T ExecuteScalar<T>(string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text, bool master = true)
        {
            using (var conn = CreateConnection(master))
            {
                return ExecuteScalar<T>(conn, commandText, parameters, commandType);
            }
        }
        /// <summary>
        /// 执行查询，并返回查询所返回的结果集中第一行的第一列。 所有其他的列和行将被忽略。
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
        /// 执行查询，并返回查询所返回的结果集中第一行的第一列。 所有其他的列和行将被忽略。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="commandInfo"></param>
        /// <returns></returns>
        public T ExecuteScalar<T>(DbCommandInfo commandInfo)
        {
            var obj = ExecuteScalar(commandInfo);
            return ObjectConvert.ChangeType<T>(obj);
        }

#if !NET40
        /// <summary>
        /// 执行查询，并返回查询所返回的结果集中第一行的第一列。 所有其他的列和行将被忽略。
        /// </summary>
        /// <param name="commandInfo"></param>
        /// <returns></returns>
        public async Task<object> ExecuteScalarAsync(DbCommandInfo commandInfo)
        {
            return await ExecuteCommandInfoAsync(commandInfo, async command => await command.ExecuteScalarAsync());
        }
        /// <summary>
        /// 执行查询，并返回查询所返回的结果集中第一行的第一列。 所有其他的列和行将被忽略。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="commandInfo"></param>
        /// <returns></returns>
        public async Task<T> ExecuteScalarAsync<T>(DbCommandInfo commandInfo)
        {
            var obj = await ExecuteScalarAsync(commandInfo);
            return ObjectConvert.ChangeType<T>(obj);
        }
#endif
        #endregion

        #region GetList<T>
        /// <summary>
        /// Query multiple entity or special type value collections
        /// </summary>
        /// <typeparam name="T">Returned entity type</typeparam>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <returns>Entity or special type value list</returns>
        public List<T> GetList<T>(string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text, bool master = true)
        {
            using (var conn = CreateConnection(master))
            {
                return GetList<T>(conn, commandText, parameters, commandType);
            }
        }
        /// <summary>
        /// Query multiple entity or special type value collections
        /// </summary>
        /// <typeparam name="T">Returned entity type</typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>   
        /// <returns>Entity or special type value list</returns>
        public List<T> GetList<T>(IDbConnection connection, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
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
        /// Query multiple entity or special type value collections
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="commandInfo"></param>
        /// <returns>Entity or special type value list</returns>
        public List<T> GetList<T>(DbCommandInfo commandInfo)
        {
            //var table = ExecuteDataTable(commandInfo);
            //return table?.ToList<T>(CaseSensitive);

            using (var dataReader = ExecuteReader(commandInfo))
            {
                return dataReader.GetList<T>(CaseSensitive);
            }
        }
#if !NET40
        /// <summary>
        /// Query multiple entity or special type value collections
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="commandInfo"></param>
        /// <returns>Entity or special type value list</returns>
        public async Task<List<T>> GetListAsync<T>(DbCommandInfo commandInfo)
        {
            //var table = await ExecuteDataTableAsync(commandInfo);
            //return table?.ToList<T>(CaseSensitive);

            using (var dataReader = await ExecuteReaderAsync(commandInfo))
            {
                return dataReader.GetList<T>(CaseSensitive);
            }
        }
#endif
        #endregion

        #region Get<T>
        /// <summary>
        /// Query a single entity or special type value
        /// </summary>
        /// <typeparam name="T">Returned entity type</typeparam>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <returns>Entity or special type value</returns>
        public T Get<T>(string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text, bool master = true)
        {
            using (var conn = CreateConnection(master))
            {
                return Get<T>(conn, commandText, parameters, commandType);
            }
        }
        /// <summary>
        /// Query a single entity or special type value
        /// </summary>
        /// <typeparam name="T">Returned entity type</typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="commandType">Command type</param>
        /// <param name="commandText">Command text to be executed</param>
        /// <param name="parameters">Input parameters</param>   
        /// <returns>Entity or special type value</returns>
        public T Get<T>(IDbConnection connection, string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            using (var dataReader = ExecuteReader(connection, commandText, parameters, commandType))
            {
                return dataReader.Get<T>(CaseSensitive);
            }
        }
        /// <summary>
        /// Query a single entity or special type value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="commandInfo"></param>
        /// <returns>Entity or special type value</returns>
        public T Get<T>(DbCommandInfo commandInfo)
        {
            using (var dataReader = ExecuteReader(commandInfo))
            {
                return dataReader.Get<T>(CaseSensitive);
            }
        }
#if !NET40
        /// <summary>
        /// Query a single entity or special type value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="commandInfo"></param>
        /// <returns>Entity or special type value</returns>
        public async Task<T> GetAsync<T>(DbCommandInfo commandInfo)
        {
            using (var dataReader = await ExecuteReaderAsync(commandInfo))
            {
                return dataReader.Get<T>(CaseSensitive);
            }
        }
#endif
        #endregion

        #region DbConnection
        /// <summary>
        /// Create a new connection with default connection string
        /// </summary>
        /// <returns></returns>
        public IDbConnection CreateConnection()
        {
            return CreateConnection(true);
        }
        /// <summary>
        /// Create a new connection with default connection string
        /// </summary>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <returns></returns>
        public IDbConnection CreateConnection(bool master)
        {
            return CreateConnection(_connectionSettings?.GetConnectionString(master));
        }
        /// <summary>
        /// Create a new connection with specified connection string
        /// </summary>
        /// <param name="connectionString">Database connection string</param>
        /// <returns></returns>
        public IDbConnection CreateConnection(string connectionString)
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
        /// Create and open a new connection
        /// </summary>
        /// <returns></returns>
        public IDbConnection OpenConnection()
        {
            return OpenConnection(true);
        }
        /// <summary>
        /// Create and open a new connection
        /// </summary>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <returns></returns>
        public IDbConnection OpenConnection(bool master)
        {
            var connection = CreateConnection(master);
            OpenConnection(connection);
            return connection;
        }
        /// <summary>
        /// Create and open a new connection
        /// </summary>
        /// <param name="connectionString">Database connection string</param>
        /// <returns></returns>
        public IDbConnection OpenConnection(string connectionString)
        {
            var connection = CreateConnection(connectionString);
            OpenConnection(connection);
            return connection;
        }
        /// <summary>
        /// Open the connection if the database connection is not open
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <returns></returns>
        public IDbConnection OpenConnection(IDbConnection connection)
        {
            if (connection != null && (connection.State & ConnectionState.Open) != ConnectionState.Open)
            {
                connection.Open();
            }

            return connection;
        }

        /// <summary>
        /// Close the connection if the database connection is not closed
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <returns></returns>
        public void CloseConnection(IDbConnection connection)
        {
            if (connection != null)
            {
                if (connection.State != ConnectionState.Closed)
                {
                    connection.Close();
                }
                connection.Dispose();
            }
        }
        #endregion

        #region DbCommand
        public DbCommand CreateDbCommand(DbCommandInfo commandInfo)
        {
            return CreateDbCommand(commandInfo.Transaction, commandInfo.Connection, commandInfo.CommandType, commandInfo.CommandText, commandInfo.Parameters, commandInfo.CommandTimeout);
        }
        public DbCommand CreateDbCommand(IDbTransaction trans, IDbConnection connection, CommandType commandType, string commandText, IEnumerable<DbParameter> parameters, int? commandTimeout = null)
        {
            IDbCommand command = _providerFactory.CreateCommand() ?? throw new Exception("Failed to create DbCommand.");
            command.Transaction = trans;
            command.Connection = connection ?? trans?.Connection;
            command.CommandType = commandType;
            command.CommandText = commandText;

            if (commandTimeout.HasValue)
            {
                command.CommandTimeout = commandTimeout.Value;
            }
            else if (DefaultCommandTimeout.HasValue)
            {
                command.CommandTimeout = DefaultCommandTimeout.Value;
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
        #endregion

        #region Private Methods
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
            _connectionSettings = connectionSettings;

            if (_connectionSettings?.ConnectionStrings == null || !_connectionSettings.ConnectionStrings.Any())
            {
                _dbType = DatabaseType.Unknown;
                _providerFactory = null;
            }
            else
            {
                var providerFactory = _connectionSettings.ConnectionStrings.FirstOrDefault(c => c.ProviderFactory != null)?.ProviderFactory;
                if (providerFactory != null)// 1. 直接指定 DbProviderFactory：简单粗暴，直截了当，不需要额外配置数据库驱动映射关系，也支持任何实现了 DbProviderFactory 的关系型数据库。
                {
                    ProviderFactory = providerFactory;
                }
                else
                {
                    var dbType = _connectionSettings.ConnectionStrings.FirstOrDefault(c => c.DbType != DatabaseType.Unknown)?.DbType;
                    if (dbType.HasValue && dbType.Value != DatabaseType.Unknown)// 2. 通过 DatabaseType 映射获取到 DbProviderFactory：需要配置数据库驱动映射关系
                    {
                        DbType = dbType.Value;
                    }
                    else
                    {
                        var providerName = _connectionSettings.ConnectionStrings.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.ProviderName))?.ProviderName;
                        if (!string.IsNullOrWhiteSpace(providerName))// 3. 通过 ProviderName 映射获取到 DbProviderFactory：需要配置数据库驱动映射关系
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
