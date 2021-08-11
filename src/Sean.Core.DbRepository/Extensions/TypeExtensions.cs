#if !NET40
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Sean.Core.DbRepository.Attributes;

namespace Sean.Core.DbRepository.Extensions
{
    public static class TypeExtensions
    {
        /// <summary>
        /// 获取主表名称
        /// </summary>
        /// <param name="entityClassType"></param>
        /// <returns></returns>
        public static string GetMainTableName(this Type entityClassType)
        {
            return entityClassType.GetCustomAttributes<TableAttribute>(true).FirstOrDefault()?.Name ?? entityClassType.Name;
        }

        /// <summary>
        /// 过滤拥有<see cref="IgnoreAttribute"/>特性的所有字段
        /// </summary>
        /// <param name="entityClassType"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static List<PropertyInfo> GetValidPropertiesForSql(this Type entityClassType, Predicate<PropertyInfo> filter = null)
        {
            var list = new List<PropertyInfo>();
            var propertyInfos = entityClassType.GetProperties();
            foreach (var propertyInfo in propertyInfos)
            {
                if (propertyInfo.GetCustomAttributes<IgnoreAttribute>(false).Any()
                    || filter != null && filter(propertyInfo))
                {
                    continue;
                }
                list.Add(propertyInfo);
            }
            return list;
        }

        /// <summary>
        /// 获取主键字段
        /// </summary>
        /// <param name="entityClassType"></param>
        /// <returns></returns>
        public static List<PropertyInfo> GetPrimaryKeyProperties(this Type entityClassType)
        {
            return entityClassType.GetProperties().Where(propertyInfo => propertyInfo.GetCustomAttributes<KeyAttribute>(false).Any()).ToList();
        }

        /// <summary>
        /// 获取自增字段
        /// </summary>
        /// <param name="entityClassType"></param>
        /// <param name="includeProperties"></param>
        /// <param name="ignoreProperties"></param>
        /// <returns></returns>
        public static List<PropertyInfo> GetIdentityProperties(this Type entityClassType)
        {
            return entityClassType.GetProperties().Where(propertyInfo => propertyInfo.GetCustomAttributes<DatabaseGeneratedAttribute>(false).Any(c => c.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity)).ToList();
        }

        /// <summary>
        /// 获取自增主键字段
        /// </summary>
        /// <param name="entityClassType"></param>
        /// <returns></returns>
        public static PropertyInfo GetKeyIdentityProperty(this Type entityClassType)
        {
            return entityClassType.GetProperties().FirstOrDefault(c => c.GetCustomAttributes<KeyAttribute>(false).Any() && c.GetCustomAttributes<DatabaseGeneratedAttribute>(false).Any(o => o.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity));
        }
    }
}
#endif
