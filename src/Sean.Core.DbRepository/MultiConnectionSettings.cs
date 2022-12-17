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
    /// Single or master/slave database settings.
    /// </summary>
    public class MultiConnectionSettings
    {
        /// <summary>
        /// Database connection strings. The clustered database configuration name suffix is a number starting with 1, example: xxx.master (primary database), xxx.secondary1 (secondary database), xxx.secondary2 (secondary database).
        /// </summary>
        public List<ConnectionStringOptions> ConnectionStrings => _connectionStrings;

        public bool IsEmpty => _connectionStrings == null || !_connectionStrings.Any();

#if NETSTANDARD
        private readonly IConfiguration _configuration;
#endif

        private readonly List<ConnectionStringOptions> _connectionStrings;
        private int _times;

        #region Constructors
#if NETSTANDARD
        /// <summary>
        /// Single or clustered database.
        /// </summary>
        /// <param name="configName">Configuration ConnectionStrings name</param>
        /// <param name="configuration"></param>
        public MultiConnectionSettings(IConfiguration configuration, string configName = Constants.Master)
        {
            if (string.IsNullOrWhiteSpace(configName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(configName));

            _connectionStrings = new List<ConnectionStringOptions>();

            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            //_configuration = configuration ?? new ConfigurationBuilder()
            //    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            //    .AddJsonFile("appsettings.json", true, true)
            //    .AddEnvironmentVariables()
            //    .Build();

            ConnectionStringOptions.CreateMultiFromConnectionName(_configuration, configName)?.ForEach(Add);
        }
#else
        /// <summary>
        /// Single or clustered database.
        /// </summary>
        /// <param name="configName">Configuration ConnectionStrings name</param>
        public MultiConnectionSettings(string configName = Constants.Master)
        {
            if (string.IsNullOrWhiteSpace(configName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(configName));

            _connectionStrings = new List<ConnectionStringOptions>();

            ConnectionStringOptions.CreateMultiFromConnectionName(configName)?.ForEach(Add);
        }
#endif
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
                    Add(options);
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

            Add(options);
        }
        #endregion

        public void Add(ConnectionStringOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (!options.IsValid)
            {
                if (!string.IsNullOrWhiteSpace(options.ConnectionName))
                {
#if NETSTANDARD
                    options.ReloadFromConnectionName(_configuration);
#else
                    options.ReloadFromConnectionName();
#endif
                }
                else
                {
                    options.ReloadFromConnectionString();
                }

                if (!options.IsValid)
                {
                    throw new Exception("Invalid database connection string option.");
                }
            }

            _connectionStrings.Add(options);
        }

        public ConnectionStringOptions Get(bool master = true)
        {
            #region Single database connection configuration.
            if (_connectionStrings.Count <= 1)
            {
                return _connectionStrings.FirstOrDefault();
            }
            #endregion

            #region Multiple database connection configuration.
            if (!master && _connectionStrings.Count(c => c.Master == master) < 1)
            {
                // If there is no slave database configuration, the master database configuration is used by default.
                master = true;
            }

            if (_connectionStrings.Count(c => c.Master == master) < 2)
            {
                return _connectionStrings.FirstOrDefault(c => c.Master == master);
            }

            var list = _connectionStrings.Where(c => c.Master == master).ToList();
            return list[Interlocked.Increment(ref _times) % list.Count];
            #endregion
        }

        public string GetConnectionString(bool master = true)
        {
            return Get(master)?.ConnectionString;
        }
    }
}
