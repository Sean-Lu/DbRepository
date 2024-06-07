using Sean.Utility.Contracts;
using Sean.Utility.Format;
using System.Data.Common;
using System.IO;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Data;
using Sean.Core.DbRepository.Extensions;
#if NETSTANDARD || NET5_0_OR_GREATER
using Microsoft.Extensions.Configuration;
#endif

namespace Sean.Core.DbRepository;

public class DbOptions
{
    public NamingConvention DefaultNamingConvention { get; set; }

    /// <summary>
    /// The limit on the number of entities when executing database bulk operations. The default value is 1000.
    /// </summary>
    public int? BulkEntityCount { get; set; } = 1000;

    /// <summary>
    /// The time (in seconds) to wait for the command to execute.
    /// <para>等待命令执行所需的时间（以秒为单位）。</para>
    /// </summary>
    public int? DefaultCommandTimeout { get; set; }

    /// <summary>
    /// Whether the entity class property names are case-sensitive when mapping database table fields. The default value is false.
    /// <para>映射数据库表字段时，实体类属性名是否区分大小写。默认值：false。</para>
    /// </summary>
    public bool PropertyNameCaseSensitive { get; set; } = false;

    /// <summary>
    /// <see cref="DbProviderFactory"/> configuration file path.
    /// <para>数据库提供者工厂的配置文件路径</para>
    /// </summary>
    public string DbProviderFactoryConfigurationPath { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $@"dllconfigs\{Assembly.GetExecutingAssembly().GetName().Name}.dll.config");

    public IJsonSerializer JsonSerializer { get; set; } = JsonHelper.Serializer;

    public Action<IDbCommand> SetDbCommand { get; set; }

    public Func<DbProviderFactory, DatabaseType> MapToDatabaseType { get; set; }
    public Func<DatabaseType, DbProviderFactory> MapToDbProviderFactory { get; set; }

#if NETSTANDARD || NET5_0_OR_GREATER
    /// <summary>
    /// Custom get database connection configuration.
    /// </summary>
    public Func<IConfiguration, string, List<ConnectionStringOptions>> GetConnectionStringOptions { get; set; }
#endif

    /// <summary>
    /// <see cref="DatabaseTypeExtensions.GetSqlForTableExists"/>
    /// </summary>
    public Func<DatabaseType, DbConnection, string, string> GetSqlForTableExists { get; set; }
    /// <summary>
    /// <see cref="DatabaseTypeExtensions.GetSqlForTableFieldExists"/>
    /// </summary>
    public Func<DatabaseType, DbConnection, string, string, string> GetSqlForTableFieldExists { get; set; }

    public Func<DatabaseType, DbConnection, string, bool?> IsTableExists { get; set; }
    public Func<DatabaseType, DbConnection, string, string, bool?> IsTableFieldExists { get; set; }

    /// <summary>
    /// Whether the SQL is indent. The default value is false.
    /// </summary>
    public bool SqlIndented { get; set; } = false;
    /// <summary>
    /// Whether the SQL is parameterized. The default value is true.
    /// </summary>
    public bool SqlParameterized { get; set; } = true;

    public event Action<SqlExecutingContext> SqlExecuting;
    public event Action<SqlExecutedContext> SqlExecuted;

    private static readonly Dictionary<Type, ITypeHandler> _typeHandlers = new();

    internal void TriggerSqlExecuting(SqlExecutingContext context)
    {
        SqlExecuting?.Invoke(context);
    }
    internal void TriggerSqlExecuted(SqlExecutedContext context)
    {
        SqlExecuted?.Invoke(context);
    }

    public void AddTypeHandler(Type type, ITypeHandler typeHandler)
    {
        if (!ContainsTypeHandler(type))
        {
            _typeHandlers.Add(type, typeHandler);
        }
        else
        {
            if (_typeHandlers.TryGetValue(type, out var handler) && typeHandler == handler)
            {
                return;
            }

            RemoveTypeHandler(type);
            _typeHandlers.Add(type, typeHandler);
        }
    }
    public bool RemoveTypeHandler(Type type)
    {
        return _typeHandlers.Remove(type);
    }
    public bool ContainsTypeHandler(Type type)
    {
        return _typeHandlers.ContainsKey(type);
    }
    public ITypeHandler GetTypeHandler(Type type)
    {
        _typeHandlers.TryGetValue(type, out var handler);
        return handler;
    }
}