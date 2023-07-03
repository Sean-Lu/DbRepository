using System;
using Sean.Utility.Extensions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using Sean.Core.DbRepository.Extensions;
using System.ComponentModel;
using System.IO;
using System.Xml;

namespace Sean.Core.DbRepository.CodeFirst;

public abstract class BaseSqlGenerator : ISqlGenerator
{
    protected readonly DatabaseType _dbType;

    protected BaseSqlGenerator(DatabaseType dbType)
    {
        _dbType = dbType;
    }

    protected abstract string ConvertFieldType(PropertyInfo fieldPropertyInfo);

    public abstract string GetCreateTableSql<TEntity>(Func<string, string> tableNameFunc = null);

    public abstract string GetUpgradeSql<TEntity>(Func<string, string> tableNameFunc = null);

    protected virtual string GetTableName<TEntity>()
    {
        return typeof(TEntity).GetMainTableName();
    }

    protected virtual string GetTableDescription<TEntity>()
    {
        var entityType = typeof(TEntity);
        var tableDescription = entityType.GetCustomAttribute<DescriptionAttribute>()?.Description;
        if (string.IsNullOrWhiteSpace(tableDescription))
        {
            string filePath = Path.ChangeExtension(entityType.Assembly.Location, "xml");
            if (File.Exists(filePath))
            {
                XmlDocument xmlDoc = new XmlDocument();
                //xmlDoc.PreserveWhitespace = true;
                xmlDoc.Load(filePath);
                tableDescription = xmlDoc.SelectSingleNode($"//member[@name='T:{entityType.FullName}']")?.InnerText.Trim('\r', '\n', ' ');
            }
        }
        return tableDescription;
    }

    protected virtual string GetFieldName(PropertyInfo fieldPropertyInfo)
    {
        return fieldPropertyInfo.GetFieldName();
    }

    protected virtual string GetFieldDescription(PropertyInfo fieldPropertyInfo)
    {
        var fieldDescription = fieldPropertyInfo.GetCustomAttribute<DescriptionAttribute>()?.Description;
        if (string.IsNullOrWhiteSpace(fieldDescription))
        {
            string filePath = Path.ChangeExtension(fieldPropertyInfo.DeclaringType.Assembly.Location, "xml");
            if (File.Exists(filePath))
            {
                XmlDocument xmlDoc = new XmlDocument();
                //xmlDoc.PreserveWhitespace = true;
                xmlDoc.Load(filePath);
                fieldDescription = xmlDoc.SelectSingleNode($"//member[@name='P:{fieldPropertyInfo.DeclaringType.FullName}.{fieldPropertyInfo.Name}']")?.InnerText?.Trim('\r', '\n', ' ');
            }
        }
        return fieldDescription;
    }

    protected virtual bool IsNotAllowNull(PropertyInfo fieldPropertyInfo)
    {
        return fieldPropertyInfo.PropertyType.IsValueType && !fieldPropertyInfo.PropertyType.IsNullableType()
               || IsPrimaryKey(fieldPropertyInfo)
               || fieldPropertyInfo.PropertyType.GetCustomAttributes<RequiredAttribute>(false).Any();
    }

    protected virtual bool IsPrimaryKey(PropertyInfo fieldPropertyInfo)
    {
        return fieldPropertyInfo.PropertyType.GetCustomAttributes<KeyAttribute>(false).Any();
    }

    protected virtual bool IsIdentity(PropertyInfo fieldPropertyInfo)
    {
        return fieldPropertyInfo.PropertyType.GetCustomAttributes<DatabaseGeneratedAttribute>(false).Any(c => c.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity);
    }
}