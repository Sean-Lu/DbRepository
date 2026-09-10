using System;
using Sean.Utility.Format;

namespace Sean.Core.DbRepository.Extensions;

/// <summary>
/// 查询结果的值转换规则，供 Reader 和 DataRow 共用，避免可空值与元组行为不一致。
/// </summary>
internal static class ResultValueMapping
{
    public static object ConvertValue(object value, Type targetType)
    {
        if (value != null && value != DBNull.Value)
        {
            var valueType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            // 已可直接赋值的对象无需转换，保留接口、基类及装箱值（含 Guid?）的合法输入。
            if (valueType.IsInstanceOfType(value))
            {
                return value;
            }
            // 部分驱动把文本 Guid 列返回为字符串；只补充合法文本解析，失败仍沿用原转换异常。
            // 不处理 byte[]，避免在未明确驱动字节序的情况下改变二进制 Guid 的含义。
            if (valueType == typeof(Guid) && value is string text && Guid.TryParse(text, out var guid))
            {
                return guid;
            }
            // 原转换器仅直接识别枚举，Nullable<枚举> 需要先解开包装再转换。
            if (valueType.IsEnum)
            {
                return ObjectConvert.ChangeType(value, valueType);
            }
        }

        // 其他情况保留原目标类型，特别是空字符串到 DateTime? 应仍返回 null。
        return ObjectConvert.ChangeType(value, targetType);
    }

    public static bool IsTuple(Type type)
    {
        if (!type.IsGenericType)
        {
            return false;
        }

        var definition = type.GetGenericTypeDefinition();
        var count = type.GetGenericArguments().Length;
        // 必须匹配系统元组的完整名称，不能把 TupleDto<T> 等普通实体当成元组。
        // 旧目标框架没有内置 ValueTuple，使用完整名称避免新增运行时依赖。
        return count >= 1 && count <= 8
            && (definition.FullName == "System.Tuple`" + count
                || definition.FullName == "System.ValueTuple`" + count);
    }

    public static object CreateTuple(Type type, int fieldCount, Func<int, object> getValue, bool useConstructorDefaults, int offset = 0)
    {
        var arguments = type.GetGenericArguments();
        var values = new object[arguments.Length];
        for (var index = 0; index < arguments.Length; index++)
        {
            var ordinal = offset + index;
            var value = ordinal < fieldCount ? getValue(ordinal) : null;
            if (index == 7 && IsTuple(arguments[index]) && !arguments[index].IsInstanceOfType(value))
            {
                // 第八个构造参数是 Rest，并非第八个元素；递归承接剩余列。
                // 列本身已经是所需 Rest 元组时直接保留，兼容原有的装箱元组输入。
                // 此列已为识别装箱 Rest 读取过，递归必须复用该值，兼容顺序读取的 Reader。
                values[index] = CreateTuple(arguments[index], fieldCount,
                    nextOrdinal => nextOrdinal == ordinal ? value : getValue(nextOrdinal), useConstructorDefaults, ordinal);
            }
            else
            {
                // 列不足或 DBNull 使用对应元素的默认值，多余列与原有行为一样忽略。
                // DataRow 原先让反射构造器处理 null；显式无参构造的 struct 与 Activator 默认值不同，保留该行为。
                values[index] = useConstructorDefaults && (value == null || value == DBNull.Value)
                    ? null : ConvertValue(value, arguments[index]);
            }
        }
        return Activator.CreateInstance(type, values);
    }
}
