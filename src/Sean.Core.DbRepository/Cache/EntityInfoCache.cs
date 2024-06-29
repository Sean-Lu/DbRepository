using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using Sean.Core.DbRepository.Extensions;
using Sean.Utility.Extensions;

namespace Sean.Core.DbRepository;

public static class EntityInfoCache
{
    private static readonly ConcurrentDictionary<Type, EntityInfo> _entityInfoCache = new();

    public static int Count()
    {
        return _entityInfoCache.Count;
    }

    public static bool ContainsKey(Type entityClassType)
    {
        return _entityInfoCache.ContainsKey(entityClassType);
    }

    public static List<Type> Keys()
    {
        return _entityInfoCache.Keys.ToList();
    }

    public static EntityInfo Get(Type entityClassType)
    {
        if (entityClassType == null)
        {
            return null;
        }

        if (_entityInfoCache.TryGetValue(entityClassType, out var entityInfo) && entityInfo != null)// Try to get entity info from cache.
        {
            return entityInfo;
        }

        entityInfo = new EntityInfo
        {
            NamingConvention = DbContextConfiguration.Options.DefaultNamingConvention,
            FieldInfos = new List<EntityFieldInfo>()
        };

        if (entityClassType.IsAnonymousType())
        {
            // 匿名类型特殊处理，不走缓存
            foreach (var propertyInfo in entityClassType.GetProperties())
            {
                entityInfo.FieldInfos.Add(new EntityFieldInfo
                {
                    Property = propertyInfo,
                    PropertyName = propertyInfo.Name,
                    FieldName = propertyInfo.Name
                });
            }
            return entityInfo;
        }

        var namingConvention = entityClassType.GetCustomAttribute<NamingConventionAttribute>(true)?.NamingConvention;
        if (namingConvention.HasValue)
        {
            entityInfo.NamingConvention = namingConvention.Value;
        }

        var tableName = entityClassType.GetCustomAttribute<TableAttribute>(true)?.Name;
        if (!string.IsNullOrWhiteSpace(tableName))
        {
            entityInfo.TableName = tableName;
        }
        else
        {
            var entityClassName = entityClassType.Name;
            const string entityClassSuffix = "Entity";
            if (entityClassName.EndsWith(entityClassSuffix) && entityClassName.Length > entityClassSuffix.Length)
            {
                entityClassName = entityClassName.Substring(0, entityClassName.Length - entityClassSuffix.Length);
            }
            entityInfo.TableName = entityClassName.ToNamingConvention(entityInfo.NamingConvention);
        }

        entityInfo.TableDescription = GetTableDescription(entityClassType);

        var propertyInfos = entityClassType.GetProperties();
        foreach (var propertyInfo in propertyInfos)
        {
            if (propertyInfo.IsNotMappedField())
            {
                continue;
            }

            var columnAttr = propertyInfo.GetCustomAttribute<ColumnAttribute>(true);
            var fieldInfo = new EntityFieldInfo
            {
                Property = propertyInfo,
                PropertyName = propertyInfo.Name,
                FieldName = propertyInfo.GetFieldName(entityInfo.NamingConvention),
                FieldTypeName = columnAttr?.TypeName,
                FieldDefaultValue = propertyInfo.GetCustomAttribute<DefaultValueAttribute>(true)?.Value,
                FieldDescription = GetFieldDescription(propertyInfo),
                Order = columnAttr?.Order,
                MaxLength = propertyInfo.GetCustomAttribute<MaxLengthAttribute>(true)?.Length,
                IsPrimaryKey = propertyInfo.IsPrimaryKey(),
                IsIdentityField = propertyInfo.IsIdentityField(),
                IsRequiredField = propertyInfo.GetCustomAttributes<RequiredAttribute>(true).Any()
            };
            var numberAttr = propertyInfo.GetCustomAttribute<NumericAttribute>(true);
            if (numberAttr != null)
            {
                fieldInfo.NumericPrecision = numberAttr.Precision;
                fieldInfo.NumericScale = numberAttr.Scale;
            }
            fieldInfo.IsNotAllowNull = fieldInfo.IsPrimaryKey || fieldInfo.IsRequiredField;
            entityInfo.FieldInfos.Add(fieldInfo);
        }

        if (entityInfo.FieldInfos.Any(c => c.Order > 0))
        {
            var orderedFieldInfos = entityInfo.FieldInfos
                .Where(c => c.Order > 0)
                .OrderBy(c => c.Order)
                .ToList();
            var nonOrderedFieldInfos = entityInfo.FieldInfos
                .Except(orderedFieldInfos)
                .ToList();
            entityInfo.FieldInfos = orderedFieldInfos.Concat(nonOrderedFieldInfos).ToList();
        }

        _entityInfoCache.AddOrUpdate(entityClassType, entityInfo, (_, _) => entityInfo);// Save entity info into cache.
        return entityInfo;
    }

    public static EntityInfo Remove(Type entityClassType)
    {
        _entityInfoCache.TryRemove(entityClassType, out var entityInfoRemoved);
        return entityInfoRemoved;
    }

    public static void Clear()
    {
        _entityInfoCache.Clear();
    }

    private static string GetTableDescription(Type entityClassType)
    {
        var tableDescription = entityClassType.GetCustomAttribute<DescriptionAttribute>(true)?.Description;
        if (string.IsNullOrWhiteSpace(tableDescription))
        {
            var filePath = Path.ChangeExtension(entityClassType.Assembly.Location, "xml");
            if (File.Exists(filePath))
            {
                var xmlDoc = new XmlDocument();
                //xmlDoc.PreserveWhitespace = true;
                xmlDoc.Load(filePath);
                tableDescription = xmlDoc.SelectSingleNode($"//member[@name='T:{entityClassType.FullName}']")?.InnerText.Trim('\r', '\n', ' ');
            }
        }
        return tableDescription;
    }

    private static string GetFieldDescription(MemberInfo memberInfo)
    {
        var fieldDescription = memberInfo.GetCustomAttribute<DescriptionAttribute>(true)?.Description;
        if (string.IsNullOrWhiteSpace(fieldDescription))
        {
            var filePath = Path.ChangeExtension(memberInfo.DeclaringType.Assembly.Location, "xml");
            if (File.Exists(filePath))
            {
                var xmlDoc = new XmlDocument();
                //xmlDoc.PreserveWhitespace = true;
                xmlDoc.Load(filePath);
                fieldDescription = xmlDoc.SelectSingleNode($"//member[@name='P:{memberInfo.DeclaringType.FullName}.{memberInfo.Name}']")?.InnerText?.Trim('\r', '\n', ' ');
            }
        }
        return fieldDescription;
    }
}

public class EntityInfo
{
    public NamingConvention NamingConvention { get; set; }

    public string TableName { get; set; }
    public string TableDescription { get; set; }

    /// <summary>
    /// 所有字段信息
    /// </summary>
    public List<EntityFieldInfo> FieldInfos { get; set; }
}

public class EntityFieldInfo
{
    public PropertyInfo Property { get; set; }
    public string PropertyName { get; set; }

    /// <summary>
    /// 字段名称
    /// </summary>
    public string FieldName { get; set; }
    public string FieldTypeName { get; set; }
    public object FieldDefaultValue { get; set; }
    public string FieldDescription { get; set; }
    public int? Order { get; set; }
    public int? MaxLength { get; set; }
    public int? NumericPrecision { get; set; }
    public int? NumericScale { get; set; }

    /// <summary>
    /// 是否是主键字段 <see cref="KeyAttribute"/>
    /// </summary>
    public bool IsPrimaryKey { get; set; }
    /// <summary>
    /// 是否是自增字段 <see cref="DatabaseGeneratedOption.Identity"/>
    /// </summary>
    public bool IsIdentityField { get; set; }
    public bool IsRequiredField { get; set; }
    public bool IsNotAllowNull { get; set; }
}