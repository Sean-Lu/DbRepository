using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Linq;
#if NETSTANDARD || NET5_0_OR_GREATER
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
#endif

namespace Sean.Core.DbRepository
{
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
        public static bool ParseConnectionString(string connectionString, out string relConnectionString, out DatabaseType databaseType, out string providerName)
        {
            databaseType = DatabaseType.Unknown;
            providerName = null;

            if (!string.IsNullOrEmpty(connectionString) && (connectionString.Contains(Constants.DatabaseType) || connectionString.Contains(Constants.ProviderName)))
            {
                var dic = GetConnectionDictionary(connectionString);
                if (dic != null)
                {
                    if (dic.ContainsKey(Constants.DatabaseType))
                    {
                        var value = dic[Constants.DatabaseType];
                        if (Enum.TryParse<DatabaseType>(value, out var dbType))
                        {
                            databaseType = dbType;
                            dic.Remove(Constants.DatabaseType);
                            relConnectionString = GetConnectionString(dic);
                            return true;
                        }
                    }

                    if (dic.ContainsKey(Constants.ProviderName))
                    {
                        providerName = dic[Constants.ProviderName];
                        dic.Remove(Constants.ProviderName);
                        relConnectionString = GetConnectionString(dic);
                        return true;
                    }
                }
            }

            relConnectionString = connectionString;
            return false;
        }

        /// <summary>
        /// Converts the connection string to a dictionary.
        /// </summary>
        /// <param name="connectionString">Database connection string.</param>
        /// <returns></returns>
        public static Dictionary<string, string> GetConnectionDictionary(string connectionString)
        {
            var result = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return result;
            }

            connectionString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList().ForEach(c =>
            {
                if (string.IsNullOrWhiteSpace(c)) return;
                var index = c.IndexOf('=');
                var key = c.Substring(0, index).Trim();
                var value = c.Substring(index + 1).Trim();
                result.Add(key, value);
            });

            return result;
        }

        /// <summary>
        /// Converts the dictionary to a connection string.
        /// </summary>
        /// <param name="dic"></param>
        /// <returns>Database connection string.</returns>
        public static string GetConnectionString(Dictionary<string, string> dic)
        {
            var list = new List<string>();
            foreach (var keyValuePair in dic)
            {
                list.Add($"{keyValuePair.Key}={keyValuePair.Value}");
            }
            return string.Join(";", list);
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
            if (ParseConnectionString(connectionString, out var relConnString, out var databaseType, out var providerName))
            {
                ConnectionString = relConnString;
                DbType = databaseType;
                ProviderName = providerName;
                return;
            }
            ConnectionString = connectionString;

            var databaseTypeFromConfig = configuration.GetValue<DatabaseType>($"{Constants.DatabaseSettings}:{Constants.DatabaseType}", DatabaseType.Unknown);
            if (databaseTypeFromConfig != DatabaseType.Unknown)
            {
                DbType = databaseTypeFromConfig;
                return;
            }

            var providerNameFromConfig = configuration.GetValue<string>($"{Constants.DatabaseSettings}:{Constants.ProviderName}", null);
            if (!string.IsNullOrWhiteSpace(providerNameFromConfig))
            {
                ProviderName = providerNameFromConfig;
                return;
            }

            databaseTypeFromConfig = configuration.GetValue<DatabaseType>($"{Constants.DatabaseSettings}:{Constants.DatabaseTypes}:{ConnectionName}", DatabaseType.Unknown);
            if (databaseTypeFromConfig != DatabaseType.Unknown)
            {
                DbType = databaseTypeFromConfig;
                return;
            }

            providerNameFromConfig = configuration.GetValue<string>($"{Constants.DatabaseSettings}:{Constants.ProviderNames}:{ConnectionName}", null);
            if (!string.IsNullOrWhiteSpace(providerNameFromConfig))
            {
                ProviderName = providerNameFromConfig;
                return;
            }
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
}