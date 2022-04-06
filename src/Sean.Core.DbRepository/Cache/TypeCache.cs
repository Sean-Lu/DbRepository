using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
#if !NET40
using System.ComponentModel.DataAnnotations.Schema;
#endif
using System.Linq;
using System.Reflection;
using Sean.Core.DbRepository.Extensions;
using Sean.Utility.Extensions;

namespace Sean.Core.DbRepository
{
    internal class TypeCache
    {
        private static readonly ConcurrentDictionary<Type, EntityInfo> _entityInfoDic = new();

        public static EntityInfo GetEntityInfo(Type entityClassType)
        {
            if (entityClassType == null)
            {
                return null;
            }

            if (_entityInfoDic.TryGetValue(entityClassType, out var entityInfo) && entityInfo != null)// 先从缓存中读取
            {
                return entityInfo;
            }

            entityInfo = new EntityInfo
            {
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

            entityInfo.MainTableName = entityClassType.GetCustomAttributesExt<TableAttribute>(true).FirstOrDefault()?.Name ?? entityClassType.Name;
            entityInfo.Sequence = entityClassType.GetCustomAttributesExt<SequenceAttribute>(true).FirstOrDefault()?.Name;

            var propertyInfos = entityClassType.GetProperties();
            foreach (var propertyInfo in propertyInfos)
            {
                if (propertyInfo.GetCustomAttributesExt<NotMappedAttribute>(false).Any()
                    || propertyInfo.GetCustomAttributesExt<IgnoreAttribute>(false).Any())
                {
                    continue;
                }

                var fieldInfo = new TableFieldInfo
                {
                    Property = propertyInfo,
                    FieldName = propertyInfo.GetFieldName(),
                    PrimaryKey = propertyInfo.GetCustomAttributesExt<KeyAttribute>(false).Any(),
                    Identity = propertyInfo.GetCustomAttributesExt<DatabaseGeneratedAttribute>(false).Any(c => c.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity)
                };
                entityInfo.FieldInfos.Add(fieldInfo);
            }

            _entityInfoDic.AddOrUpdate(entityClassType, entityInfo, (_, _) => entityInfo);// 更新缓存
            return entityInfo;
        }
    }

    public class EntityInfo
    {
        /// <summary>
        /// 主表表名
        /// </summary>
        public string MainTableName { get; set; }

        /// <summary>
        /// 序列
        /// </summary>
        public string Sequence { get; set; }

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
        /// <summary>
        /// 是否是主键字段
        /// </summary>
        public bool PrimaryKey { get; set; }
        /// <summary>
        /// 是否是自增字段
        /// </summary>
        public bool Identity { get; set; }
    }
}
