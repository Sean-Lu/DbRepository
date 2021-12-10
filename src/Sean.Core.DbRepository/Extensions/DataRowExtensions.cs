using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Sean.Core.DbRepository.Factory;
using Sean.Utility.Format;

namespace Sean.Core.DbRepository.Extensions
{
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
        /// <param name="ignoreCase">是否忽略大小写</param>
        /// <returns></returns>
        public static T ToEntity<T>(this DataRow dr, bool ignoreCase = false)
        {
            if (dr == null)
            {
                return default;
            }

            var type = typeof(T);
            T model = default;
            if (type.IsValueType// 值类型，如：int、long、double、decimal、DateTime、bool、可空类型等
                || type == typeof(string)
            )
            {
                if (dr[0] != DBNull.Value)// 对象不能从 DBNull 转换为其他类型。
                {
                    var json = JsonHelper.Serialize(Convert.ChangeType(dr[0], Nullable.GetUnderlyingType(type) ?? type));
                    model = JsonHelper.Deserialize<T>(json);
                }
            }
            else if (type == typeof(object))// dynamic动态类型
            {
                var json = JsonHelper.Serialize(dr.ToDataTable());
                var list = JsonHelper.Deserialize<List<T>>(json);
                model = list.FirstOrDefault();
            }
            else if (type.IsClass && type.GetConstructor(Type.EmptyTypes) != null)
            {
                model = Activator.CreateInstance<T>();//new T();

                foreach (DataColumn column in dr.Table.Columns)
                {
                    var propertyInfo = ignoreCase
                        ? type.GetProperties().FirstOrDefault(c => c.Name.ToLower() == column.ColumnName.ToLower())
                        : type.GetProperty(column.ColumnName);
                    if (propertyInfo != null && dr[column.ColumnName] != DBNull.Value && propertyInfo.CanWrite)
                    {
                        //propertyInfo.SetValue(model, Convert.ChangeType(dr[column.ColumnName], propertyInfo.PropertyType), null);// 可空类型会报错
                        propertyInfo.SetValue(model, Convert.ChangeType(dr[column.ColumnName], Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType), null);
                    }
                }
            }
            else
            {
                throw new NotSupportedException($"The reflection of this type [{type.FullName}] is not currently supported.");
            }

            return model;
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
}
