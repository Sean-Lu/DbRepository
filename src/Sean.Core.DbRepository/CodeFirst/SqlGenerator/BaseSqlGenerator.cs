using System;
using System.Collections.Generic;
using System.Linq;
using Sean.Core.DbRepository.Extensions;
using Sean.Core.DbRepository.DbFirst;
#if NETFRAMEWORK
using System.Data.Odbc;
using System.Data.OleDb;
#endif

namespace Sean.Core.DbRepository.CodeFirst;

public abstract class BaseSqlGenerator : ISqlGenerator
{
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
        var codeGenerator = CodeGeneratorFactory.GetCodeGenerator(_dbType);
        codeGenerator.Initialize(_db);
        var tableFieldInfos = codeGenerator.GetTableFieldInfo(tableName);
        return entityType.GetEntityInfo().FieldInfos.Where(entityTableFieldInfo => !tableFieldInfos.Exists(c => c.FieldName == entityTableFieldInfo.FieldName)).ToList();
    }

    protected virtual List<TableFieldModel> GetEntityMissingTableFields(Type entityType, string tableName)
    {
        var codeGenerator = CodeGeneratorFactory.GetCodeGenerator(_dbType);
        codeGenerator.Initialize(_db);
        var tableFieldInfos = codeGenerator.GetTableFieldInfo(tableName);
        var entityTableFieldInfos = entityType.GetEntityInfo().FieldInfos;
        return tableFieldInfos.Where(c => !entityTableFieldInfos.Exists(entityTableFieldInfo => entityTableFieldInfo.FieldName == c.FieldName)).ToList();
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

        return defaultValue switch
        {
            bool boolValue => _dbType switch
            {
                DatabaseType.QuestDB => boolValue ? "TRUE" : "FALSE",
                DatabaseType.Informix => boolValue ? "'t'" : "'f'",
                DatabaseType.Xugu => boolValue ? "true" : "false",
                _ => boolValue ? "1" : "0"
            },
            char charValue => $"'{charValue}'",
            string stringValue => $"'{stringValue}'",
            Enum enumValue => Convert.ToInt32(enumValue).ToString(),
            _ => defaultValue.ToString()
        };
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