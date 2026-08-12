using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using Sean.Core.DbRepository.Extensions;
using Sean.Utility.Extensions;

namespace Sean.Core.DbRepository;

public static class EntityInfoCache
{
    private static readonly ConcurrentDictionary<Type, EntityInfo> _entityInfoCache = new();
    private static readonly ConditionalWeakTable<Type, object> _entityBuildLocks = new();
    private static readonly ConcurrentDictionary<Assembly, IReadOnlyDictionary<string, string>> _xmlDescriptionCache = new();
    private static readonly ConditionalWeakTable<Assembly, object> _xmlBuildLocks = new();

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

        // Try to get entity info from cache.
        if (_entityInfoCache.TryGetValue(entityClassType, out var entityInfo) && entityInfo != null)
        {
            return entityInfo;
        }

        if (entityClassType.IsAnonymousType())
        {
            return BuildAnonymousTypeInfo(entityClassType);
        }

        // ConcurrentDictionary 的值工厂可能并行执行；类型级锁保证只构建并发布一个完整实例。
        var buildLock = _entityBuildLocks.GetValue(entityClassType, _ => new object());
        lock (buildLock)
        {
            if (_entityInfoCache.TryGetValue(entityClassType, out entityInfo) && entityInfo != null)
            {
                return entityInfo;
            }

            entityInfo = BuildEntityInfo(entityClassType);
            _entityInfoCache[entityClassType] = entityInfo;
            return entityInfo;
        }
    }

    private static EntityInfo BuildAnonymousTypeInfo(Type entityClassType)
    {
        var entityInfo = new EntityInfo
        {
            NamingConvention = DbContextConfiguration.Options.DefaultNamingConvention,
            FieldInfos = new List<EntityFieldInfo>()
        };

        // 匿名类型特殊处理，不走缓存，保留每次返回独立元数据实例的现有语义。
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

    private static EntityInfo BuildEntityInfo(Type entityClassType)
    {
        var entityInfo = new EntityInfo
        {
            NamingConvention = DbContextConfiguration.Options.DefaultNamingConvention,
            FieldInfos = new List<EntityFieldInfo>()
        };

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

        entityInfo.JoinInfos = new List<JoinDescriptor>();
        var leftJoinAttributes = entityClassType.GetCustomAttributes<LeftJoinAttribute>();
        foreach (var leftJoinAttr in leftJoinAttributes)
        {
            if (!string.IsNullOrWhiteSpace(leftJoinAttr.Alias) && entityInfo.JoinInfos.Exists(c => c.Alias == leftJoinAttr.Alias))
            {
                throw new Exception($"别名为 {leftJoinAttr.Alias} 的Join配置重复");
            }

            entityInfo.JoinInfos.Add(new JoinDescriptor
            {
                JoinTableType = leftJoinAttr.JoinTableType,
                LocalKey = leftJoinAttr.LocalKey,
                ForeignKey = leftJoinAttr.ForeignKey,
                Alias = leftJoinAttr.Alias
            });
        }

        entityInfo.IndexInfos = new List<IndexDescriptor>();
        var indexAttributes = entityClassType.GetCustomAttributes<IndexAttribute>();
        foreach (var indexAttribute in indexAttributes)
        {
            entityInfo.IndexInfos.Add(new IndexDescriptor
            {
                IndexPropertyNames = indexAttribute.IndexPropertyNames,
                IndexName = indexAttribute.IndexName,
                IndexType = indexAttribute.IndexType
            });
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

        return entityInfo;
    }

    public static EntityInfo Remove(Type entityClassType)
    {
        _entityInfoCache.TryRemove(entityClassType, out var entityInfoRemoved);
        if (entityClassType != null)
        {
            // 保留 Remove 后重新读取 XML 注释的刷新能力。
            _xmlDescriptionCache.TryRemove(entityClassType.Assembly, out _);
        }
        return entityInfoRemoved;
    }

    public static void Clear()
    {
        _entityInfoCache.Clear();
        _xmlDescriptionCache.Clear();
    }

    private static string GetTableDescription(Type entityClassType)
    {
        var tableDescription = entityClassType.GetCustomAttribute<DescriptionAttribute>(true)?.Description;
        if (string.IsNullOrWhiteSpace(tableDescription))
        {
            tableDescription = GetXmlDescription(entityClassType.Assembly,
                $"T:{entityClassType.FullName}");
        }
        return tableDescription;
    }

    private static string GetFieldDescription(MemberInfo memberInfo)
    {
        var fieldDescription = memberInfo.GetCustomAttribute<DescriptionAttribute>(true)?.Description;
        if (string.IsNullOrWhiteSpace(fieldDescription))
        {
            fieldDescription = GetXmlDescription(memberInfo.DeclaringType.Assembly,
                $"P:{memberInfo.DeclaringType.FullName}.{memberInfo.Name}");
        }
        return fieldDescription;
    }

    private static string GetXmlDescription(Assembly assembly, string memberName)
    {
        if (!_xmlDescriptionCache.TryGetValue(assembly, out var descriptions))
        {
            // 不直接使用 GetOrAdd 的值工厂，避免多个实体并发首次访问时重复读取同一个 XML 文件。
            var buildLock = _xmlBuildLocks.GetValue(assembly, _ => new object());
            lock (buildLock)
            {
                if (!_xmlDescriptionCache.TryGetValue(assembly, out descriptions))
                {
                    descriptions = LoadXmlDescriptions(assembly);
                    _xmlDescriptionCache[assembly] = descriptions;
                }
            }
        }
        descriptions.TryGetValue(memberName, out var description);
        return description;
    }

    private static IReadOnlyDictionary<string, string> LoadXmlDescriptions(Assembly assembly)
    {
        var descriptions = new Dictionary<string, string>(StringComparer.Ordinal);
        var filePath = Path.ChangeExtension(assembly.Location, "xml");
        if (!File.Exists(filePath))
        {
            return descriptions;
        }

        var xmlDoc = new XmlDocument();
        xmlDoc.Load(filePath);
        var memberNodes = xmlDoc.SelectNodes("//member[@name]");
        if (memberNodes == null)
        {
            return descriptions;
        }

        foreach (XmlNode memberNode in memberNodes)
        {
            var name = memberNode.Attributes?["name"]?.Value;
            if (!string.IsNullOrEmpty(name) && !descriptions.ContainsKey(name))
            {
                // 与原 SelectSingleNode 行为一致：重复名称保留文档中首次出现的节点。
                descriptions.Add(name, memberNode.InnerText?.Trim('\r', '\n', ' '));
            }
        }
        return descriptions;
    }
}

public class EntityInfo
{
    public NamingConvention NamingConvention { get; set; }

    public string TableName { get; set; }
    public string TableDescription { get; set; }

    public List<JoinDescriptor> JoinInfos { get; set; }

    public List<IndexDescriptor> IndexInfos { get; set; }

    /// <summary>
    /// 所有字段信息
    /// </summary>
    public List<EntityFieldInfo> FieldInfos { get; set; }
}

public class JoinDescriptor
{
    public Type JoinTableType { get; set; }
    public string LocalKey { get; set; }
    public string ForeignKey { get; set; }
    public string Alias { get; set; }

    public string GetTableName()
    {
        return JoinTableType.GetEntityInfo().TableName;
    }
}

public class IndexDescriptor
{
    public string[] IndexPropertyNames { get; set; }
    public string IndexName { get; set; }
    public DbIndexType IndexType { get; set; }
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
