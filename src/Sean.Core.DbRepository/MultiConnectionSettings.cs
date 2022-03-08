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
        /// 数据库连接字符串，可以配置多个，后缀是以1开始的数字，示例：xxx.master（主库）、xxx.secondary1（从库1）、xxx.secondary2（从库2）
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
        /// 清空所有数据库连接字符串
        /// </summary>
        public void ClearConnectionStrings()
        {
            _connectionStrings.Clear();
        }

        /// <summary>
        /// 添加数据库连接字符串
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
                if (string.IsNullOrWhiteSpace(options.ProviderName) && options.DbType == DatabaseType.Unknown)
                {
                    throw new Exception($"无效的数据库连接配置[{options.ConnectionString}]，请设置[{Constants.ConfigurationProviderName}]或[{Constants.ConfigurationDatabaseType}]的值。");
                }
                options.ConnectionString = relConnString;
                options.DbType = databaseType;
                options.ProviderName = providerName;
            }

            _connectionStrings.Add(options);
        }
        /// <summary>
        /// 获取数据库连接字符串
        /// </summary>
        /// <param name="master">true:主库，false:从库</param>
        public string GetConnectionString(bool master = true)
        {
            if (!master && _connectionStrings.Count(c => !c.Master) < 1)
            {
                // 如果没有从库配置，则默认取主库配置
                master = true;
            }

            // 单连接配置
            if (_connectionStrings.Count <= 1)
            {
                var options = _connectionStrings.FirstOrDefault();
                return options != null && options.Master == master ? options.ConnectionString : string.Empty;
            }

            // 多连接配置
            var list = _connectionStrings.Where(c => c.Master == master).ToList();
            var connString = list.Count < 2 ? list.FirstOrDefault()?.ConnectionString : list[Interlocked.Increment(ref _times) % list.Count]?.ConnectionString;
            return connString;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionString">示例："xxx;ProviderName=xxx" 或 "xxx;DatabaseType=xxx"</param>
        /// <param name="relConnString"></param>
        /// <param name="databaseType"></param>
        /// <param name="providerName"></param>
        /// <returns></returns>
        public static void ParseConnectionString(string connectionString, out string relConnString, out DatabaseType databaseType, out string providerName)
        {
            relConnString = null;
            databaseType = DatabaseType.Unknown;
            providerName = null;

            if (connectionString.Contains(Constants.ConfigurationProviderName))
            {
                var dic = GetConnectionDictionary(connectionString);
                providerName = dic?.FirstOrDefault(c => c.Key == Constants.ConfigurationProviderName).Value;
                dic?.Remove(Constants.ConfigurationProviderName);
                relConnString = GetConnectionString(dic);
            }
            else if (connectionString.Contains(Constants.ConfigurationDatabaseType))
            {
                var dic = GetConnectionDictionary(connectionString);
                var value = dic?.FirstOrDefault(c => c.Key == Constants.ConfigurationDatabaseType).Value;
                if (Enum.TryParse<DatabaseType>(value, out var dbType))
                {
                    databaseType = dbType;
                }
                dic?.Remove(Constants.ConfigurationDatabaseType);
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
