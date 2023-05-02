using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository.Util;

internal static class SqlParameterUtil
{
    public static Dictionary<string, object> ConvertToDicParameter<TEntity>(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null)
    {
        var fields = fieldExpression?.GetFieldNames();
        return ConvertToDicParameter(entity, fields);
    }

    public static IEnumerable<DbParameter> ConvertToDbParameters(DatabaseType dbType, ISqlCommand sqlCommand, Func<DbParameter> dbParameterFactory)
    {
        if (sqlCommand?.Parameter == null)
        {
            return null;
        }

        var sql = sqlCommand.Sql;
        var parameter = sqlCommand.Parameter;

        if (parameter is IEnumerable<DbParameter> listDbParameters)
        {
            return listDbParameters;
        }

        var dicParameters = ConvertToDicParameter(parameter);
        if (dicParameters == null)
        {
            return new List<DbParameter>();
        }

        if (sqlCommand.UseQuestionMarkParameter)
        {
            return ConvertToDbParameters(dbType, dicParameters, dbParameterFactory);
        }

        if (dbType == DatabaseType.MsAccess)
        {
            if (sqlCommand.SqlParameterSorted && sqlCommand.UnusedSqlParameterRemoved)
            {
                return ConvertToDbParameters(dbType, dicParameters, dbParameterFactory);
            }

            var sortedSqlParameters = ParseSqlParameters(sql);
            var newDicParameters = new Dictionary<string, object>();
            foreach (var keyValuePair in sortedSqlParameters)
            {
                var paraName = keyValuePair.Key;
                if (!dicParameters.ContainsKey(paraName))
                {
                    throw new InvalidOperationException($"The sql parameter [{paraName}] does not exist.");
                }

                newDicParameters.Add(paraName, dicParameters[paraName]);
            }
            return ConvertToDbParameters(dbType, newDicParameters, dbParameterFactory);
        }

        if (!sqlCommand.UnusedSqlParameterRemoved)
        {
            RemoveUnusedParameters(dicParameters, sql);
        }

        return ConvertToDbParameters(dbType, dicParameters, dbParameterFactory);
    }

    public static List<DbParameter> ConvertToDbParameters(DatabaseType dbType, Dictionary<string, object> dicParameters, Func<DbParameter> dbParameterFactory)
    {
        var result = new List<DbParameter>();
        if (dicParameters == null)
        {
            return result;
        }

        foreach (var keyValuePair in dicParameters)
        {
            var sqlParameter = dbParameterFactory();
            sqlParameter.ParameterName = keyValuePair.Key;
            sqlParameter.SetParameterTypeAndValue(keyValuePair.Value, dbType);
            result.Add(sqlParameter);
        }
        return result;
    }

    public static IEnumerable<PropertyInfo> FilterParameters(IEnumerable<PropertyInfo> parameters, string sql)
    {
        var list = new List<PropertyInfo>();
        foreach (var p in parameters)
        {
            if (Regex.IsMatch(sql, $@"[?@:]{p.Name}([^\p{{L}}\p{{N}}_]+|$)", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant))
                list.Add(p);
        }
        return list;
    }
    public static IEnumerable<string> FilterParameters(IEnumerable<string> parameters, string sql)
    {
        var list = new List<string>();
        foreach (var p in parameters)
        {
            if (Regex.IsMatch(sql, $@"[?@:]{p}([^\p{{L}}\p{{N}}_]+|$)", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant))
                list.Add(p);
        }
        return list;
    }
    public static void RemoveUnusedParameters(Dictionary<string, object> parameters, string sql)
    {
        for (var i = 0; i < parameters.Count; i++)
        {
            var item = parameters.ElementAt(i);
            if (!Regex.IsMatch(sql, $@"[?@:]{item.Key}([^\p{{L}}\p{{N}}_]+|$)", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant))
                parameters.Remove(item.Key);
        }
    }

    public static Dictionary<string, int> ParseSqlParameters(string sql)
    {
        var dict = new Dictionary<string, int>(16);
        var index = 0;
        var regex = new Regex(@"[?@:](\w+)", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);
        foreach (Match match in regex.Matches(sql))
        {
            var name = match.Groups[1].Value;
            if (!dict.ContainsKey(name))
            {
                dict.Add(name, ++index);
            }
        }
        return dict;
    }

    public static string UseQuestionMarkParameter(string sql)
    {
        var regex = new Regex(@"[?@:](\w+)", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);
        return regex.Replace(sql, "?");
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