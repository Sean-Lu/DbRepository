using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository
{
    public class SqlParameterUtil
    {
        public static Dictionary<string, object> ConvertToDicParameter<TEntity>(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null)
        {
            var fields = fieldExpression?.GetMemberNames();
            return ConvertToDicParameter(entity, fields);
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

                    paramDic.Add(fieldInfo.FieldName, fieldInfo.Property.GetValue(instance, null));
                }
            }
            else
            {
                // 所有字段
                foreach (var fieldInfo in instance.GetType().GetEntityInfo().FieldInfos)
                {
                    paramDic.Add(fieldInfo.FieldName, fieldInfo.Property.GetValue(instance, null));
                }
            }
            return paramDic;
        }
    }
}
