using Sean.Utility.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sean.Core.DbRepository.Extensions;

public static class TypeExtensions
{
    /// <summary>
    /// 获取数据库表的实体类信息
    /// </summary>
    /// <param name="entityClassType"></param>
    /// <returns></returns>
    public static EntityInfo GetEntityInfo(this Type entityClassType)
    {
        return EntityTypeCache.GetEntityInfo(entityClassType);
    }

    /// <summary>
    /// 获取数据库表的主表名称
    /// </summary>
    /// <param name="entityClassType"></param>
    /// <returns></returns>
    public static string GetMainTableName(this Type entityClassType)
    {
        return EntityTypeCache.GetEntityInfo(entityClassType).MainTableName;
    }

    /// <summary>
    /// 获取数据库表的所有字段
    /// </summary>
    /// <param name="entityClassType"></param>
    /// <returns></returns>
    public static List<string> GetAllFieldNames(this Type entityClassType)
    {
        return EntityTypeCache.GetEntityInfo(entityClassType).FieldInfos.Select(c => c.FieldName).ToList();
    }

    /// <summary>
    /// 获取数据库表的主键字段
    /// </summary>
    /// <param name="entityClassType"></param>
    /// <returns></returns>
    public static List<string> GetPrimaryKeys(this Type entityClassType)
    {
        return EntityTypeCache.GetEntityInfo(entityClassType).FieldInfos.Where(c => c.PrimaryKey).Select(c => c.FieldName).ToList();
    }

    /// <summary>
    /// 获取数据库表的自增字段
    /// </summary>
    /// <param name="entityClassType"></param>
    /// <returns></returns>
    public static List<string> GetIdentityFieldNames(this Type entityClassType)
    {
        return EntityTypeCache.GetEntityInfo(entityClassType).FieldInfos.Where(c => c.Identity).Select(c => c.FieldName).ToList();
    }

    /// <summary>
    /// 获取数据库表的自增主键字段
    /// </summary>
    /// <param name="entityClassType"></param>
    /// <returns></returns>
    public static PropertyInfo GetKeyIdentityProperty(this Type entityClassType)
    {
        return EntityTypeCache.GetEntityInfo(entityClassType).FieldInfos.FirstOrDefault(c => c.PrimaryKey && c.Identity)?.Property;
    }

    public static NamingConvention GetNamingConvention(this Type entityClassType)
    {
        return EntityTypeCache.GetEntityInfo(entityClassType).NamingConvention;
    }

    internal static object CreateInstanceByConstructor(this Type type)
    {
        var emptyParamConstructor = type.GetConstructor(Type.EmptyTypes);
        if (emptyParamConstructor != null)
        {
            return Activator.CreateInstance(type);
        }

        var constructors = type.GetConstructors();
        var minParamConstructor = constructors.Length > 1
            ? constructors.OrderBy(c => c.GetParameters().Length).FirstOrDefault()
            : constructors.FirstOrDefault();
        if (minParamConstructor == null)
        {
            return default;
        }

        object[] parameterArgs = null;
        var parameters = minParamConstructor.GetParameters();
        if (parameters.Length > 0)
        {
            parameterArgs = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var parameterInfo = parameters[i];
                parameterArgs[i] = parameterInfo.HasDefaultValue
                    ? parameterInfo.DefaultValue
                    : parameterInfo.ParameterType.GetDefaultValue();
            }
        }
        return Activator.CreateInstance(type, parameterArgs);
    }
}