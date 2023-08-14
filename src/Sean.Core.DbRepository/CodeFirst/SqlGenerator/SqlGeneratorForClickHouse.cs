using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository.CodeFirst;

public class SqlGeneratorForClickHouse : BaseSqlGenerator, ISqlGenerator
{
    public SqlGeneratorForClickHouse() : base(DatabaseType.ClickHouse)
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
                    //var maxLength = fieldPropertyInfo.GetCustomAttribute<StringLengthAttribute>()?.MaximumLength ??
                    //                fieldPropertyInfo.GetCustomAttribute<MaxLengthAttribute>()?.Length ?? 0;
                    result = "String";
                    break;
                }
            case not null when underlyingType == typeof(DateTime):
                result = "DateTime";
                break;
            case not null when underlyingType == typeof(decimal):
                {
                    var numberAttr = fieldPropertyInfo.GetCustomAttribute<NumericAttribute>();
                    result = numberAttr != null ? $"Decimal({numberAttr.Precision},{numberAttr.Scale})" : "Decimal";
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
        var tableName = entityInfo.MainTableName;
        if (tableNameFunc != null)
        {
            tableName = tableNameFunc(tableName);
        }
        sb.AppendLine($"CREATE TABLE {_dbType.MarkAsTableOrFieldName(tableName)} (");
        var fieldInfoList = new List<string>();
        var sbFieldInfo = new StringBuilder();
        foreach (var fieldInfo in entityInfo.FieldInfos)
        {
            sbFieldInfo.Clear();
            var fieldPropertyInfo = fieldInfo.Property;
            var fieldName = fieldInfo.FieldName;
            var fieldDescription = GetFieldDescription(fieldPropertyInfo);
            sbFieldInfo.Append($"  {_dbType.MarkAsTableOrFieldName(fieldName)}");
            sbFieldInfo.Append(IsNotAllowNull(fieldPropertyInfo)
                ? $" {ConvertFieldType(fieldPropertyInfo)} NOT NULL"
                : $" Nullable({ConvertFieldType(fieldPropertyInfo)})");
            var fieldDefaultValue = GetFieldDefaultValue(fieldPropertyInfo);
            if (fieldDefaultValue != null)
            {
                sbFieldInfo.Append($" DEFAULT {ConvertFieldDefaultValue(fieldDefaultValue)}");
            }
            //if (fieldInfo.Identity)
            //{
            //    sbFieldInfo.Append(" AUTO_INCREMENT");
            //}
            if (!string.IsNullOrWhiteSpace(fieldDescription))
            {
                sbFieldInfo.Append($" COMMENT '{fieldDescription}'");
            }
            fieldInfoList.Add(sbFieldInfo.ToString());
        }
        if (entityInfo.FieldInfos.Any(c => c.PrimaryKey))
        {
            fieldInfoList.Add($"  PRIMARY KEY ({string.Join(",", entityInfo.FieldInfos.Where(c => c.PrimaryKey).Select(c => _dbType.MarkAsTableOrFieldName(c.FieldName)).ToList())})");
        }
        sb.AppendLine(string.Join($",{Environment.NewLine}", fieldInfoList));
        sb.Append(") ENGINE = MergeTree()");
        var tableDescription = GetTableDescription(entityType);
        if (!string.IsNullOrWhiteSpace(tableDescription))
        {
            sb.Append($" COMMENT '{tableDescription}'");
        }
        sb.Append(";");
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
        var tableName = entityInfo.MainTableName;
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
        missingTableFieldInfo?.ForEach(c =>
        {
            sb.Clear();
            var fieldDescription = GetFieldDescription(c.Property);
            sb.Append($"ALTER TABLE {_dbType.MarkAsTableOrFieldName(tableName)} ADD {_dbType.MarkAsTableOrFieldName(c.FieldName)}");
            sb.Append(IsNotAllowNull(c.Property)
                ? $" {ConvertFieldType(c.Property)} NOT NULL"
                : $" Nullable({ConvertFieldType(c.Property)})");
            var fieldDefaultValue = GetFieldDefaultValue(c.Property);
            if (fieldDefaultValue != null)
            {
                sb.Append($" DEFAULT {ConvertFieldDefaultValue(fieldDefaultValue)}");
            }
            if (!string.IsNullOrWhiteSpace(fieldDescription))
            {
                sb.Append($" COMMENT '{fieldDescription}'");
            }
            sb.Append(";");
            result.Add(sb.ToString());
        });
        return result;
    }
}