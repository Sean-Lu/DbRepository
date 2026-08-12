using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Sean.Core.DbRepository.Extensions;
using Sean.Core.DbRepository.DbFirst;
using Sean.Core.DbRepository.Util;
#if NETFRAMEWORK
using System.Data.Odbc;
using System.Data.OleDb;
#endif

namespace Sean.Core.DbRepository.CodeFirst;

public abstract class BaseSqlGenerator : ISqlGenerator
{
    /// <summary>
    /// 支持使用 CREATE INDEX IF NOT EXISTS 语法创建索引的数据库
    /// </summary>
    private static readonly List<DatabaseType> CreateIndexSupportIfNotExistsDatabaseTypes = new()
    {
        //DatabaseType.MySql,// 不支持
        DatabaseType.TiDB,
        //DatabaseType.SqlServer,// 不支持
        DatabaseType.SQLite,// SQLite 3.3.0+
        DatabaseType.DuckDB,
        //DatabaseType.MsAccess,// 不支持
        DatabaseType.PostgreSql,// PostgreSQL 9.5+
        //DatabaseType.OpenGauss,// 不支持
        DatabaseType.HighgoDB,
        DatabaseType.IvorySQL,
        DatabaseType.Dameng,
        DatabaseType.KingbaseES
    };

    /// <summary>
    /// 这些数据库的索引语法和能力无法由通用 CREATE INDEX 安全表达。
    /// </summary>
    private static readonly HashSet<DatabaseType> CreateIndexUnsupportedDatabaseTypes = new()
    {
        DatabaseType.ClickHouse,
        DatabaseType.QuestDB
    };

    protected DbFactory _db;
    protected readonly DatabaseType _dbType;

    protected BaseSqlGenerator(DatabaseType dbType)
    {
        _dbType = dbType;
    }

    public virtual void Initialize(string connectionString)
    {
        _db = new DbFactory(new MultiConnectionSettings(ConnectionStringOptions.Create(connectionString, _dbType)));
    }
    public virtual void Initialize(DbFactory dbFactory)
    {
        _db = dbFactory;
    }

    protected virtual List<EntityFieldInfo> GetDbMissingTableFields(Type entityType, string tableName)
    {
        return CodeGeneratorFactory.UseCodeGenerator(_dbType, codeGenerator =>
        {
            if (codeGenerator == null)
            {
                throw new NotSupportedException($"数据库类型 [{_dbType}] 暂不支持读取表结构，无法执行 CodeFirst 升级。");
            }
            codeGenerator.Initialize(_db);
            var tableFieldInfos = codeGenerator.GetTableFieldInfo(tableName);
            return entityType.GetEntityInfo().FieldInfos
                .Where(entityTableFieldInfo => !tableFieldInfos.Exists(c => IsSameFieldName(c.FieldName,
                    entityTableFieldInfo.FieldName)))
                .ToList();
        });
    }

    protected virtual List<TableFieldModel> GetEntityMissingTableFields(Type entityType, string tableName)
    {
        return CodeGeneratorFactory.UseCodeGenerator(_dbType, codeGenerator =>
        {
            if (codeGenerator == null)
            {
                throw new NotSupportedException($"数据库类型 [{_dbType}] 暂不支持读取表结构，无法执行 CodeFirst 升级。");
            }
            codeGenerator.Initialize(_db);
            var tableFieldInfos = codeGenerator.GetTableFieldInfo(tableName);
            var entityTableFieldInfos = entityType.GetEntityInfo().FieldInfos;
            return tableFieldInfos
                .Where(c => !entityTableFieldInfos.Exists(entityTableFieldInfo => IsSameFieldName(
                    entityTableFieldInfo.FieldName, c.FieldName)))
                .ToList();
        });
    }

    /// <summary>
    /// 字段名保持精确比较，避免把区分大小写的带引号标识符误判为同一字段。
    /// </summary>
    private static bool IsSameFieldName(string left, string right)
    {
        return string.Equals(left, right, StringComparison.Ordinal);
    }

    protected virtual bool IsTableExists(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(tableName));

        var master = true;
        string connectionString = _db.ConnectionSettings.GetConnectionString(master);
        if (TableInfoCache.IsTableExists(connectionString, master, tableName))
        {
            return true;
        }

        bool? exists = null;
        using (var connection = _db.OpenNewConnection(connectionString))
        {
            exists = DbContextConfiguration.Options.IsTableExists?.Invoke(_dbType, connection, tableName) ??
                     connection switch
                     {
#if NETFRAMEWORK
                         OleDbConnection oleDbConnection => oleDbConnection.IsTableExists(tableName),
                         OdbcConnection odbcConnection => odbcConnection.IsTableExists(tableName),
#endif
                         _ => null
                     };

            if (!exists.HasValue)
            {
                var sql = _dbType.GetSqlForTableExists(connection, tableName);
                exists = _db.ExecuteScalar<int>(connection, sql) > 0;
            }
        }

        if (exists.GetValueOrDefault())
        {
            TableInfoCache.AddTable(connectionString, master, tableName);
        }
        return exists.GetValueOrDefault();
    }

    protected virtual string ConvertFieldDefaultValue(object defaultValue)
    {
        if (defaultValue == null)
        {
            return null;
        }

        // 仅对原生布尔类型使用方言支持的布尔字面量，其余数据库保持原有数值表示。
        return defaultValue switch
        {
            bool boolValue => _dbType switch
            {
                DatabaseType.PostgreSql => boolValue ? "TRUE" : "FALSE",
                DatabaseType.OpenGauss => boolValue ? "TRUE" : "FALSE",
                DatabaseType.HighgoDB => boolValue ? "TRUE" : "FALSE",
                DatabaseType.IvorySQL => boolValue ? "TRUE" : "FALSE",
                DatabaseType.KingbaseES => boolValue ? "TRUE" : "FALSE",
                DatabaseType.Firebird => boolValue ? "TRUE" : "FALSE",
                DatabaseType.DuckDB => boolValue ? "TRUE" : "FALSE",
                DatabaseType.QuestDB => boolValue ? "TRUE" : "FALSE",
                DatabaseType.Informix => boolValue ? "'t'" : "'f'",
                DatabaseType.Xugu => boolValue ? "true" : "false",
                _ => boolValue ? "1" : "0"
            },
            char charValue => ConvertDdlTextLiteral(charValue.ToString()),
            string stringValue => ConvertDdlTextLiteral(stringValue),
            // 现有字段类型生成器统一以整数类型表示枚举，保持原有 Int32 转换规则，避免默认值与字段类型宽度不一致。
            Enum enumValue => Convert.ToInt32(enumValue, CultureInfo.InvariantCulture)
                .ToString(CultureInfo.InvariantCulture),
            byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal =>
                Convert.ToString(defaultValue, CultureInfo.InvariantCulture),
            // 未明确修复的类型保持原有转换行为，避免本批次扩大兼容性影响。
            _ => defaultValue.ToString()
        };
    }

    /// <summary>
    /// 转义 DDL 中仅允许字面量的文本，避免为旧版本数据库生成 CONCAT 等表达式。
    /// </summary>
    protected virtual string ConvertDdlTextLiteral(string value)
    {
        return SqlBuilderUtil.EscapeSqlLiteral(_dbType, value, false);
    }

    /// <summary>
    /// 判断指定索引能否使用关系型数据库的通用 CREATE INDEX 语法生成。
    /// </summary>
    protected virtual bool SupportsCreateIndex(DbIndexType indexType)
    {
        return !CreateIndexUnsupportedDatabaseTypes.Contains(_dbType);
    }

    protected virtual List<string> GetCreateIndexSql(Type entityType, bool ignoreIfExists = false, string tableName = null)
    {
        var entityInfo = entityType.GetEntityInfo();
        if (string.IsNullOrWhiteSpace(tableName))
        {
            tableName = entityInfo.TableName;
        }
        var indexInfos = entityInfo.IndexInfos;
        if (indexInfos == null || !indexInfos.Any())
        {
            return null;
        }

        var sqlList = new List<string>();
        foreach (var indexDescriptor in indexInfos)
        {
            var indexPropertyNames = indexDescriptor.IndexPropertyNames;
            var indexName = indexDescriptor.IndexName;
            var indexType = indexDescriptor.IndexType;
            if (!SupportsCreateIndex(indexType))
            {
                if (indexType == DbIndexType.Unique)
                {
                    // 静默忽略唯一索引会丢失数据约束，无法安全生成时必须明确失败。
                    throw new NotSupportedException(
                        $"数据库类型 [{_dbType}] 不支持通过通用 CodeFirst 语法创建唯一索引，请使用数据库专用方式创建。");
                }

                // 普通索引不影响数据约束，跳过无法用通用语法安全表达的数据库专用索引。
                continue;
            }
            if (indexPropertyNames == null || !indexPropertyNames.Any())
            {
                continue;
            }

            var indexFieldNames = entityInfo.FieldInfos.Where(c => indexPropertyNames.Contains(c.PropertyName)).Select(c => c.FieldName).ToArray();
            if (!indexFieldNames.Any())
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(indexName))
            {
                indexName = $"IDX_{tableName}_{string.Join("_", indexFieldNames)}";
            }

            var sql = GetCreateIndexSql(indexType, indexName, tableName, indexFieldNames, ignoreIfExists);
            if (!string.IsNullOrWhiteSpace(sql))
            {
                sqlList.Add(sql);
            }
        }

        return sqlList;
    }

    protected virtual string GetCreateIndexSql(DbIndexType indexType, string indexName, string tableName, IEnumerable<string> indexFieldNames, bool ignoreIfExists = false)
    {
        switch (indexType)
        {
            case DbIndexType.Normal:
                return $"CREATE INDEX{(ignoreIfExists && CreateIndexSupportIfNotExistsDatabaseTypes.Contains(_dbType) ? " IF NOT EXISTS" : string.Empty)} {_dbType.MarkAsIdentifier(indexName)} ON {_dbType.MarkAsIdentifier(tableName)} ({string.Join(",", indexFieldNames.Select(c => _dbType.MarkAsIdentifier(c)).ToList())})";
            case DbIndexType.Unique:
                return $"CREATE UNIQUE INDEX{(ignoreIfExists && CreateIndexSupportIfNotExistsDatabaseTypes.Contains(_dbType) ? " IF NOT EXISTS" : string.Empty)} {_dbType.MarkAsIdentifier(indexName)} ON {_dbType.MarkAsIdentifier(tableName)} ({string.Join(",", indexFieldNames.Select(c => _dbType.MarkAsIdentifier(c)).ToList())})";
            default:
                throw new NotImplementedException($"不支持的索引类型：{indexType}");
        }
    }

    public virtual List<string> GetCreateTableSql<TEntity>(bool ignoreIfExists = false, Func<string, string> tableNameFunc = null)
    {
        return GetCreateTableSql(typeof(TEntity), ignoreIfExists, tableNameFunc);
    }
    public abstract List<string> GetCreateTableSql(Type entityType, bool ignoreIfExists = false, Func<string, string> tableNameFunc = null);

    public virtual List<string> GetUpgradeSql<TEntity>(Func<string, string> tableNameFunc = null)
    {
        return GetUpgradeSql(typeof(TEntity), tableNameFunc);
    }
    public abstract List<string> GetUpgradeSql(Type entityType, Func<string, string> tableNameFunc = null);
}
