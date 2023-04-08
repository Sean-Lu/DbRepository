using System.Data;
using Dapper;
using Newtonsoft.Json;

namespace Example.Dapper.Core.Domain.Handler;

public class JsonTypeHandler<T> : SqlMapper.TypeHandler<T>
    where T : class
{
    public override T Parse(object value)
    {
        return JsonConvert.DeserializeObject<T>((string)value);
    }

    public override void SetValue(IDbDataParameter parameter, T value)
    {
        parameter.Value = JsonConvert.SerializeObject(value);
    }
}