using System;
using Dapper;
using System.Data;

namespace Example.Dapper.Core.Domain.Handler;

public class BoolTypeHandler : SqlMapper.TypeHandler<bool>
{
    public override void SetValue(IDbDataParameter parameter, bool value)
    {
        parameter.Value = value ? (byte)1 : (byte)(0);
        parameter.DbType = DbType.Byte;
    }

    public override bool Parse(object value)
    {
        return Convert.ToBoolean(value);
    }
}