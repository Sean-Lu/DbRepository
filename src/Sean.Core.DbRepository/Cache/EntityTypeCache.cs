using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using Sean.Core.DbRepository.Extensions;
using Sean.Utility.Extensions;

namespace Sean.Core.DbRepository;

/// <summary>
/// Table entity class type cache.
/// </summary>
public static class EntityTypeCache
{
    private static readonly ConcurrentDictionary<Type, EntityInfo> _entityInfoCache = new();

    public static EntityInfo GetEntityInfo(Type entityClassType)
    {
        if (entityClassType == null)
        {
            return null;
        }

        if (_entityInfoCache.TryGetValue(entityClassType, out var entityInfo) && entityInfo != null)// Try to get data from cache.
        {
            return entityInfo;
        }

        entityInfo = new EntityInfo
        {
            NamingConvention = DbContextConfiguration.Options.DefaultNamingConvention,
            FieldInfos = new List<TableFieldInfo>()
        };

        if (entityClassType.IsAnonymousType())
        {
            // 匿名类型特殊处理，不走缓存
            foreach (var propertyInfo in entityClassType.GetProperties())
            {
                entityInfo.FieldInfos.Add(new TableFieldInfo
                {
                    Property = propertyInfo,
                    FieldName = propertyInfo.Name
                });
            }
            return entityInfo;
        }

        var namingConvention = entityClassType.GetCustomAttributes<NamingConventionAttribute>(true).FirstOrDefault()?.NamingConvention;
        if (namingConvention.HasValue)
        {
            entityInfo.NamingConvention = namingConvention.Value;
        }

        var tableName = entityClassType.GetCustomAttributes<TableAttribute>(true).FirstOrDefault()?.Name;
        if (!string.IsNullOrWhiteSpace(tableName))
        {
            entityInfo.MainTableName = tableName;
        }
        else
        {
            var entityClassName = entityClassType.Name;
            const string entityClassSuffix = "Entity";
            if (entityClassName.EndsWith(entityClassSuffix) && entityClassName.Length > entityClassSuffix.Length)
            {
                entityClassName = entityClassName.Substring(0, entityClassName.Length - entityClassSuffix.Length);
            }
            entityInfo.MainTableName = entityClassName.ToNamingConvention(entityInfo.NamingConvention);
        }

        var propertyInfos = entityClassType.GetProperties();
        foreach (var propertyInfo in propertyInfos)
        {
            if (propertyInfo.GetCustomAttributes<NotMappedAttribute>(true).Any())
            {
                continue;
            }

            var fieldInfo = new TableFieldInfo
            {
                Property = propertyInfo,
                FieldName = propertyInfo.GetFieldName(entityInfo.NamingConvention),
                Order = propertyInfo.GetCustomAttributes<ColumnAttribute>(true).FirstOrDefault()?.Order,
                PrimaryKey = propertyInfo.GetCustomAttributes<KeyAttribute>(true).Any(),
                Identity = propertyInfo.GetCustomAttributes<DatabaseGeneratedAttribute>(true).Any(c => c.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity)
            };
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

        _entityInfoCache.AddOrUpdate(entityClassType, entityInfo, (_, _) => entityInfo);// Update cache.
        return entityInfo;
    }
}

public class EntityInfo
{
    /// <summary>
    /// 主表表名
    /// </summary>
    public string MainTableName { get; set; }
    public NamingConvention NamingConvention { get; set; }

    /// <summary>
    /// 所有字段信息
    /// </summary>
    public List<TableFieldInfo> FieldInfos { get; set; }
}

public class TableFieldInfo
{
    public PropertyInfo Property { get; set; }
    /// <summary>
    /// 字段名称
    /// </summary>
    public string FieldName { get; set; }
    public int? Order { get; set; }
    /// <summary>
    /// 是否是主键字段 <see cref="KeyAttribute"/>
    /// </summary>
    public bool PrimaryKey { get; set; }
    /// <summary>
    /// 是否是自增字段 <see cref="DatabaseGeneratedOption.Identity"/>
    /// </summary>
    public bool Identity { get; set; }
}