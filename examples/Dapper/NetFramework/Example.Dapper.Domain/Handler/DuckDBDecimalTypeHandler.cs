using System;
using System.Data;
using Dapper;

namespace Example.Dapper.Domain.Handler;

public class DuckDBDecimalTypeHandler : SqlMapper.TypeHandler<decimal>
{
    public override void SetValue(IDbDataParameter parameter, decimal value)
    {
        parameter.Value = Convert.ToDouble(value);
        parameter.DbType = DbType.Double;
    }

    public override decimal Parse(object value)
    {
        return Convert.ToDecimal(value);
    }
}