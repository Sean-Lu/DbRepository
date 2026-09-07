using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Linq;
#if NETSTANDARD || NET5_0_OR_GREATER
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
#endif

namespace Sean.Core.DbRepository;

public class ConnectionStringOptions
{
    private ConnectionStringOptions()
    {
    }
    /// <summary>
    /// Creates an instance of <see cref="ConnectionStringOptions"/>.
    /// </summary>
    /// <param name="connectionString">Database connection string.</param>
    /// <param name="providerName">Database provider name.</param>
    /// <param name="master">true: master database, false: slave database.</param>
    /// <returns></returns>
    public ConnectionStringOptions(string connectionString, string providerName, bool master = true)
    {
        ConnectionString = connectionString;
        ProviderName = providerName;
        Master = master;
    }
    /// <summary>
    /// Creates an instance of <see cref="ConnectionStringOptions"/>.
    /// </summary>
    /// <param name="connectionString">Database connection string.</param>
    /// <param name="dbType">Database type.</param>
    /// <param name="master">true: master database, false: slave database.</param>
    /// <returns></returns>
    public ConnectionStringOptions(string connectionString, DatabaseType dbType, bool master = true)
    {
        ConnectionString = connectionString;
        DbType = dbType;
        Master = master;
    }
    /// <summary>
    /// Creates an instance of <see cref="ConnectionStringOptions"/>.
    /// </summary>
    /// <param name="connectionString">Database connection string.</param>
    /// <param name="providerFactory">Database provider factory.</param>
    /// <param name="master">true: master database, false: slave database.</param>
    /// <returns></returns>
    public ConnectionStringOptions(string connectionString, DbProviderFactory providerFactory, bool master = true)
    {
        ConnectionString = connectionString;
        ProviderFactory = providerFactory;
        Master = master;
    }

    /// <summary>
    /// Database connection name.
    /// </summary>
    public string ConnectionName { get; set; }
    /// <summary>
    /// Database connection string.
    /// </summary>
    public string ConnectionString { get; set; }
    /// <summary>
    /// Database provider name.
    /// </summary>
    public string ProviderName { get; set; }
    /// <summary>
    /// Database type.
    /// </summary>
    public DatabaseType DbType { get; set; }
    /// <summary>
    /// Database provider factory.
    /// </summary>
    public DbProviderFactory ProviderFactory { get; set; }
    /// <summary>
    /// true: master database, false: slave database.
    /// </summary>
    public bool Master { get; set; } = true;
    public bool IsValid => !string.IsNullOrWhiteSpace(ConnectionString) && (!string.IsNullOrWhiteSpace(ProviderName) || DbType != DatabaseType.Unknown || ProviderFactory != null);

    /// <summary>
    /// Creates an instance of <see cref="ConnectionStringOptions"/>.
    /// </summary>
    /// <param name="connectionString">Database connection string.</param>
    /// <param name="providerName">Database provider name.</param>
    /// <param name="master">true: master database, false: slave database.</param>
    /// <returns></returns>
    public static ConnectionStringOptions Create(string connectionString, string providerName, bool master = true)
    {
        return new ConnectionStringOptions(connectionString, providerName, master);
    }
    /// <summary>
    /// Creates an instance of <see cref="ConnectionStringOptions"/>.
    /// </summary>
    /// <param name="connectionString">Database connection string.</param>
    /// <param name="dbType">Database type.</param>
    /// <param name="master">true: master database, false: slave database.</param>
    /// <returns></returns>
    public static ConnectionStringOptions Create(string connectionString, DatabaseType dbType, bool master = true)
    {
        return new ConnectionStringOptions(connectionString, dbType, master);
    }
    /// <summary>
    /// Creates an instance of <see cref="ConnectionStringOptions"/>.
    /// </summary>
    /// <param name="connectionString">Database connection string.</param>
    /// <param name="providerFactory">Database provider factory.</param>
    /// <param name="master">true: master database, false: slave database.</param>
    /// <returns></returns>
    public static ConnectionStringOptions Create(string connectionString, DbProviderFactory providerFactory, bool master = true)
    {
        return new ConnectionStringOptions(connectionString, providerFactory, master);
    }
    /// <summary>
    /// Creates an instance of <see cref="ConnectionStringOptions"/>.
    /// </summary>
    /// <param name="connectionString">Database connection string. The value of ProviderName or DatabaseType must be set in the database connection string in order to properly match the <see cref="DbProviderFactory"/>.</param>
    /// <param name="master">true: master database, false: slave database.</param>
    /// <returns></returns>
    public static ConnectionStringOptions CreateFromConnectionString(string connectionString, bool master = true)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(connectionString));

        var result = new ConnectionStringOptions
        {
            ConnectionString = connectionString,
            Master = master,
        };

        result.ReloadFromConnectionString();

        return result;
    }
#if NETSTANDARD || NET5_0_OR_GREATER
    /// <summary>
    /// Creates an instance of <see cref="ConnectionStringOptions"/>.
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="connectionName">Database connection name.</param>
    /// <param name="master">true: master database, false: slave database.</param>
    /// <returns></returns>
    public static ConnectionStringOptions CreateFromConnectionName(IConfiguration configuration, string connectionName, bool master = true)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        if (string.IsNullOrWhiteSpace(connectionName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(connectionName));

        var result = new ConnectionStringOptions
        {
            ConnectionName = connectionName,
            Master = master
        };

        result.ReloadFromConnectionName(configuration);

        return result;
    }
    /// <summary>
    /// Creates a collection of instances of <see cref="ConnectionStringOptions"/>.
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="connectionName">Database connection name.</param>
    /// <returns></returns>
    public static List<ConnectionStringOptions> CreateMultiFromConnectionName(IConfiguration configuration, string connectionName)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        if (string.IsNullOrWhiteSpace(connectionName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(connectionName));

        var result = new List<ConnectionStringOptions>();

        if (connectionName == Constants.Master)
        {
            var listMaster = CreateMultiFromConnectionName(configuration, Constants.Master, true);
            if (listMaster != null && listMaster.Any())
            {
                result.AddRange(listMaster);

                var listSecondary = CreateMultiFromConnectionName(configuration, Constants.Secondary, false);
                if (listSecondary != null && listSecondary.Any())
                {
                    result.AddRange(listSecondary);
                }
            }
        }
        else
        {
            var connectionString = configuration.GetConnectionString(connectionName);
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                var connectionStringOptions = CreateFromConnectionName(configuration, connectionName, true);
                if (connectionStringOptions != null)
                {
                    result.Add(connectionStringOptions);
                }
            }
            else
            {
                var listMaster = CreateMultiFromConnectionName(configuration, $"{connectionName}.{Constants.Master}", true);
                if (listMaster != null && listMaster.Any())
                {
                    result.AddRange(listMaster);

                    var listSecondary = CreateMultiFromConnectionName(configuration, $"{connectionName}.{Constants.Secondary}", false);
                    if (listSecondary != null && listSecondary.Any())
                    {
                        result.AddRange(listSecondary);
                    }
                }
            }
        }

        return result;
    }
    /// <summary>
    /// Creates a collection of instances of <see cref="ConnectionStringOptions"/>.
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="connectionName">Database connection name.</param>
    /// <param name="master">true: master database, false: slave database.</param>
    /// <param name="maxCount"></param>
    /// <returns></returns>
    private static List<ConnectionStringOptions> CreateMultiFromConnectionName(IConfiguration configuration, string connectionName, bool master, int maxCount = 10)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        if (string.IsNullOrWhiteSpace(connectionName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(connectionName));

        var result = new List<ConnectionStringOptions>();
        var connectionString = configuration.GetConnectionString(connectionName);
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            result.Add(CreateFromConnectionName(configuration, connectionName, master));
        }
        else
        {
            for (var startIndex = 1; startIndex <= maxCount; startIndex++)
            {
                var multiConnectionName = $"{connectionName}{startIndex}";
                var multiConnectionString = configuration.GetConnectionString(multiConnectionName);
                if (string.IsNullOrWhiteSpace(multiConnectionString))
                {
                    break;
                }

                result.Add(CreateFromConnectionName(configuration, multiConnectionName, master));
            }
        }

        return result;
    }
#else
    /// <summary>
    /// Creates an instance of <see cref="ConnectionStringOptions"/>.
    /// </summary>
    /// <param name="connectionName">Database connection name.</param>
    /// <param name="master">true: master database, false: slave database.</param>
    /// <returns></returns>
    public static ConnectionStringOptions CreateFromConnectionName(string connectionName, bool master = true)
    {
        if (connectionName == null) throw new ArgumentNullException(nameof(connectionName));

        var result = new ConnectionStringOptions
        {
            ConnectionName = connectionName,
            Master = master
        };

        result.ReloadFromConnectionName();

        return result;
    }
    /// <summary>
    /// Creates a collection of instances of <see cref="ConnectionStringOptions"/>.
    /// </summary>
    /// <param name="connectionName">Database connection name.</param>
    /// <returns></returns>
    public static List<ConnectionStringOptions> CreateMultiFromConnectionName(string connectionName)
    {
        if (connectionName == null) throw new ArgumentNullException(nameof(connectionName));

        var result = new List<ConnectionStringOptions>();
        if (connectionName == Constants.Master)
        {
            var listMaster = CreateMultiFromConnectionName(Constants.Master, true);
            if (listMaster != null && listMaster.Any())
            {
                result.AddRange(listMaster);

                var listSecondary = CreateMultiFromConnectionName(Constants.Secondary, false);
                if (listSecondary != null && listSecondary.Any())
                {
                    result.AddRange(listSecondary);
                }
            }
        }
        else
        {
            if (ConfigurationManager.ConnectionStrings[connectionName] != null)
            {
                var connectionStringOptions = CreateFromConnectionName(connectionName, true);
                if (connectionStringOptions != null)
                {
                    result.Add(connectionStringOptions);
                }
            }
            else
            {
                var listMaster = CreateMultiFromConnectionName($"{connectionName}.{Constants.Master}", true);
                if (listMaster != null && listMaster.Any())
                {
                    result.AddRange(listMaster);

                    var listSecondary = CreateMultiFromConnectionName($"{connectionName}.{Constants.Secondary}", false);
                    if (listSecondary != null && listSecondary.Any())
                    {
                        result.AddRange(listSecondary);
                    }
                }
            }
        }

        return result;
    }
    private static List<ConnectionStringOptions> CreateMultiFromConnectionName(string connectionName, bool master, int maxCount = 10)
    {
        if (connectionName == null) throw new ArgumentNullException(nameof(connectionName));

        var result = new List<ConnectionStringOptions>();
        var connectionStringSettings = ConfigurationManager.ConnectionStrings[connectionName];
        if (connectionStringSettings != null)
        {
            result.Add(CreateFromConnectionName(connectionName, master));
        }
        else
        {
            for (var startIndex = 1; startIndex <= maxCount; startIndex++)
            {
                var multiConnectionName = $"{connectionName}{startIndex}";
                var multiConnectionStringSettings = ConfigurationManager.ConnectionStrings[multiConnectionName];
                if (multiConnectionStringSettings == null)
                {
                    break;
                }

                result.Add(CreateFromConnectionName(multiConnectionName, master));
            }
        }

        return result;
    }
#endif

    /// <summary>
    /// Parse the database connection string.
    /// </summary>
    /// <param name="connectionString">Database connection string. Example: "xxx;ProviderName=xxx" or "xxx;DatabaseType=xxx"</param>
    /// <param name="relConnectionString">Database connection string.</param>
    /// <param name="databaseType">Database type.</param>
    /// <param name="providerName">Database provider name.</param>
    /// <returns>Database connection string</returns>
    /// <remarks>
    /// 使用标准 ADO.NET 单引号/双引号规则；不解析 ODBC 大括号语法。
    /// 厂商专有格式应通过显式指定 providerName 或 providerFactory 的构造方法传入，不在串内追加 ORM 扩展。
    /// 提取扩展后可能规范化键名和引号格式，但保留实际参数值。
    /// </remarks>
    public static bool ParseConnectionString(string connectionString, out string relConnectionString, out DatabaseType databaseType, out string providerName)
    {
        relConnectionString = connectionString;
        databaseType = DatabaseType.Unknown;
        providerName = null;

        // 不含 ORM 扩展的连接串保持原样，厂商语法仍由实际驱动负责解析。
        if (string.IsNullOrEmpty(connectionString)
            || (connectionString.IndexOf(Constants.DatabaseType, StringComparison.OrdinalIgnoreCase) < 0
                && connectionString.IndexOf(Constants.ProviderName, StringComparison.OrdinalIgnoreCase) < 0))
        {
            return false;
        }

        // 标准 ADO.NET 解析器处理引号、转义及重复键，不能按分号直接拆分密码等字段。
        var builder = new ParsedConnectionStringBuilder { ConnectionString = connectionString };
        var hasDatabaseType = builder.ContainsParsedKey(Constants.DatabaseType);
        var hasProviderName = builder.ContainsParsedKey(Constants.ProviderName);
        if (!hasDatabaseType && !hasProviderName)
        {
            return false;
        }

        var parsedType = DatabaseType.Unknown;
        if (hasDatabaseType)
        {
            var value = builder.TryGetValue(Constants.DatabaseType, out var typeValue) ? (string)typeValue : null;
            // 保留合法数值和 Unknown；拒绝未定义值以及非 Flags 枚举的逗号组合，避免选错方言。
            if (string.IsNullOrWhiteSpace(value) || value.IndexOf(',') >= 0
                || !Enum.TryParse(value, true, out parsedType) || !Enum.IsDefined(typeof(DatabaseType), parsedType))
            {
                throw new ArgumentException("Invalid DatabaseType connection string extension.", nameof(connectionString));
            }
        }

        var parsedProvider = builder.TryGetValue(Constants.ProviderName, out var providerValue) ? (string)providerValue : null;
        if (hasProviderName && string.IsNullOrWhiteSpace(parsedProvider))
        {
            throw new ArgumentException("ProviderName connection string extension cannot be empty.", nameof(connectionString));
        }

        // 两项扩展必须同时移除；驱动选择仍沿用 DbFactory 的 DatabaseType 优先规则。
        builder.Remove(Constants.DatabaseType);
        builder.Remove(Constants.ProviderName);
        relConnectionString = builder.ConnectionString;
        databaseType = parsedType;
        providerName = parsedProvider;
        return true;
    }

    /// <summary>
    /// Converts the connection string to a dictionary.
    /// </summary>
    /// <param name="connectionString">Database connection string.</param>
    /// <returns></returns>
    /// <remarks>按标准 ADO.NET 语义解析：键不区分大小写，重复键取最后值，未引用的空值视为未设置。</remarks>
    public static Dictionary<string, string> GetConnectionDictionary(string connectionString)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return result;
        }

        var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
        foreach (string key in builder.Keys)
        {
            result.Add(key, (string)builder[key]);
        }

        return result;
    }

    /// <summary>
    /// Converts the dictionary to a connection string.
    /// </summary>
    /// <param name="dic"></param>
    /// <returns>Database connection string.</returns>
    public static string GetConnectionString(Dictionary<string, string> dic)
    {
        if (dic == null) throw new ArgumentNullException(nameof(dic));

        var builder = new DbConnectionStringBuilder();
        foreach (var keyValuePair in dic)
        {
            builder[keyValuePair.Key] = keyValuePair.Value;
        }
        return builder.ConnectionString;
    }

    /// <summary>
    /// 单次解析时记录空扩展键；标准解析器会通过 Remove 丢弃未加引号的空值。
    /// </summary>
    private sealed class ParsedConnectionStringBuilder : DbConnectionStringBuilder
    {
        private readonly HashSet<string> _removedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public bool ContainsParsedKey(string key) => ContainsKey(key) || _removedKeys.Contains(key);

        public override bool Remove(string keyword)
        {
            // 仅记录出现过的键，最终值仍由标准解析器决定，保证重复键最后一次赋值生效。
            _removedKeys.Add(keyword);
            return base.Remove(keyword);
        }
    }

    public void ReloadFromConnectionString()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            return;
        }

        if (ParseConnectionString(ConnectionString, out var relConnString, out var databaseType, out var providerName))
        {
            ConnectionString = relConnString;
            DbType = databaseType;
            ProviderName = providerName;
        }
    }

#if NETSTANDARD || NET5_0_OR_GREATER
    public void ReloadFromConnectionName(IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(ConnectionName))
        {
            return;
        }

        var connectionString = configuration.GetConnectionString(ConnectionName);
        if (!ParseConnectionString(connectionString, out var relConnString, out var databaseType, out var providerName))
        {
            // 保留原有优先级：全局类型、全局驱动、命名类型、命名驱动。
            databaseType = configuration.GetValue<DatabaseType>($"{Constants.DatabaseSettings}:{Constants.DatabaseType}", DatabaseType.Unknown);
            if (databaseType == DatabaseType.Unknown)
            {
                providerName = configuration.GetValue<string>($"{Constants.DatabaseSettings}:{Constants.ProviderName}", null);
                if (string.IsNullOrWhiteSpace(providerName))
                {
                    providerName = null;
                    databaseType = configuration.GetValue<DatabaseType>($"{Constants.DatabaseSettings}:{Constants.DatabaseTypes}:{ConnectionName}", DatabaseType.Unknown);
                    if (databaseType == DatabaseType.Unknown)
                    {
                        providerName = configuration.GetValue<string>($"{Constants.DatabaseSettings}:{Constants.ProviderNames}:{ConnectionName}", null);
                    }
                }
            }
        }

        // 解析全部成功后再发布新状态，同时清掉旧元数据，避免失败时半更新或继续使用旧驱动。
        ConnectionString = relConnString;
        DbType = databaseType;
        ProviderName = string.IsNullOrWhiteSpace(providerName) ? null : providerName;
    }
#else
    public void ReloadFromConnectionName()
    {
        if (string.IsNullOrWhiteSpace(ConnectionName))
        {
            return;
        }

        var connectionStringSettings = ConfigurationManager.ConnectionStrings[ConnectionName];
        if (connectionStringSettings != null)
        {
            ConnectionString = connectionStringSettings.ConnectionString;
            ProviderName = connectionStringSettings.ProviderName;
        }
    }
#endif
}
