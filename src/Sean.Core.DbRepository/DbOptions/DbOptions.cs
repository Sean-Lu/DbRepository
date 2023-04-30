using Sean.Utility.Contracts;
using Sean.Utility.Format;
using System.Data.Common;
using System.IO;
using System.Reflection;
using System;
using System.Data;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository;

public class DbOptions
{
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

    public event Action<SqlExecutingContext> SqlExecuting;
    public event Action<SqlExecutedContext> SqlExecuted;

    internal void TriggerSqlExecuting(SqlExecutingContext context)
    {
        SqlExecuting?.Invoke(context);
    }
    internal void TriggerSqlExecuted(SqlExecutedContext context)
    {
        SqlExecuted?.Invoke(context);
    }
}