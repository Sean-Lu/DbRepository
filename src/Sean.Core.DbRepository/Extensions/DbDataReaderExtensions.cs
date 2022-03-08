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
        /// <param name="caseSensitive">表字段匹配属性名称时，是否大小写敏感</param>
        /// <returns></returns>
        public static T Get<T>(this IDataReader dataReader, bool caseSensitive = false)
        {
            var list = dataReader.GetList<T>(caseSensitive, 1);
            return list.FirstOrDefault();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataReader"></param>
        /// <param name="caseSensitive">表字段匹配属性名称时，是否大小写敏感</param>
        /// <param name="readCount">读取的记录数。如果值为null，表示读取所有记录数。</param>
        /// <returns></returns>
        public static List<T> GetList<T>(this IDataReader dataReader, bool caseSensitive = false, int? readCount = null)
        {
            if (dataReader == null)
            {
                return null;
            }

            //return dataReader.GetDataTable().ToList<T>(caseSensitive);

            var list = new List<T>();
            var count = 0;
            while (dataReader.Read())
            {
                if (readCount.HasValue && readCount.Value > 0 && count >= readCount.Value)
                {
                    break;
                }

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
                    var properties = type.GetProperties();
                    for (var i = 0; i < dataReader.FieldCount; i++)
                    {
                        var fieldName = dataReader.GetName(i);
                        var propertyInfo = !caseSensitive
                            ? properties.FirstOrDefault(c => c.Name.ToLower() == fieldName.ToLower())
                            : type.GetProperty(fieldName);
                        var value = dataReader[i];
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
                list.Add(model);

                count++;
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
            var dataSet = dataReader.GetDataSet();
            return dataSet != null && dataSet.Tables.Count > 0 ? dataSet.Tables[0] : null;
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
