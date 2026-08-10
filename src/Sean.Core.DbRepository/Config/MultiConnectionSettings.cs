using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
#if NETSTANDARD || NET5_0_OR_GREATER
using Microsoft.Extensions.Configuration;
#endif

namespace Sean.Core.DbRepository;

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

#if NETSTANDARD || NET5_0_OR_GREATER
    private readonly IConfiguration _configuration;
#endif

    private readonly List<ConnectionStringOptions> _connectionStrings;
    private int _times;

    #region Constructors
#if NETSTANDARD || NET5_0_OR_GREATER
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

        if (DbContextConfiguration.Options.GetConnectionStringOptions != null)
        {
            _configuration = configuration;
            DbContextConfiguration.Options.GetConnectionStringOptions(configuration, configName)?.ForEach(Add);
        }
        else
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            //_configuration = configuration ?? new ConfigurationBuilder()
            //    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            //    .AddJsonFile("appsettings.json", true, true)
            //    .AddEnvironmentVariables()
            //    .Build();

            ConnectionStringOptions.CreateMultiFromConnectionName(_configuration, configName)?.ForEach(Add);
        }
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

    public void Clear()
    {
        _connectionStrings.Clear();
    }

    public void Add(ConnectionStringOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));

        if (!options.IsValid)
        {
            if (!string.IsNullOrWhiteSpace(options.ConnectionName))
            {
#if NETSTANDARD || NET5_0_OR_GREATER
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
            return _connectionStrings.Count == 0 ? null : _connectionStrings[0];
        }
        #endregion

        #region Multiple database connection configuration.
        var firstMatch = FindConnections(master, out var matchCount);
        if (!master && matchCount == 0)
        {
            // 如果没有从库配置，则默认回退到主库配置。
            master = true;
            firstMatch = FindConnections(master, out matchCount);
        }

        if (matchCount < 2)
        {
            return firstMatch;
        }

        var selectedIndex = (int)((uint)Interlocked.Increment(ref _times) % (uint)matchCount);
        foreach (var options in _connectionStrings)
        {
            if (options.Master == master && selectedIndex-- == 0)
            {
                return options;
            }
        }

        // ConnectionStrings 是公开的可变列表；如果它在计数和选择之间发生变化，
        // 则返回第一次匹配项作为安全兜底。
        return firstMatch;
        #endregion
    }

    private ConnectionStringOptions FindConnections(bool master, out int count)
    {
        ConnectionStringOptions firstMatch = null;
        count = 0;
        foreach (var options in _connectionStrings)
        {
            if (options.Master != master)
            {
                continue;
            }

            firstMatch ??= options;
            count++;
        }
        return firstMatch;
    }

    public string GetConnectionString(bool master = true)
    {
        return Get(master)?.ConnectionString;
    }
}
