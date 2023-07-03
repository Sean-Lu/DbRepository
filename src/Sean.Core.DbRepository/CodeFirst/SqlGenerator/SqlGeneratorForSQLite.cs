using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository.CodeFirst;

public class SqlGeneratorForSQLite : BaseSqlGenerator, ISqlGenerator
{
    public SqlGeneratorForSQLite() : base(DatabaseType.SQLite)
    {
    }

    protected override string ConvertFieldType(PropertyInfo fieldPropertyInfo)
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
            case not null when underlyingType == typeof(long) || underlyingType == typeof(int) ||
                               underlyingType == typeof(bool) || underlyingType.IsEnum:
                result = "INTEGER";
                break;
            case not null when underlyingType == typeof(string) || underlyingType == typeof(DateTime):
                result = "TEXT";
                break;
            case not null when underlyingType == typeof(decimal):
                {
                    var numberAttr = fieldPropertyInfo.GetCustomAttribute<NumberAttribute>();
                    result = numberAttr != null ? $"DECIMAL({numberAttr.Precision},{numberAttr.Scale})" : "DECIMAL";
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

    public override string GetCreateTableSql<TEntity>(Func<string, string> tableNameFunc = null)
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
        var hasPrimaryKeyIdentity = false;
        foreach (var fieldInfo in entityInfo.FieldInfos)
        {
            sbFieldInfo.Clear();
            var fieldPropertyInfo = fieldInfo.Property;
            var fieldName = fieldInfo.FieldName;
            sbFieldInfo.Append($"  {_dbType.MarkAsTableOrFieldName(fieldName)} {ConvertFieldType(fieldPropertyInfo)}");
            if (IsNotAllowNull(fieldPropertyInfo))
            {
                sbFieldInfo.Append(" NOT NULL");
            }
            if (fieldInfo.Identity)
            {
                if (fieldInfo.PrimaryKey)
                {
                    sbFieldInfo.Append(" PRIMARY KEY");
                    hasPrimaryKeyIdentity = true;
                }
                sbFieldInfo.Append(" AUTOINCREMENT");
            }
            fieldInfoList.Add(sbFieldInfo.ToString());
        }
        if (!hasPrimaryKeyIdentity && entityInfo.FieldInfos.Any(c => c.PrimaryKey))
        {
            fieldInfoList.Add($"  PRIMARY KEY ({string.Join(",", entityInfo.FieldInfos.Where(c => c.PrimaryKey).Select(c => _dbType.MarkAsTableOrFieldName(c.FieldName)).ToList())})");
        }
        sb.AppendLine(string.Join($",{Environment.NewLine}", fieldInfoList));
        sb.Append(");");
        return sb.ToString();
    }

    public override string GetUpgradeSql<TEntity>(Func<string, string> tableNameFunc = null)
    {
        throw new NotImplementedException();
    }
}