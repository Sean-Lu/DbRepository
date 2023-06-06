using System;
using System.Data;
using Dapper;

namespace Example.Dapper.Domain.Handler;

public class MsAccessDateTimeHandler : SqlMapper.TypeHandler<DateTime>
{
    public override void SetValue(IDbDataParameter parameter, DateTime value)
    {
        parameter.Value = value.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public override DateTime Parse(object value)
    {
        return value != null ? Convert.ToDateTime(value) : default;
    }
}

public class MsAccessDateTimeNullableHandler : SqlMapper.TypeHandler<DateTime?>
{
    public override void SetValue(IDbDataParameter parameter, DateTime? value)
    {
        parameter.Value = value?.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public override DateTime? Parse(object value)
    {
        return value != null ? Convert.ToDateTime(value) : default;
    }
}