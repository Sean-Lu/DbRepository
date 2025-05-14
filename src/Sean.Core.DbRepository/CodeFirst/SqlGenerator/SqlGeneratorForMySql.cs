using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository.CodeFirst;

public class SqlGeneratorForMySql : BaseSqlGenerator
{
    public SqlGeneratorForMySql() : base(DatabaseType.MySql)
    {
    }
    public SqlGeneratorForMySql(DatabaseType compatibleDbType) : base(compatibleDbType)
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
                result = "bit(1)";
                break;
            case not null when underlyingType == typeof(string):
                {
                    result = fieldInfo.MaxLength.HasValue ? $"varchar({fieldInfo.MaxLength.Value})" : "varchar";
                    break;
                }
            case not null when underlyingType == typeof(DateTime):
                result = "datetime";
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

    public override List<string> GetCreateTableSql(Type entityType, bool ignoreIfExists = false, Func<string, string> tableNameFunc = null)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var entityInfo = entityType.GetEntityInfo();
        var tableName = tableNameFunc != null ? tableNameFunc(entityInfo.TableName) : entityInfo.TableName;
        sb.AppendLine($"CREATE TABLE{(ignoreIfExists ? " IF NOT EXISTS" : string.Empty)} {_dbType.MarkAsIdentifier(tableName)} (");
        var fieldInfoList = new List<string>();
        var sbFieldInfo = new StringBuilder();
        foreach (var fieldInfo in entityInfo.FieldInfos)
        {
            sbFieldInfo.Clear();
            sbFieldInfo.Append($"  {_dbType.MarkAsIdentifier(fieldInfo.FieldName)} {ConvertFieldType(fieldInfo)}");
            if (fieldInfo.IsNotAllowNull)
            {
                sbFieldInfo.Append(" NOT NULL");
            }
            if (fieldInfo.FieldDefaultValue != null)
            {
                sbFieldInfo.Append($" DEFAULT {ConvertFieldDefaultValue(fieldInfo.FieldDefaultValue)}");
            }
            var autoUpdateFieldAttribute = fieldInfo.Property.GetCustomAttribute<AutoUpdateCurrentTimestampAttribute>(true);
            if (autoUpdateFieldAttribute != null)
            {
                if (!autoUpdateFieldAttribute.SetDefault)
                {
                    sbFieldInfo.Append(" ON UPDATE CURRENT_TIMESTAMP");
                }
                else if (fieldInfo.FieldDefaultValue == null)
                {
                    sbFieldInfo.Append(" DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");
                }
            }
            if (fieldInfo.IsIdentityField)
            {
                sbFieldInfo.Append(" AUTO_INCREMENT");
            }
            if (!string.IsNullOrWhiteSpace(fieldInfo.FieldDescription))
            {
                sbFieldInfo.Append($" COMMENT '{fieldInfo.FieldDescription}'");
            }
            fieldInfoList.Add(sbFieldInfo.ToString());
        }
        if (entityInfo.FieldInfos.Any(c => c.IsPrimaryKey))
        {
            fieldInfoList.Add($"  PRIMARY KEY ({string.Join(",", entityInfo.FieldInfos.Where(c => c.IsPrimaryKey).Select(c => _dbType.MarkAsIdentifier(c.FieldName)).ToList())})");
        }
        sb.AppendLine(string.Join($",{Environment.NewLine}", fieldInfoList));
        sb.Append(")");
        if (!string.IsNullOrWhiteSpace(entityInfo.TableDescription))
        {
            sb.Append($" COMMENT='{entityInfo.TableDescription}'");
        }
        sb.AppendLine(";");
        var createIndexSql = GetCreateIndexSql(entityType, ignoreIfExists, tableName);
        createIndexSql?.ForEach(sql =>
        {
            sb.AppendLine($"{sql};");
        });
        result.Add(sb.ToString());
        return result;
    }

    public override List<string> GetUpgradeSql(Type entityType, Func<string, string> tableNameFunc = null)
    {
        var entityInfo = entityType.GetEntityInfo();
        var tableName = tableNameFunc != null ? tableNameFunc(entityInfo.TableName) : entityInfo.TableName;
        if (!IsTableExists(tableName))
        {
            return GetCreateTableSql(entityType, false, _ => tableName);
        }
        var missingTableFieldInfo = GetDbMissingTableFields(entityType, tableName);
        var result = new List<string>();
        var sb = new StringBuilder();
        missingTableFieldInfo?.ForEach(fieldInfo =>
        {
            sb.Clear();
            sb.Append($"ALTER TABLE {_dbType.MarkAsIdentifier(tableName)} ADD {_dbType.MarkAsIdentifier(fieldInfo.FieldName)} {ConvertFieldType(fieldInfo)}");
            if (fieldInfo.IsNotAllowNull)
            {
                sb.Append(" NOT NULL");
            }
            if (fieldInfo.FieldDefaultValue != null)
            {
                sb.Append($" DEFAULT {ConvertFieldDefaultValue(fieldInfo.FieldDefaultValue)}");
            }
            var autoUpdateFieldAttribute = fieldInfo.Property.GetCustomAttribute<AutoUpdateCurrentTimestampAttribute>(true);
            if (autoUpdateFieldAttribute != null)
            {
                if (!autoUpdateFieldAttribute.SetDefault)
                {
                    sb.Append(" ON UPDATE CURRENT_TIMESTAMP");
                }
                else if (fieldInfo.FieldDefaultValue == null)
                {
                    sb.Append(" DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");
                }
            }
            if (!string.IsNullOrWhiteSpace(fieldInfo.FieldDescription))
            {
                sb.Append($" COMMENT '{fieldInfo.FieldDescription}'");
            }
            sb.Append(";");
            result.Add(sb.ToString());
        });
        return result;
    }
}