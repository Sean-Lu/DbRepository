using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository.CodeFirst;

public class SqlGeneratorForQuestDB : BaseSqlGenerator, ISqlGenerator
{
    public SqlGeneratorForQuestDB() : base(DatabaseType.QuestDB)
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
                    //var maxLength = fieldPropertyInfo.GetCustomAttribute<StringLengthAttribute>()?.MaximumLength ??
                    //                fieldPropertyInfo.GetCustomAttribute<MaxLengthAttribute>()?.Length ?? 0;
                    result = "STRING";
                    break;
                }
            case not null when underlyingType == typeof(DateTime):
                result = "TIMESTAMP";
                break;
            case not null when underlyingType == typeof(decimal) || underlyingType == typeof(double):
                {
                    //var numberAttr = fieldPropertyInfo.GetCustomAttribute<NumberAttribute>();
                    result = "DOUBLE";
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
        sb.AppendLine($"CREATE TABLE {_dbType.MarkAsTableOrFieldName(tableName)} (");
        var fieldInfoList = new List<string>();
        var sbFieldInfo = new StringBuilder();
        foreach (var fieldInfo in entityInfo.FieldInfos)
        {
            sbFieldInfo.Clear();
            var fieldPropertyInfo = fieldInfo.Property;
            var fieldName = fieldInfo.FieldName;
            //var fieldDescription = GetFieldDescription(fieldPropertyInfo);
            sbFieldInfo.Append($"  {_dbType.MarkAsTableOrFieldName(fieldName)} {ConvertFieldType(fieldPropertyInfo)}");
            if (IsNotAllowNull(fieldPropertyInfo))
            {
                sbFieldInfo.Append(" NOT NULL");
            }
            //if (fieldInfo.Identity)
            //{
            //    sbFieldInfo.Append(" IDENTITY(1,1)");
            //}
            //if (!string.IsNullOrWhiteSpace(fieldDescription))
            //{
            //    sbFieldInfo.Append($" COMMENT '{fieldDescription}'");
            //}
            fieldInfoList.Add(sbFieldInfo.ToString());
        }
        //if (entityInfo.FieldInfos.Any(c => c.PrimaryKey))
        //{
        //    fieldInfoList.Add($"  PRIMARY KEY ({string.Join(",", entityInfo.FieldInfos.Where(c => c.PrimaryKey).Select(c => _dbType.MarkAsTableOrFieldName(c.FieldName)).ToList())})");
        //}
        sb.AppendLine(string.Join($",{Environment.NewLine}", fieldInfoList));
        sb.Append(")");
        //var tableDescription = GetTableDescription<TEntity>();
        //if (!string.IsNullOrWhiteSpace(tableDescription))
        //{
        //    sb.Append($" COMMENT '{tableDescription}'");
        //}
        sb.Append(";");
        return sb.ToString();
    }

    public virtual string GetUpgradeSql<TEntity>(Func<string, string> tableNameFunc = null)
    {
        throw new NotImplementedException();
    }
}