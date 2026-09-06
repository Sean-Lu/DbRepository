using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Sean.Core.DbRepository.Extensions;

/// <summary>
/// Extensions for <see cref="DataRow"/>
/// </summary>
public static class DataRowExtensions
{
    /// <summary>
    /// 将<see cref="DataRow"/>转换成实体
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="dr">数据行</param>
    /// <returns></returns>
    public static T ToEntity<T>(this DataRow dr)
    {
        if (dr == null)
        {
            return default;
        }

        return CreateMapper<T>(dr.Table)(dr);
    }

    internal static Func<DataRow, T> CreateMapper<T>(DataTable table)
    {
        var type = typeof(T);
        if (ResultValueMapping.IsTuple(type))
        {
            return dr => (T)ResultValueMapping.CreateTuple(type, table.Columns.Count, index => dr[index], useConstructorDefaults: true);
        }

        // 泛型类型还可能是 Nullable 或普通 DTO，不能因未匹配元组就静默返回 default。
        if (type.IsValueType || type == typeof(string))// 值类型、字符串
        {
            return dr =>
            {
                var value = dr[0];
                return value != DBNull.Value ? (T)ResultValueMapping.ConvertValue(value, type) : default;
            };
        }
        else if (type == typeof(object))// dynamic动态类型
        {
            return dr =>
            {
                var json = DbContextConfiguration.Options.JsonSerializer.Serialize(dr.ToDataTable());
                var list = DbContextConfiguration.Options.JsonSerializer.Deserialize<List<T>>(json);
                return list.FirstOrDefault();
            };
        }
        else if (type.IsClass && type.GetConstructor(Type.EmptyTypes) != null)// 实体类
        {
            // DataTable 的列结构固定，批量转换时复用映射，避免每行重复扫描反射信息。
            var columnNames = table.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToList();
            var properties = EntityPropertyMapping.Create(type, columnNames);
            return dr =>
            {
                var model = Activator.CreateInstance<T>();
                for (var index = 0; index < properties.Length; index++)
                {
                    var propertyInfo = properties[index];
                    if (propertyInfo != null)
                    {
                        var value = dr[index];
                        if (value != DBNull.Value)
                        {
                            propertyInfo.SetValue(model, ResultValueMapping.ConvertValue(value, propertyInfo.PropertyType), null);
                        }
                    }
                }
                return model;
            };
        }
        else
        {
            throw new NotSupportedException($"Unsupported type: {type.FullName}");
        }
    }

    /// <summary>
    /// DataRow转DataTable
    /// </summary>
    /// <param name="dr"></param>
    /// <returns></returns>
    public static DataTable ToDataTable(this DataRow dr)
    {
        var table = dr.Table.Clone();
        table.ImportRow(dr);
        return table;
    }
}
