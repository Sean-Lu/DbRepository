using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository.CodeFirst;

public class SqlGeneratorForSqlServer : BaseSqlGenerator, ISqlGenerator
{
    public SqlGeneratorForSqlServer() : base(DatabaseType.SqlServer)
    {
    }

    protected virtual string ConvertFieldType(EntityFieldInfo fieldInfo)
    {
        if (!string.IsNullOrWhiteSpace(fieldInfo.FieldTypeName))
        {
            return fieldInfo.FieldTypeName;
        }

        var underlyingType = Nullable.GetUnderlyingType(fieldInfo.Property.PropertyType) ?? fieldInfo.Property.PropertyType;
        string result;
        switch (underlyingType)
        {
            case not null when underlyingType == typeof(long):
                result = "bigint";
                break;
            case not null when underlyingType == typeof(int) || underlyingType.IsEnum:
                result = "int";
                break;
            case not null when underlyingType == typeof(bool):
                result = "bit";
                break;
            case not null when underlyingType == typeof(string):
                {
                    result = fieldInfo.MaxLength.HasValue ? $"nvarchar({fieldInfo.MaxLength.Value})" : "nvarchar";
                    break;
                }
            case not null when underlyingType == typeof(DateTime):
                result = "datetime2";
                break;
            case not null when underlyingType == typeof(decimal):
                {
                    result = fieldInfo.NumericPrecision.HasValue ? $"decimal({fieldInfo.NumericPrecision.GetValueOrDefault()},{fieldInfo.NumericScale.GetValueOrDefault()})" : "decimal";
                    break;
                }
            default:
                result = $"##{underlyingType.Name}##";
                break;
        }
        return result;
    }

    public virtual List<string> GetCreateTableSql<TEntity>(Func<string, string> tableNameFunc = null)
    {
        return GetCreateTableSql(typeof(TEntity), tableNameFunc);
    }
    public List<string> GetCreateTableSql(Type entityType, Func<string, string> tableNameFunc = null)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var entityInfo = entityType.GetEntityInfo();
        var tableName = entityInfo.TableName;
        if (tableNameFunc != null)
        {
            tableName = tableNameFunc(tableName);
        }
        sb.AppendLine($"CREATE TABLE {_dbType.MarkAsTableOrFieldName(tableName)} (");
        var fieldInfoList = new List<string>();
        var sbFieldInfo = new StringBuilder();
        var fieldDescriptionDic = new Dictionary<string, string>();
        foreach (var fieldInfo in entityInfo.FieldInfos)
        {
            sbFieldInfo.Clear();
            if (!string.IsNullOrWhiteSpace(fieldInfo.FieldDescription))
            {
                fieldDescriptionDic.Add(fieldInfo.FieldName, fieldInfo.FieldDescription);
            }
            sbFieldInfo.Append($"  {_dbType.MarkAsTableOrFieldName(fieldInfo.FieldName)} {ConvertFieldType(fieldInfo)}");
            if (fieldInfo.IsNotAllowNull)
            {
                sbFieldInfo.Append(" NOT NULL");
            }
            if (fieldInfo.FieldDefaultValue != null)
            {
                sbFieldInfo.Append($" DEFAULT {ConvertFieldDefaultValue(fieldInfo.FieldDefaultValue)}");
            }
            if (fieldInfo.IsIdentityField)
            {
                sbFieldInfo.Append(" IDENTITY");
            }
            fieldInfoList.Add(sbFieldInfo.ToString());
        }
        if (entityInfo.FieldInfos.Any(c => c.IsPrimaryKey))
        {
            fieldInfoList.Add($"  PRIMARY KEY ({string.Join(",", entityInfo.FieldInfos.Where(c => c.IsPrimaryKey).Select(c => _dbType.MarkAsTableOrFieldName(c.FieldName)).ToList())})");
        }
        sb.AppendLine(string.Join($",{Environment.NewLine}", fieldInfoList));
        sb.AppendLine(");");
        if (!string.IsNullOrWhiteSpace(entityInfo.TableDescription))
        {
            sb.AppendLine($"EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'{entityInfo.TableDescription}', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'{tableName}';");
        }
        foreach (var kv in fieldDescriptionDic)
        {
            sb.AppendLine($"EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'{kv.Value}', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'{tableName}', @level2type=N'COLUMN',@level2name=N'{kv.Key}';");
        }
        result.Add(sb.ToString());
        return result;
    }

    public virtual List<string> GetUpgradeSql<TEntity>(Func<string, string> tableNameFunc = null)
    {
        return GetUpgradeSql(typeof(TEntity), tableNameFunc);
    }
    public List<string> GetUpgradeSql(Type entityType, Func<string, string> tableNameFunc = null)
    {
        var entityInfo = entityType.GetEntityInfo();
        var tableName = entityInfo.TableName;
        if (tableNameFunc != null)
        {
            tableName = tableNameFunc(tableName);
        }
        if (!IsTableExists(tableName))
        {
            return GetCreateTableSql(entityType, _ => tableName);
        }
        var missingTableFieldInfo = GetDbMissingTableFields(entityType, tableName);
        var result = new List<string>();
        var sb = new StringBuilder();
        var fieldDescriptionDic = new Dictionary<string, string>();
        missingTableFieldInfo?.ForEach(fieldInfo =>
        {
            sb.Clear();
            if (!string.IsNullOrWhiteSpace(fieldInfo.FieldDescription))
            {
                fieldDescriptionDic.Add(fieldInfo.FieldName, fieldInfo.FieldDescription);
            }
            sb.Append($"ALTER TABLE {_dbType.MarkAsTableOrFieldName(tableName)} ADD {_dbType.MarkAsTableOrFieldName(fieldInfo.FieldName)} {ConvertFieldType(fieldInfo)}");
            if (fieldInfo.IsNotAllowNull)
            {
                sb.Append(" NOT NULL");
            }
            if (fieldInfo.FieldDefaultValue != null)
            {
                sb.Append($" DEFAULT {ConvertFieldDefaultValue(fieldInfo.FieldDefaultValue)}");
            }
            sb.Append(";");
            result.Add(sb.ToString());
        });
        foreach (var kv in fieldDescriptionDic)
        {
            result.Add($"EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'{kv.Value}', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'{tableName}', @level2type=N'COLUMN',@level2name=N'{kv.Key}';");
        }
        return result;
    }
}