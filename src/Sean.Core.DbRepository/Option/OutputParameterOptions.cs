using Sean.Utility.Format;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Sean.Core.DbRepository;

public class OutputParameterOptions
{
    public object OutputTarget { get; set; }
    public PropertyInfo OutputPropertyInfo { get; set; }

    public void ExecuteOutput(Func<string, object> getParamValue)
    {
        OutputPropertyInfo.SetValue(OutputTarget, ConvertOutputValue(getParamValue(OutputPropertyInfo.Name), OutputPropertyInfo.PropertyType));
    }

    private static object ConvertOutputValue(object value, Type targetType)
    {
        // 文本输出可回写 Guid/Guid?；空值及解析失败仍沿用原转换，不能提前修改目标。
        return value is string text && (targetType == typeof(Guid) || targetType == typeof(Guid?))
            && Guid.TryParse(text, out var guid) ? guid : ObjectConvert.ChangeType(value, targetType);
    }
}

public class OutputParameterOptions<TEntity> : OutputParameterOptions
{
    /// <summary>
    /// 与输出回写共享同一目标；非泛型属性被设置为不兼容类型时，泛型读取明确报错。
    /// </summary>
    public new TEntity OutputTarget
    {
        get => base.OutputTarget == null ? default : (TEntity)base.OutputTarget;
        set => base.OutputTarget = value;
    }

    public Expression<Func<TEntity, object>> FieldExpression { get; set; }
}
