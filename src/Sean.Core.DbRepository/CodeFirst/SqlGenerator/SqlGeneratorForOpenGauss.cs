﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository.CodeFirst;

public class SqlGeneratorForOpenGauss : BaseSqlGenerator
{
    public SqlGeneratorForOpenGauss() : base(DatabaseType.OpenGauss)
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
                result = "integer";
                break;
            case not null when underlyingType == typeof(bool):
                result = "boolean";
                break;
            case not null when underlyingType == typeof(string):
                {
                    result = fieldInfo.MaxLength.HasValue ? $"varchar({fieldInfo.MaxLength.Value})" : "text";
                    break;
                }
            case not null when underlyingType == typeof(DateTime):
                result = "timestamp with time zone";
                break;
            case not null when underlyingType == typeof(decimal):
                {
                    result = fieldInfo.NumericPrecision.HasValue ? $"numeric({fieldInfo.NumericPrecision.GetValueOrDefault()},{fieldInfo.NumericScale.GetValueOrDefault()})" : "numeric";
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
        var tableName = entityInfo.TableName;
        if (tableNameFunc != null)
        {
            tableName = tableNameFunc(tableName);
        }
        sb.AppendLine($"CREATE TABLE{(ignoreIfExists ? " IF NOT EXISTS" : string.Empty)} {_dbType.MarkAsTableOrFieldName(tableName)} (");
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
            if (!fieldInfo.IsIdentityField)
            {
                sbFieldInfo.Append($"  {_dbType.MarkAsTableOrFieldName(fieldInfo.FieldName)} {ConvertFieldType(fieldInfo)}");
                if (fieldInfo.IsNotAllowNull)
                {
                    sbFieldInfo.Append(" NOT NULL");
                }
                if (fieldInfo.FieldDefaultValue != null)
                {
                    sbFieldInfo.Append($" DEFAULT {ConvertFieldDefaultValue(fieldInfo.FieldDefaultValue)}");
                }
            }
            else
            {
                sbFieldInfo.Append($"  {_dbType.MarkAsTableOrFieldName(fieldInfo.FieldName)} SERIAL");
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
            sb.AppendLine($"COMMENT ON TABLE {_dbType.MarkAsTableOrFieldName(tableName)} IS '{entityInfo.TableDescription}';");
        }
        foreach (var kv in fieldDescriptionDic)
        {
            sb.AppendLine($"COMMENT ON COLUMN {_dbType.MarkAsTableOrFieldName(tableName)}.{_dbType.MarkAsTableOrFieldName(kv.Key)} IS '{kv.Value}';");
        }
        result.Add(sb.ToString());
        return result;
    }

    public override List<string> GetUpgradeSql(Type entityType, Func<string, string> tableNameFunc = null)
    {
        var entityInfo = entityType.GetEntityInfo();
        var tableName = entityInfo.TableName;
        if (tableNameFunc != null)
        {
            tableName = tableNameFunc(tableName);
        }
        if (!IsTableExists(tableName))
        {
            return GetCreateTableSql(entityType, false, _ => tableName);
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
            result.Add($"COMMENT ON COLUMN {_dbType.MarkAsTableOrFieldName(tableName)}.{_dbType.MarkAsTableOrFieldName(kv.Key)} IS '{kv.Value}';");
        }
        return result;
    }
}