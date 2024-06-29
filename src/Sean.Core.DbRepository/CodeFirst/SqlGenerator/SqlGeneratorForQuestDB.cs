using System;
using System.Collections.Generic;
using System.Text;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository.CodeFirst;

public class SqlGeneratorForQuestDB : BaseSqlGenerator, ISqlGenerator
{
    public SqlGeneratorForQuestDB() : base(DatabaseType.QuestDB)
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
                result = "LONG";
                break;
            case not null when underlyingType == typeof(int) || underlyingType.IsEnum:
                result = "INT";
                break;
            case not null when underlyingType == typeof(bool):
                result = "BOOLEAN";
                break;
            case not null when underlyingType == typeof(string):
                {
                    result = "STRING";
                    break;
                }
            case not null when underlyingType == typeof(DateTime):
                result = "TIMESTAMP";
                break;
            case not null when underlyingType == typeof(decimal) || underlyingType == typeof(double):
                {
                    result = "DOUBLE";
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
        foreach (var fieldInfo in entityInfo.FieldInfos)
        {
            sbFieldInfo.Clear();
            sbFieldInfo.Append($"  {_dbType.MarkAsTableOrFieldName(fieldInfo.FieldName)} {ConvertFieldType(fieldInfo)}");
            if (fieldInfo.IsNotAllowNull)
            {
                sbFieldInfo.Append(" NOT NULL");
            }
            if (fieldInfo.FieldDefaultValue != null)
            {
                sbFieldInfo.Append($" DEFAULT {ConvertFieldDefaultValue(fieldInfo.FieldDefaultValue)}");
            }
            //if (!string.IsNullOrWhiteSpace(fieldInfo.FieldDescription))
            //{
            //    sbFieldInfo.Append($" COMMENT '{fieldInfo.FieldDescription}'");
            //}
            fieldInfoList.Add(sbFieldInfo.ToString());
        }
        //if (entityInfo.FieldInfos.Any(c => c.IsPrimaryKey))
        //{
        //    fieldInfoList.Add($"  PRIMARY KEY ({string.Join(",", entityInfo.FieldInfos.Where(c => c.IsPrimaryKey).Select(c => _dbType.MarkAsTableOrFieldName(c.FieldName)).ToList())})");
        //}
        sb.AppendLine(string.Join($",{Environment.NewLine}", fieldInfoList));
        sb.Append(")");
        //if (!string.IsNullOrWhiteSpace(entityInfo.TableDescription))
        //{
        //    sb.Append($" COMMENT '{entityInfo.TableDescription}'");
        //}
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
        missingTableFieldInfo?.ForEach(fieldInfo =>
        {
            sb.Clear();
            sb.Append($"ALTER TABLE {_dbType.MarkAsTableOrFieldName(tableName)} ADD {_dbType.MarkAsTableOrFieldName(fieldInfo.FieldName)} {ConvertFieldType(fieldInfo)}");
            if (fieldInfo.IsNotAllowNull)
            {
                sb.Append(" NOT NULL");
            }
            if (fieldInfo.FieldDefaultValue != null)
            {
                sb.Append($" DEFAULT {ConvertFieldDefaultValue(fieldInfo.FieldDefaultValue)}");
            }
            //if (!string.IsNullOrWhiteSpace(fieldInfo.FieldDescription))
            //{
            //    sb.Append($" COMMENT '{fieldInfo.FieldDescription}'");
            //}
            sb.Append(";");
            result.Add(sb.ToString());
        });
        return result;
    }
}