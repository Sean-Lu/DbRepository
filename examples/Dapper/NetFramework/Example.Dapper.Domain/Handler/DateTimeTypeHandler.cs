using System;
using System.Data;
using Dapper;

namespace Example.Dapper.Domain.Handler;

public class DateTimeTypeHandler : SqlMapper.TypeHandler<DateTime>
{
    public override void SetValue(IDbDataParameter parameter, DateTime value)
    {
        parameter.Value = value;
        parameter.DbType = DbType.DateTime;
    }

    public override DateTime Parse(object value)
    {
        DateTime result;
        if (value is DateTime time)
        {
            result = time;
        }
        else if (value is long tick)
        {
            result = new DateTime(tick);
        }
        else
        {
            result = Convert.ToDateTime(value);
        }

        return result.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(result, DateTimeKind.Local) : result;
    }
}

public class DateTimeNullableTypeHandler : SqlMapper.TypeHandler<DateTime?>
{
    public override void SetValue(IDbDataParameter parameter, DateTime? value)
    {
        parameter.Value = value;
        parameter.DbType = DbType.DateTime;
    }

    public override DateTime? Parse(object value)
    {
        if (value == null)
        {
            return default;
        }

        DateTime result;
        if (value is DateTime time)
        {
            result = time;
        }
        else if (value is long tick)
        {
            result = new DateTime(tick);
        }
        else
        {
            result = Convert.ToDateTime(value);
        }

        return result.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(result, DateTimeKind.Local) : result;
    }
}

public class AccessDateTimeHandler : SqlMapper.TypeHandler<DateTime>
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


public class AccessDateTimeNullableHandler : SqlMapper.TypeHandler<DateTime?>
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