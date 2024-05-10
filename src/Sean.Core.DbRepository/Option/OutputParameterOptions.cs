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
    public new TEntity OutputTarget { get; set; }
    public Expression<Func<TEntity, object>> FieldExpression { get; set; }
}