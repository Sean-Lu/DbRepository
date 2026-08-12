using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository.CodeFirst;

public class SqlGeneratorForSQLite : BaseSqlGenerator
{
    public SqlGeneratorForSQLite() : base(DatabaseType.SQLite)
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
            case not null when underlyingType == typeof(long) || underlyingType == typeof(int) ||
                               underlyingType == typeof(bool) || underlyingType.IsEnum:
                result = "INTEGER";
                break;
            case not null when underlyingType == typeof(string) || underlyingType == typeof(DateTime):
                result = "TEXT";
                break;
            case not null when underlyingType == typeof(decimal):
                {
                    result = fieldInfo.NumericPrecision.HasValue ? $"DECIMAL({fieldInfo.NumericPrecision.GetValueOrDefault()},{fieldInfo.NumericScale.GetValueOrDefault()})" : "DECIMAL";
                    break;
                }
            case not null when underlyingType == typeof(byte[]):
                result = "BLOB";
                break;
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
        var hasPrimaryKeyIdentity = false;
        var primaryKeyCount = entityInfo.FieldInfos.Count(c => c.IsPrimaryKey);
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
            if (fieldInfo.IsIdentityField)
            {
                // SQLite 的 AUTOINCREMENT 只能用于唯一的 INTEGER 主键，复合主键必须保留表级约束。
                if (primaryKeyCount == 1 && fieldInfo.IsPrimaryKey && string.Equals(ConvertFieldType(fieldInfo), "INTEGER",
                        StringComparison.OrdinalIgnoreCase))
                {
                    sbFieldInfo.Append(" PRIMARY KEY AUTOINCREMENT");
                    hasPrimaryKeyIdentity = true;
                }
            }
            fieldInfoList.Add(sbFieldInfo.ToString());
        }
        if (!hasPrimaryKeyIdentity && entityInfo.FieldInfos.Any(c => c.IsPrimaryKey))
        {
            fieldInfoList.Add($"  PRIMARY KEY ({string.Join(",", entityInfo.FieldInfos.Where(c => c.IsPrimaryKey).Select(c => _dbType.MarkAsIdentifier(c.FieldName)).ToList())})");
        }
        sb.AppendLine(string.Join($",{Environment.NewLine}", fieldInfoList));
        sb.AppendLine(");");
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
            sb.Append(";");
            result.Add(sb.ToString());
        });
        return result;
    }
}
