using Sean.Utility.Extensions;
using System;
using System.Linq;

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
        return EntityInfoCache.Get(entityClassType);
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