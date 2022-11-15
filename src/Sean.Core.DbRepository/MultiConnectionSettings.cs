using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
#if NETSTANDARD
using Microsoft.Extensions.Configuration;
#endif

namespace Sean.Core.DbRepository
{
    /// <summary>
    /// Single or clustered database settings.
    /// </summary>
    public class MultiConnectionSettings
    {
        /// <summary>
        /// Database connection strings. The clustered database configuration name suffix is a number starting with 1, example: xxx.master (primary database), xxx.secondary1 (secondary database), xxx.secondary2 (secondary database).
        /// </summary>
        public List<ConnectionStringOptions> ConnectionStrings => _connectionStrings;

#if NETSTANDARD
        private readonly IConfiguration _configuration;
        private readonly List<IConfigurationSection> _connSections;
#endif
        private readonly List<ConnectionStringOptions> _connectionStrings;
        private int _times;

        #region Constructors
        /// <summary>
        /// Single or clustered database.
        /// </summary>
        /// <param name="configName">Configuration ConnectionStrings name</param>
#if NETSTANDARD
        public MultiConnectionSettings(IConfiguration configuration = null, string configName = Constants.Master)
#else
        public MultiConnectionSettings(string configName = Constants.Master)
#endif
        {
            if (string.IsNullOrWhiteSpace(configName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(configName));

            _connectionStrings = new List<ConnectionStringOptions>();

#if NETSTANDARD
            _configuration = configuration ?? new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", true, true)
                .AddEnvironmentVariables()
                .Build();
            _connSections = _configuration.GetSection("ConnectionStrings")?.GetChildren()?.ToList();
#endif

            if (configName == Constants.Master)
            {
                AddConnStringFromConfiguration(Constants.Master, true);
                AddConnStringFromConfiguration(Constants.Secondary, false);
            }
            else
            {
                string originalConnString = null;
#if NETSTANDARD
                originalConnString = _configuration.GetConnectionString(configName);
#else
                originalConnString = ConfigurationManager.ConnectionStrings[configName]?.ConnectionString;
#endif
                if (!string.IsNullOrWhiteSpace(originalConnString))
                {
                    string providerName = null;
#if !NETSTANDARD
                    providerName = ConfigurationManager.ConnectionStrings[configName]?.ProviderName;
#endif
                    AddConnectionString(new ConnectionStringOptions(originalConnString, providerName, true));
                }
                else
                {
                    AddConnStringFromConfiguration($"{configName}.{Constants.Master}", true);
                    AddConnStringFromConfiguration($"{configName}.{Constants.Secondary}", false);
                }
            }
        }
        /// <summary>
        /// Single or clustered database.
        /// </summary>
        /// <param name="list"></param>
        public MultiConnectionSettings(IEnumerable<ConnectionStringOptions> list)
        {
            _connectionStrings = new List<ConnectionStringOptions>();

            if (list != null)
            {
                foreach (var options in list)
                {
                    AddConnectionString(options);
                }
            }
        }
        /// <summary>
        /// Single database.
        /// </summary>
        /// <param name="options"></param>
        public MultiConnectionSettings(ConnectionStringOptions options)
        {
            _connectionStrings = new List<ConnectionStringOptions>();

            AddConnectionString(options);
        }
        #endregion

        /// <summary>
        /// Clear all database connection strings.
        /// </summary>
        public void ClearConnectionStrings()
        {
            _connectionStrings.Clear();
        }

        /// <summary>
        /// Adds database connection string.
        /// </summary>
        /// <param name="options"></param>
        public void AddConnectionString(ConnectionStringOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrWhiteSpace(options.ConnectionString))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(options.ConnectionString));

            if (string.IsNullOrWhiteSpace(options.ProviderName) && options.DbType == DatabaseType.Unknown && options.ProviderFactory == null)
            {
                ParseConnectionString(options.ConnectionString, out var relConnString, out var databaseType, out var providerName);
                options.ConnectionString = relConnString;
                options.DbType = databaseType;
                options.ProviderName = providerName;
                if (string.IsNullOrWhiteSpace(options.ProviderName) && options.DbType == DatabaseType.Unknown)
                {
                    //throw new Exception($"无效的数据库连接字符串[{options.ConnectionString}]，请设置[{Constants.ProviderName}]或[{Constants.DatabaseType}]的值。");
                    throw new Exception($"Invalid database connection string [{options.ConnectionString}], please set the value of [{Constants.ProviderName}] or [{Constants.DatabaseType}].");
                }
            }

            _connectionStrings.Add(options);
        }
        /// <summary>
        /// Gets the database connection string.
        /// </summary>
        /// <param name="master">true: master database, false: slave database.</param>
        public string GetConnectionString(bool master = true)
        {
            #region 单数据库连接配置
            if (_connectionStrings.Count <= 1)
            {
                return _connectionStrings.FirstOrDefault()?.ConnectionString;
            }
            #endregion

            #region 多数据库连接配置
            if (!master && _connectionStrings.Count(c => c.Master == master) < 1)
            {
                // 如果没有从库，则默认使用主库
                master = true;
            }

            if (_connectionStrings.Count(c => c.Master == master) < 2)
            {
                return _connectionStrings.FirstOrDefault(c => c.Master == master)?.ConnectionString;
            }

            var list = _connectionStrings.Where(c => c.Master == master).ToList();
            var connString = list[Interlocked.Increment(ref _times) % list.Count]?.ConnectionString;
            return connString;
            #endregion
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionString">
        /// Example: "xxx;ProviderName=xxx" or "xxx;DatabaseType=xxx"
        /// </param>
        /// <param name="relConnString"></param>
        /// <param name="databaseType"></param>
        /// <param name="providerName"></param>
        /// <returns></returns>
        public static void ParseConnectionString(string connectionString, out string relConnString, out DatabaseType databaseType, out string providerName)
        {
            relConnString = null;
            databaseType = DatabaseType.Unknown;
            providerName = null;

            if (connectionString.Contains(Constants.ProviderName))
            {
                var dic = GetConnectionDictionary(connectionString);
                providerName = dic?.FirstOrDefault(c => c.Key == Constants.ProviderName).Value;
                dic?.Remove(Constants.ProviderName);
                relConnString = GetConnectionString(dic);
            }
            else if (connectionString.Contains(Constants.DatabaseType))
            {
                var dic = GetConnectionDictionary(connectionString);
                var value = dic?.FirstOrDefault(c => c.Key == Constants.DatabaseType).Value;
                if (Enum.TryParse<DatabaseType>(value, out var dbType))
                {
                    databaseType = dbType;
                }
                dic?.Remove(Constants.DatabaseType);
                relConnString = GetConnectionString(dic);
            }
            else
            {
                relConnString = connectionString;
            }
        }

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

        public static string GetConnectionString(Dictionary<string, string> dic)
        {
            var list = new List<string>();
            foreach (var keyValuePair in dic)
            {
                list.Add($"{keyValuePair.Key}={keyValuePair.Value}");
            }
            return string.Join(";", list);
        }

        private void AddConnStringFromConfiguration(string connStringKeyPrefix, bool master)
        {
            var maxCount = 10;
#if NETSTANDARD
            var originalConnString = _configuration.GetConnectionString(connStringKeyPrefix);
            if (!string.IsNullOrWhiteSpace(originalConnString))
            {
                AddConnectionString(new ConnectionStringOptions(originalConnString, master));
            }
            else if (_connSections != null && _connSections.Any())
            {
                for (var i = 0; i < maxCount; i++)
                {
                    var section = _connSections.Find(c => c.Key == $"{connStringKeyPrefix}{i + 1}");
                    if (string.IsNullOrWhiteSpace(section?.Value)) break;
                    AddConnectionString(new ConnectionStringOptions(section.Value, master));
                }
            }
#else
            var originalConnString = ConfigurationManager.ConnectionStrings[connStringKeyPrefix]?.ConnectionString;
            if (!string.IsNullOrWhiteSpace(originalConnString))
            {
                AddConnectionString(new ConnectionStringOptions(originalConnString, ConfigurationManager.ConnectionStrings[connStringKeyPrefix]?.ProviderName, master));
            }
            else if (ConfigurationManager.ConnectionStrings.Count > 0)
            {
                for (var i = 0; i < maxCount; i++)
                {
                    originalConnString = ConfigurationManager.ConnectionStrings[$"{connStringKeyPrefix}{i + 1}"]?.ConnectionString;
                    if (string.IsNullOrWhiteSpace(originalConnString)) break;
                    AddConnectionString(new ConnectionStringOptions(originalConnString, ConfigurationManager.ConnectionStrings[$"{connStringKeyPrefix}{i + 1}"]?.ProviderName, master));
                }
            }
#endif
        }
    }
}
