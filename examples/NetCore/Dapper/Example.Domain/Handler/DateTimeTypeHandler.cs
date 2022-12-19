using Dapper;
using Newtonsoft.Json;
using System;
using System.Data;

namespace Example.Domain.Handler
{
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
}
