using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
#if !NET40
using System.ComponentModel.DataAnnotations.Schema;
#endif
using System.Linq;
using System.Reflection;
using System.Text;
using Sean.Core.DbRepository.Attributes;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository.Cache
{
    internal class TypeCache
    {
        private static readonly ConcurrentDictionary<Type, EntityInfo> _entityInfoDic = new();

        public static EntityInfo GetEntityInfo(Type entityClassType)
        {
            if (_entityInfoDic.TryGetValue(entityClassType, out var entityInfo) && entityInfo != null)
            {
                return entityInfo;
            }

            entityInfo = new EntityInfo
            {
                MainTableName = entityClassType.GetCustomAttributesExt<TableAttribute>(true).FirstOrDefault()?.Name ?? entityClassType.Name
            };

            var propertyInfos = entityClassType.GetProperties();
            foreach (var propertyInfo in propertyInfos)
            {
                if (propertyInfo.GetCustomAttributesExt<IgnoreAttribute>(false).Any())
                {
                    continue;
                }

                var fieldInfo = new FieldInfo
                {
                    Property = propertyInfo,
                    FieldName = propertyInfo.GetFieldName(),
                    PrimaryKey = propertyInfo.GetCustomAttributesExt<KeyAttribute>(false).Any(),
                    Identity = propertyInfo.GetCustomAttributesExt<DatabaseGeneratedAttribute>(false).Any(c => c.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity)
                };
                entityInfo.FieldInfos.Add(fieldInfo);
            }

            _entityInfoDic.AddOrUpdate(entityClassType, entityInfo, (_, _) => entityInfo);
            return entityInfo;
        }
    }

    internal class EntityInfo
    {
        /// <summary>
        /// 主表表名
        /// </summary>
        public string MainTableName { get; set; }

        /// <summary>
        /// 所有字段信息
        /// </summary>
        public List<FieldInfo> FieldInfos { get; set; } = new();
    }

    internal class FieldInfo
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
