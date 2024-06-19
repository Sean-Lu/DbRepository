using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository.CodeFirst;

public class SqlGeneratorForMsAccess : BaseSqlGenerator, ISqlGenerator
{
    public SqlGeneratorForMsAccess() : base(DatabaseType.MsAccess)
    {
    }

    protected virtual string ConvertFieldType(PropertyInfo fieldPropertyInfo)
    {
        var columnAttribute = fieldPropertyInfo.GetCustomAttribute<ColumnAttribute>(true);
        if (!string.IsNullOrWhiteSpace(columnAttribute?.TypeName))
        {
            return columnAttribute.TypeName;
        }

        var underlyingType = Nullable.GetUnderlyingType(fieldPropertyInfo.PropertyType) ?? fieldPropertyInfo.PropertyType;
        string result;
        switch (underlyingType)
        {
            case not null when underlyingType == typeof(long):
                result = "LONG";
                break;
            case not null when underlyingType == typeof(int) || underlyingType.IsEnum:
                result = "INTEGER";
                break;
            case not null when underlyingType == typeof(bool):
                result = "BIT";
                break;
            case not null when underlyingType == typeof(string):
                {
                    var maxLength = fieldPropertyInfo.GetCustomAttribute<StringLengthAttribute>(true)?.MaximumLength ??
                                    fieldPropertyInfo.GetCustomAttribute<MaxLengthAttribute>(true)?.Length ?? 0;
                    result = maxLength > 0 ? $"VARCHAR({maxLength})" : "VARCHAR";
                    break;
                }
            case not null when underlyingType == typeof(DateTime):
                result = "TIMESTAMP";
                break;
            case not null when underlyingType == typeof(decimal):
                {
                    var numberAttr = fieldPropertyInfo.GetCustomAttribute<NumericAttribute>(true);
                    result = numberAttr != null ? $"DECIMAL({numberAttr.Precision},{numberAttr.Scale})" : "DECIMAL";
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
            if (!fieldInfo.Identity)
            {
                sbFieldInfo.Append($"  {_dbType.MarkAsTableOrFieldName(fieldName)} {ConvertFieldType(fieldPropertyInfo)}");
                if (IsNotAllowNull(fieldPropertyInfo))
                {
                    sbFieldInfo.Append(" NOT NULL");
                }
            }
            else
            {
                sbFieldInfo.Append($"  {_dbType.MarkAsTableOrFieldName(fieldName)} AUTOINCREMENT NOT NULL");
            }
            var fieldDefaultValue = GetFieldDefaultValue(fieldPropertyInfo);
            if (fieldDefaultValue != null)
            {
                sbFieldInfo.Append($" DEFAULT {ConvertFieldDefaultValue(fieldDefaultValue)}");
            }
            fieldInfoList.Add(sbFieldInfo.ToString());
        }
        if (entityInfo.FieldInfos.Any(c => c.PrimaryKey))
        {
            fieldInfoList.Add($"  PRIMARY KEY ({string.Join(",", entityInfo.FieldInfos.Where(c => c.PrimaryKey).Select(c => _dbType.MarkAsTableOrFieldName(c.FieldName)).ToList())})");
        }
        sb.AppendLine(string.Join($",{Environment.NewLine}", fieldInfoList));
        sb.Append(");");
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
            sb.Append($"ALTER TABLE {_dbType.MarkAsTableOrFieldName(tableName)} ADD {_dbType.MarkAsTableOrFieldName(c.FieldName)} {ConvertFieldType(c.Property)}");
            if (IsNotAllowNull(c.Property))
            {
                sb.Append(" NOT NULL");
            }
            var fieldDefaultValue = GetFieldDefaultValue(c.Property);
            if (fieldDefaultValue != null)
            {
                sb.Append($" DEFAULT {ConvertFieldDefaultValue(fieldDefaultValue)}");
            }
            sb.Append(";");
            result.Add(sb.ToString());
        });
        return result;
    }
}