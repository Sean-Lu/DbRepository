using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sean.Core.DbRepository.Attributes;
using Sean.Core.DbRepository.Cache;

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
            return TypeCache.GetEntityInfo(entityClassType).MainTableName;
        }

        /// <summary>
        /// 获取所有数据库表字段（过滤：<see cref="IgnoreAttribute"/>）
        /// </summary>
        /// <param name="entityClassType"></param>
        /// <returns></returns>
        public static List<string> GetAllFieldNames(this Type entityClassType)
        {
            return TypeCache.GetEntityInfo(entityClassType).FieldInfos.Select(c => c.FieldName).ToList();
        }

        /// <summary>
        /// 获取主键字段
        /// </summary>
        /// <param name="entityClassType"></param>
        /// <returns></returns>
        public static List<string> GetPrimaryKeys(this Type entityClassType)
        {
            return TypeCache.GetEntityInfo(entityClassType).FieldInfos.Where(c => c.PrimaryKey).Select(c => c.FieldName).ToList();
        }

        /// <summary>
        /// 获取自增字段
        /// </summary>
        /// <param name="entityClassType"></param>
        /// <returns></returns>
        public static List<string> GetIdentityFieldNames(this Type entityClassType)
        {
            return TypeCache.GetEntityInfo(entityClassType).FieldInfos.Where(c => c.Identity).Select(c => c.FieldName).ToList();
        }

        /// <summary>
        /// 获取自增主键字段
        /// </summary>
        /// <param name="entityClassType"></param>
        /// <returns></returns>
        public static PropertyInfo GetKeyIdentityProperty(this Type entityClassType)
        {
            return TypeCache.GetEntityInfo(entityClassType).FieldInfos.FirstOrDefault(c => c.PrimaryKey && c.Identity)?.Property;
        }
    }
}
