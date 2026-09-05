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
        OutputPropertyInfo.SetValue(OutputTarget, ObjectConvert.ChangeType(getParamValue(OutputPropertyInfo.Name), OutputPropertyInfo.PropertyType));
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
