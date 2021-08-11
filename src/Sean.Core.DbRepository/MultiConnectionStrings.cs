﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using Sean.Utility.Extensions;
#if NETSTANDARD
using Microsoft.Extensions.Configuration;
#endif

namespace Sean.Core.DbRepository
{
    public class MultiConnectionStrings
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

#if NETSTANDARD
        public MultiConnectionStrings(IConfiguration configuration = null, string configName = Constants.Master)
#else
        public MultiConnectionStrings(string configName = Constants.Master)
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

        public MultiConnectionStrings(List<ConnectionStringOptions> list)
        {
            _connectionStrings = list ?? throw new ArgumentNullException(nameof(list));
        }

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

            if (string.IsNullOrWhiteSpace(options.ProviderName) && options.DbType == DatabaseType.Unknown)
            {
                options.ConnectionString = GetValidConnectionString(options.ConnectionString, out var databaseType, out var providerName);
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
                master = true;
            }
            var list = _connectionStrings.Where(c => c.Master == master).ToList();
            var connString = list.Count < 2 ? list.FirstOrDefault()?.ConnectionString : list[Interlocked.Increment(ref _times) % list.Count]?.ConnectionString;
            return connString;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionString">"xxx;ProviderName=xxx" 或 "xxx;DatabaseType=xxx"</param>
        /// <param name="databaseType"></param>
        /// <param name="providerName"></param>
        /// <returns></returns>
        public string GetValidConnectionString(string connectionString, out DatabaseType databaseType, out string providerName)
        {
            databaseType = DatabaseType.Unknown;
            providerName = null;

            string validConnString;
            if (connectionString.Contains(Constants.ConfigurationProviderName))
            {
                var dic = GetConnectionDictionary(connectionString);
                providerName = dic?.FirstOrDefault(c => c.Key == Constants.ConfigurationProviderName).Value;
                dic?.Remove(Constants.ConfigurationProviderName);
                validConnString = GetConnectionString(dic);
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
                validConnString = GetConnectionString(dic);
            }
            else
            {
                validConnString = connectionString;
            }

            return validConnString;
        }

        public Dictionary<string, string> GetConnectionDictionary(string connectionString)
        {
            var result = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return result;
            }

            connectionString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ForEach(c =>
            {
                if (string.IsNullOrWhiteSpace(c)) return;
                var index = c.IndexOf('=');
                var key = c.Substring(0, index).Trim();
                var value = c.Substring(index + 1).Trim();
                result.Add(key, value);
            });

            return result;
        }

        public string GetConnectionString(Dictionary<string, string> dic)
        {
            var list = new List<string>();
            dic.ForEach(c =>
            {
                list.Add($"{c.Key}={c.Value}");
            });
            return string.Join(";", list);
        }

        private void AddConnStringFromConfiguration(string connStringKeyPrefix, bool master)
        {
            var maxCount = 10;
#if NETSTANDARD
            var originalConnString = _configuration.GetConnectionString(connStringKeyPrefix);
            if (!string.IsNullOrWhiteSpace(originalConnString))
            {
                AddConnectionString(new ConnectionStringOptions(originalConnString, null, master));
            }
            else if (_connSections != null && _connSections.Any())
            {
                for (var i = 0; i < maxCount; i++)
                {
                    var section = _connSections.Find(c => c.Key == $"{connStringKeyPrefix}{i + 1}");
                    if (string.IsNullOrWhiteSpace(section?.Value)) break;
                    AddConnectionString(new ConnectionStringOptions(section.Value, null, master));
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
