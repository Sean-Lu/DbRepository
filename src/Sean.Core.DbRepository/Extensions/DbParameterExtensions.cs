using System;
using System.Data.Common;
using System.Data;
using Sean.Utility.Format;

namespace Sean.Core.DbRepository.Extensions;

internal static class DbParameterExtensions
{
    public static void SetParameterTypeAndValue(this DbParameter sqlParameter, object parameterValue, DatabaseType dbType)
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
                bool => dbType == DatabaseType.Oracle ? DbType.Byte : DbType.Boolean,
                DateTime => dbType == DatabaseType.MsAccess ? DbType.String : DbType.DateTime,
                Guid => DbType.Guid,
                _ => sqlParameter.DbType
            };
        }

        if (parameterValue is Enum)
            sqlParameter.Value = ObjectConvert.ChangeType<int>(parameterValue);
        else if (parameterValue is bool && dbType == DatabaseType.Oracle)
            sqlParameter.Value = ObjectConvert.ChangeType<byte>(parameterValue);// Oracle: bool -> byte
        else
            sqlParameter.Value = parameterValue ?? DBNull.Value;

        if (parameterValue != null)
        {
            var parameterType = parameterValue.GetType();
            if (DbContextConfiguration.Options.ContainsTypeHandler(parameterType))
            {
                var typeHandler = DbContextConfiguration.Options.GetTypeHandler(parameterType);
                typeHandler?.Set(sqlParameter, parameterValue, dbType);
            }
        }
    }
}