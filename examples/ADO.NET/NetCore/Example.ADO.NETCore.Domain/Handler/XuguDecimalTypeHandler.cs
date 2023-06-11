using System;
using System.Data;
using System.Data.Common;
using Sean.Core.DbRepository;

namespace Example.ADO.NETCore.Domain.Handler;

public class XuguDecimalTypeHandler : ITypeHandler
{
    public void Set(DbParameter dbParameter, object value, DatabaseType databaseType)
    {
        dbParameter.Value = Convert.ToDouble(value);
        dbParameter.DbType = DbType.Double;
    }
}