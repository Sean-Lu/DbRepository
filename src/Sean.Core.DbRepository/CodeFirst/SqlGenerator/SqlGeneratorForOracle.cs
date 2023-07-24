﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository.CodeFirst;

public class SqlGeneratorForOracle : BaseSqlGenerator, ISqlGenerator
{
    public SqlGeneratorForOracle() : base(DatabaseType.Oracle)
    {
    }

    protected virtual string ConvertFieldType(PropertyInfo fieldPropertyInfo)
    {
        var columnAttribute = fieldPropertyInfo.GetCustomAttribute<ColumnAttribute>();
        if (!string.IsNullOrWhiteSpace(columnAttribute?.TypeName))
        {
            return columnAttribute.TypeName;
        }

        var underlyingType = Nullable.GetUnderlyingType(fieldPropertyInfo.PropertyType) ?? fieldPropertyInfo.PropertyType;
        string result;
        switch (underlyingType)
        {
            case not null when underlyingType == typeof(long):
                result = "NUMBER(19)";
                break;
            case not null when underlyingType == typeof(int) || underlyingType.IsEnum:
                result = "NUMBER(10)";
                break;
            case not null when underlyingType == typeof(bool):
                result = "NUMBER(1)";
                break;
            case not null when underlyingType == typeof(string):
                {
                    var maxLength = fieldPropertyInfo.GetCustomAttribute<StringLengthAttribute>()?.MaximumLength ??
                                    fieldPropertyInfo.GetCustomAttribute<MaxLengthAttribute>()?.Length ?? 0;
                    result = maxLength > 0 ? $"NVARCHAR2({maxLength})" : "NVARCHAR2";
                    break;
                }
            case not null when underlyingType == typeof(DateTime):
                result = "TIMESTAMP(7)";
                break;
            case not null when underlyingType == typeof(decimal):
                {
                    var numberAttr = fieldPropertyInfo.GetCustomAttribute<NumberAttribute>();
                    result = numberAttr != null ? $"DECIMAL({numberAttr.Precision},{numberAttr.Scale})" : "DECIMAL";
                    break;
                }
            default:
                result = $"##{underlyingType.Name}##";
                break;
        }
        return result;
    }

    public virtual string GetCreateTableSql<TEntity>(Func<string, string> tableNameFunc = null)
    {
        var sb = new StringBuilder();
        var entityInfo = typeof(TEntity).GetEntityInfo();
        var tableName = entityInfo.MainTableName;
        if (tableNameFunc != null)
        {
            tableName = tableNameFunc(tableName);
        }
        sb.AppendLine("BEGIN");
        sb.AppendLine($"execute immediate 'CREATE TABLE {_dbType.MarkAsTableOrFieldName(tableName)} (");
        var fieldInfoList = new List<string>();
        var sbFieldInfo = new StringBuilder();
        var fieldDescriptionDic = new Dictionary<string, string>();
        foreach (var fieldInfo in entityInfo.FieldInfos)
        {
            sbFieldInfo.Clear();
            var fieldPropertyInfo = fieldInfo.Property;
            var fieldName = fieldInfo.FieldName;
            var fieldDescription = GetFieldDescription(fieldPropertyInfo);
            if (!string.IsNullOrWhiteSpace(fieldDescription))
            {
                fieldDescriptionDic.Add(fieldName, fieldDescription);
            }
            sbFieldInfo.Append($"  {_dbType.MarkAsTableOrFieldName(fieldName)} {ConvertFieldType(fieldPropertyInfo)}");
            if (fieldInfo.Identity)
            {
                sbFieldInfo.Append(" GENERATED BY DEFAULT ON NULL AS IDENTITY");
            }
            if (IsNotAllowNull(fieldPropertyInfo))
            {
                sbFieldInfo.Append(" NOT NULL");
            }
            fieldInfoList.Add(sbFieldInfo.ToString());
        }
        if (entityInfo.FieldInfos.Any(c => c.PrimaryKey))
        {
            fieldInfoList.Add($"  PRIMARY KEY ({string.Join(",", entityInfo.FieldInfos.Where(c => c.PrimaryKey).Select(c => _dbType.MarkAsTableOrFieldName(c.FieldName)).ToList())})");
        }
        sb.AppendLine(string.Join($",{Environment.NewLine}", fieldInfoList));
        sb.AppendLine(")';");
        var tableDescription = GetTableDescription<TEntity>();
        if (!string.IsNullOrWhiteSpace(tableDescription))
        {
            sb.AppendLine($"execute immediate 'COMMENT ON TABLE {_dbType.MarkAsTableOrFieldName(tableName)} IS ''{tableDescription}''';");
        }
        foreach (var kv in fieldDescriptionDic)
        {
            sb.AppendLine($"execute immediate 'COMMENT ON COLUMN {_dbType.MarkAsTableOrFieldName(tableName)}.{_dbType.MarkAsTableOrFieldName(kv.Key)} IS ''{kv.Value}''';");
        }
        sb.Append("END;");
        return sb.ToString();
    }

    public virtual string GetUpgradeSql<TEntity>(Func<string, string> tableNameFunc = null)
    {
        throw new NotImplementedException();
    }
}