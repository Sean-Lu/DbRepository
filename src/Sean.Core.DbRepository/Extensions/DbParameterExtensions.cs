using System;
using System.Data.Common;
using System.Data;
using Sean.Utility.Format;

namespace Sean.Core.DbRepository.Extensions
{
    public static class DbParameterExtensions
    {
        public static void SetParameterTypeAndValue(this DbParameter sqlParameter, object parameterValue)
        {
            if (parameterValue != null && parameterValue != DBNull.Value)
            {
                sqlParameter.DbType = parameterValue switch
                {
                    byte => DbType.Byte,
                    sbyte => DbType.SByte,
                    short => DbType.Int16,
                    ushort => DbType.UInt16,
                    int or Enum => DbType.Int32,
                    uint => DbType.UInt32,
                    long => DbType.Int64,
                    ulong => DbType.UInt64,
                    float => DbType.Single,
                    double => DbType.Double,
                    decimal => DbType.Decimal,
                    string => DbType.String,
                    bool => DbType.Boolean,
                    DateTime => DbType.DateTime,
                    Guid => DbType.Guid,
                    _ => sqlParameter.DbType
                };
            }

            sqlParameter.Value = parameterValue is Enum
                ? ObjectConvert.ChangeType<int>(parameterValue)
                : parameterValue ?? DBNull.Value;
        }
    }
}