using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository.CodeFirst;

public class SqlGeneratorForClickHouse : BaseSqlGenerator
{
    public SqlGeneratorForClickHouse() : base(DatabaseType.ClickHouse)
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
                result = "Int64";
                break;
            case not null when underlyingType == typeof(int) || underlyingType.IsEnum:
                result = "Int32";
                break;
            case not null when underlyingType == typeof(bool):
                result = "Bool";
                break;
            case not null when underlyingType == typeof(string):
                {
                    result = "String";
                    break;
                }
            case not null when underlyingType == typeof(DateTime):
                result = "DateTime";
                break;
            case not null when underlyingType == typeof(decimal):
                {
                    result = fieldInfo.NumericPrecision.HasValue ? $"Decimal({fieldInfo.NumericPrecision.GetValueOrDefault()},{fieldInfo.NumericScale.GetValueOrDefault()})" : "Decimal";
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
            sbFieldInfo.Append($"  {_dbType.MarkAsIdentifier(fieldInfo.FieldName)}");
            sbFieldInfo.Append(fieldInfo.IsNotAllowNull
                ? $" {ConvertFieldType(fieldInfo)} NOT NULL"
                : $" Nullable({ConvertFieldType(fieldInfo)})");
            if (fieldInfo.FieldDefaultValue != null)
            {
                sbFieldInfo.Append($" DEFAULT {ConvertFieldDefaultValue(fieldInfo.FieldDefaultValue)}");
            }
            //if (fieldInfo.Identity)
            //{
            //    sbFieldInfo.Append(" AUTO_INCREMENT");
            //}
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
        sb.Append(") ENGINE = MergeTree()");
        if (!string.IsNullOrWhiteSpace(entityInfo.TableDescription))
        {
            sb.Append($" COMMENT '{entityInfo.TableDescription}'");
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
            sb.Append($"ALTER TABLE {_dbType.MarkAsIdentifier(tableName)} ADD {_dbType.MarkAsIdentifier(fieldInfo.FieldName)}");
            sb.Append(fieldInfo.IsNotAllowNull
                ? $" {ConvertFieldType(fieldInfo)} NOT NULL"
                : $" Nullable({ConvertFieldType(fieldInfo)})");
            if (fieldInfo.FieldDefaultValue != null)
            {
                sb.Append($" DEFAULT {ConvertFieldDefaultValue(fieldInfo.FieldDefaultValue)}");
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