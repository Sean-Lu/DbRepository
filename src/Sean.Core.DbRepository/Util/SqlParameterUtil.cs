using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository.Util
{
    public static class SqlParameterUtil
    {
        public static Dictionary<string, object> ConvertToDicParameter<TEntity>(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null)
        {
            var fields = fieldExpression?.GetFieldNames();
            return ConvertToDicParameter(entity, fields);
        }

        public static IEnumerable<DbParameter> ConvertToDbParameters(object obj, Func<DbParameter> dbParameterFactory)
        {
            if (obj == null)
            {
                return null;
            }

            if (obj is IEnumerable<DbParameter> listDbParameters)
            {
                return listDbParameters;
            }

            var result = new List<DbParameter>();
            var dicParameters = ConvertToDicParameter(obj);
            if (dicParameters != null)
            {
                foreach (var keyValuePair in dicParameters)
                {
                    var sqlParameter = dbParameterFactory();
                    sqlParameter.ParameterName = keyValuePair.Key;
                    sqlParameter.SetParameterTypeAndValue(keyValuePair.Value);
                    result.Add(sqlParameter);
                }
            }
            return result;
        }

        internal static Dictionary<string, object> ConvertToDicParameter(object instance, IEnumerable<string> fields = null)
        {
            if (instance == null)
            {
                return new Dictionary<string, object>();
            }

            if (instance is Dictionary<string, object> oldParameter)
            {
                return oldParameter;
            }

            var paramDic = new Dictionary<string, object>();
            if (fields != null && fields.Any())
            {
                // 指定字段
                var type = instance.GetType();
                var tableFieldInfos = type.GetEntityInfo().FieldInfos;
                foreach (var field in fields)
                {
                    var fieldInfo = tableFieldInfos.Find(c => c.FieldName == field);
                    if (fieldInfo == null)
                    {
                        throw new InvalidOperationException($"Table field [{field}] not found in [{type.FullName}].");
                    }

                    paramDic.Add(fieldInfo.Property.Name, fieldInfo.Property.GetValue(instance, null));
                }
            }
            else
            {
                // 所有字段
                foreach (var fieldInfo in instance.GetType().GetEntityInfo().FieldInfos)
                {
                    paramDic.Add(fieldInfo.Property.Name, fieldInfo.Property.GetValue(instance, null));
                }
            }
            return paramDic;
        }
    }
}
