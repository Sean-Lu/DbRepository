using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
        /// <param name="caseSensitive">表字段匹配属性名称时，是否大小写敏感</param>
        /// <returns></returns>
        public static T ToEntity<T>(this DataRow dr, bool caseSensitive = false)
        {
            if (dr == null)
            {
                return default;
            }

            T model = default;
            var type = typeof(T);
            if (type.IsValueType// 值类型，如：int、long、double、decimal、DateTime、bool、可空类型等
                || type == typeof(string)
            )
            {
                var value = dr[0];
                if (value != DBNull.Value)// 对象不能从 DBNull 转换为其他类型。
                {
                    model = ObjectConvert.ChangeType<T>(value);
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
                var properties = type.GetProperties();
                foreach (DataColumn column in dr.Table.Columns)
                {
                    var fieldName = column.ColumnName;
                    var propertyInfo = !caseSensitive
                        ? properties.FirstOrDefault(c => c.Name.ToLower() == fieldName.ToLower())
                        : type.GetProperty(fieldName);
                    var value = dr[fieldName];
                    if (propertyInfo != null && propertyInfo.CanWrite && value != DBNull.Value)
                    {
                        propertyInfo.SetValue(model, ObjectConvert.ChangeType(value, propertyInfo.PropertyType), null);
                    }
                }
            }
            else
            {
                throw new NotSupportedException($"Unsupported type: {type.FullName}");
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
