using System;
using System.Collections.Generic;
using Sean.Utility.Extensions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using Sean.Core.DbRepository.Extensions;
using System.ComponentModel;
using System.IO;
using System.Xml;
using Sean.Core.DbRepository.DbFirst;
#if NETFRAMEWORK
using System.Data.Odbc;
using System.Data.OleDb;
#endif

namespace Sean.Core.DbRepository.CodeFirst;

public abstract class BaseSqlGenerator : IBaseSqlGenerator
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

    protected virtual string GetTableName<TEntity>()
    {
        return GetTableName(typeof(TEntity));
    }
    protected virtual string GetTableName(Type entityType)
    {
        return entityType.GetMainTableName();
    }

    protected virtual string GetTableDescription<TEntity>()
    {
        return GetTableDescription(typeof(TEntity));
    }
    protected virtual string GetTableDescription(Type entityType)
    {
        var tableDescription = entityType.GetCustomAttribute<DescriptionAttribute>()?.Description;
        if (string.IsNullOrWhiteSpace(tableDescription))
        {
            string filePath = Path.ChangeExtension(entityType.Assembly.Location, "xml");
            if (File.Exists(filePath))
            {
                XmlDocument xmlDoc = new XmlDocument();
                //xmlDoc.PreserveWhitespace = true;
                xmlDoc.Load(filePath);
                tableDescription = xmlDoc.SelectSingleNode($"//member[@name='T:{entityType.FullName}']")?.InnerText.Trim('\r', '\n', ' ');
            }
        }
        return tableDescription;
    }

    protected virtual string GetFieldName(PropertyInfo fieldPropertyInfo)
    {
        return fieldPropertyInfo.GetFieldName();
    }

    protected virtual string GetFieldDescription(PropertyInfo fieldPropertyInfo)
    {
        var fieldDescription = fieldPropertyInfo.GetCustomAttribute<DescriptionAttribute>()?.Description;
        if (string.IsNullOrWhiteSpace(fieldDescription))
        {
            string filePath = Path.ChangeExtension(fieldPropertyInfo.DeclaringType.Assembly.Location, "xml");
            if (File.Exists(filePath))
            {
                XmlDocument xmlDoc = new XmlDocument();
                //xmlDoc.PreserveWhitespace = true;
                xmlDoc.Load(filePath);
                fieldDescription = xmlDoc.SelectSingleNode($"//member[@name='P:{fieldPropertyInfo.DeclaringType.FullName}.{fieldPropertyInfo.Name}']")?.InnerText?.Trim('\r', '\n', ' ');
            }
        }
        return fieldDescription;
    }

    protected virtual object GetFieldDefaultValue(PropertyInfo fieldPropertyInfo)
    {
        return fieldPropertyInfo.GetCustomAttribute<DefaultValueAttribute>()?.Value;
    }

    protected virtual List<TableFieldInfo> GetDbMissingTableFields<TEntity>(string tableName)
    {
        return GetDbMissingTableFields(typeof(TEntity), tableName);
    }
    protected virtual List<TableFieldInfo> GetDbMissingTableFields(Type entityType, string tableName)
    {
        var codeGenerator = CodeGeneratorFactory.GetCodeGenerator(_dbType);
        codeGenerator.Initialize(_db);
        var tableFieldInfos = codeGenerator.GetTableFieldInfo(tableName);
        return entityType.GetEntityInfo().FieldInfos.Where(entityTableFieldInfo => !tableFieldInfos.Exists(c => c.FieldName == entityTableFieldInfo.FieldName)).ToList();
    }

    protected virtual List<TableFieldModel> GetEntityMissingTableFields<TEntity>(string tableName)
    {
        return GetEntityMissingTableFields(typeof(TEntity), tableName);
    }
    protected virtual List<TableFieldModel> GetEntityMissingTableFields(Type entityType, string tableName)
    {
        var codeGenerator = CodeGeneratorFactory.GetCodeGenerator(_dbType);
        codeGenerator.Initialize(_db);
        var tableFieldInfos = codeGenerator.GetTableFieldInfo(tableName);
        var entityTableFieldInfos = entityType.GetEntityInfo().FieldInfos;
        return tableFieldInfos.Where(c => !entityTableFieldInfos.Exists(entityTableFieldInfo => entityTableFieldInfo.FieldName == c.FieldName)).ToList();
    }

    protected virtual bool IsNotAllowNull(PropertyInfo fieldPropertyInfo)
    {
        return fieldPropertyInfo.PropertyType.IsValueType && !fieldPropertyInfo.PropertyType.IsNullableType()
               || IsPrimaryKey(fieldPropertyInfo)
               || fieldPropertyInfo.GetCustomAttributes<RequiredAttribute>(false).Any();
    }

    protected virtual bool IsPrimaryKey(PropertyInfo fieldPropertyInfo)
    {
        return fieldPropertyInfo.GetCustomAttributes<KeyAttribute>(false).Any();
    }

    protected virtual bool IsIdentity(PropertyInfo fieldPropertyInfo)
    {
        return fieldPropertyInfo.GetCustomAttributes<DatabaseGeneratedAttribute>(false).Any(c => c.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity);
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
}