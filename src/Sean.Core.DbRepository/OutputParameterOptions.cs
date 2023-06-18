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
        var parameterValue = getParamValue(OutputPropertyInfo.Name);
        var convertedValue = ObjectConvert.ChangeType(parameterValue, OutputPropertyInfo.PropertyType);
        OutputPropertyInfo.SetValue(OutputTarget, convertedValue);
    }
}

public class OutputParameterOptions<TEntity> : OutputParameterOptions
{
    public new TEntity OutputTarget { get; set; }
    public Expression<Func<TEntity, object>> FieldExpression { get; set; }
}