using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Sean.Utility.Format;

namespace Sean.Core.DbRepository.Extensions
{
    /// <summary>
    /// Extensions for <see cref="DbDataReader"/>
    /// </summary>
    public static class DbDataReaderExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataReader"></param>
        /// <param name="ignoreCase">是否忽略大小写</param>
        /// <returns></returns>
        public static List<T> GetList<T>(this IDataReader dataReader, bool ignoreCase = false)
        {
            if (dataReader == null)
            {
                return null;
            }

            //return dataReader.GetDataTable().ToList<T>(ignoreCase);

            var list = new List<T>();
            while (dataReader.Read())
            {
                T model = default;
                var type = typeof(T);
                if (type.IsValueType// 值类型，如：int、long、double、decimal、DateTime、bool、可空类型等
                    || type == typeof(string)
                )
                {
                    var value = dataReader[0];
                    if (value != DBNull.Value)// 对象不能从 DBNull 转换为其他类型。
                    {
                        model = ObjectConvert.ChangeType<T>(value);
                    }
                }
                else if (type == typeof(object))// dynamic动态类型
                {
                    var dic = new Dictionary<string, object>();
                    for (var i = 0; i < dataReader.FieldCount; i++)
                    {
                        dic.Add(dataReader.GetName(i), dataReader[i]);
                    }
                    var json = JsonHelper.Serialize(dic);
                    model = JsonHelper.Deserialize<T>(json);
                }
                else if (type.IsClass && type.GetConstructor(Type.EmptyTypes) != null)
                {
                    model = Activator.CreateInstance<T>();//new T();
                    for (var i = 0; i < dataReader.FieldCount; i++)
                    {
                        var propertyInfo = ignoreCase
                            ? type.GetProperties().FirstOrDefault(c => c.Name.ToLower() == dataReader.GetName(i).ToLower())
                            : type.GetProperty(dataReader.GetName(i));
                        var value = dataReader[i];
                        if (propertyInfo != null && propertyInfo.CanWrite && value != DBNull.Value)
                        {
                            propertyInfo.SetValue(model, ObjectConvert.ChangeType(value, propertyInfo.PropertyType), null);
                        }
                    }
                }
                else
                {
                    throw new NotSupportedException($"The reflection of this type [{type.FullName}] is not currently supported.");
                }
                list.Add(model);
            }
            return list;
        }

        /// <summary>
        /// <see cref="DataTable"/>
        /// </summary>
        /// <param name="dataReader"></param>
        /// <returns></returns>
        public static DataTable GetDataTable(this IDataReader dataReader)
        {
            var table = new DataTable();
            table.Load(dataReader);
            return table;
        }
        /// <summary>
        /// <see cref="DataSet"/>
        /// </summary>
        /// <param name="dataReader"></param>
        /// <returns></returns>
        public static DataSet GetDataSet(this IDataReader dataReader)
        {
            if (dataReader == null)
            {
                return null;
            }

            var result = new DataSet();

            do
            {
                var table = new DataTable();
                for (var i = 0; i < dataReader.FieldCount; i++)
                {
                    var column = new DataColumn
                    {
                        DataType = dataReader.GetFieldType(i),
                        ColumnName = dataReader.GetName(i)
                    };
                    table.Columns.Add(column);
                }

                while (dataReader.Read())
                {
                    var row = table.NewRow();
                    for (var i = 0; i < dataReader.FieldCount; i++)
                    {
                        row[i] = dataReader[i];
                    }
                    table.Rows.Add(row);
                }
                result.Tables.Add(table);
            } while (dataReader.NextResult());

            return result;
        }
    }
}
