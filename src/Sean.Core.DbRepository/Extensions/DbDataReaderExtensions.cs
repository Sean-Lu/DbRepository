using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Sean.Utility.Format;

namespace Sean.Core.DbRepository.Extensions;

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
    /// <returns></returns>
    public static T Get<T>(this IDataReader dataReader)
    {
        var list = dataReader.GetList<T>(1);
        if (list == null)
        {
            return default;
        }
        return list.FirstOrDefault();
    }
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="dataReader"></param>
    /// <param name="readCount">读取的记录数。如果值为null，表示读取所有记录数。</param>
    /// <returns></returns>
    public static List<T> GetList<T>(this IDataReader dataReader, int? readCount = null)
    {
        if (dataReader == null)
        {
            return null;
        }

        //return dataReader.GetDataTable().ToList<T>();

        var list = new List<T>();
        var count = 0;
        while (dataReader.Read())
        {
            if (readCount.HasValue && readCount.Value > 0 && count >= readCount.Value)
            {
                break;
            }

            T model = GetModelInternal<T>(dataReader);
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
        var table = new DataTable();

        //table.Load(dataReader);// Exception
        //return table;

        for (var i = 0; i < dataReader.FieldCount; i++)
        {
            var dataType = dataReader.GetFieldType(i);
            var columnName = dataReader.GetName(i);
            if (table.Columns.Contains(columnName))
            {
                var index = 1;
                do
                {
                    columnName = $"{dataReader.GetName(i)}{index}";
                    if (!table.Columns.Contains(columnName))
                    {
                        break;
                    }
                    index++;
                } while (true);
            }
            var column = new DataColumn
            {
                DataType = dataType,
                ColumnName = columnName
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
            var table = GetDataTable(dataReader);
            if (table != null)
            {
                result.Tables.Add(table);
            }
        } while (dataReader.NextResult());

        return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="dataReader"></param>
    /// <returns></returns>
    public static async Task<T> GetAsync<T>(this DbDataReader dataReader)
    {
        var list = await dataReader.GetListAsync<T>(1);
        if (list == null)
        {
            return default;
        }
        return list.FirstOrDefault();
    }
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="dataReader"></param>
    /// <param name="readCount">读取的记录数。如果值为null，表示读取所有记录数。</param>
    /// <returns></returns>
    public static async Task<List<T>> GetListAsync<T>(this DbDataReader dataReader, int? readCount = null)
    {
        if (dataReader == null)
        {
            return null;
        }

        //return dataReader.GetDataTable().ToList<T>();

        var list = new List<T>();
        var count = 0;
        while (await dataReader.ReadAsync())
        {
            if (readCount.HasValue && readCount.Value > 0 && count >= readCount.Value)
            {
                break;
            }

            T model = GetModelInternal<T>(dataReader);
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
    public static async Task<DataTable> GetDataTableAsync(this DbDataReader dataReader)
    {
        var table = new DataTable();

        //table.Load(dataReader);// Exception
        //return table;

        for (var i = 0; i < dataReader.FieldCount; i++)
        {
            var dataType = dataReader.GetFieldType(i);
            var columnName = dataReader.GetName(i);
            if (table.Columns.Contains(columnName))
            {
                var index = 1;
                do
                {
                    columnName = $"{dataReader.GetName(i)}{index}";
                    if (!table.Columns.Contains(columnName))
                    {
                        break;
                    }
                    index++;
                } while (true);
            }
            var column = new DataColumn
            {
                DataType = dataType,
                ColumnName = columnName
            };
            table.Columns.Add(column);
        }

        while (await dataReader.ReadAsync())
        {
            var row = table.NewRow();
            for (var i = 0; i < dataReader.FieldCount; i++)
            {
                row[i] = dataReader[i];
            }
            table.Rows.Add(row);
        }

        return table;
    }
    /// <summary>
    /// <see cref="DataSet"/>
    /// </summary>
    /// <param name="dataReader"></param>
    /// <returns></returns>
    public static async Task<DataSet> GetDataSetAsync(this DbDataReader dataReader)
    {
        if (dataReader == null)
        {
            return null;
        }

        var result = new DataSet();

        do
        {
            var table = await GetDataTableAsync(dataReader);
            if (table != null)
            {
                result.Tables.Add(table);
            }
        } while (await dataReader.NextResultAsync());

        return result;
    }

    private static T GetModelInternal<T>(IDataReader dataReader)
    {
        T model = default;
        var type = typeof(T);
        if (type.IsGenericType)
        {
            var genericType = type.GetGenericTypeDefinition();
            if (genericType.Name.StartsWith("Tuple"))// Tuple<>
            {
                var itemCount = type.GetProperties().Count(c => c.Name.StartsWith("Item"));
                if (itemCount > 0)
                {
                    var values = new object[itemCount];// dataReader.FieldCount
                    dataReader.GetValues(values);
                    if (values.Any(c => c == DBNull.Value))
                    {
                        for (var i = 0; i < values.Length; i++)
                        {
                            if (values[i] == DBNull.Value)
                            {
                                //var propertyInfo = type.GetProperty($"Item{i + 1}");
                                //values[i] = propertyInfo != null ? propertyInfo.PropertyType.GetDefaultValue() : null;
                                values[i] = null;
                            }
                        }
                    }
                    model = (T)Activator.CreateInstance(typeof(T), values);
                }
            }
            else if (genericType.Name.StartsWith("ValueTuple"))// 匿名类：ValueTuple<>
            {
                var itemCount = type.GetFields().Count(c => c.Name.StartsWith("Item"));
                if (itemCount > 0)
                {
                    var values = new object[itemCount];// dataReader.FieldCount
                    dataReader.GetValues(values);
                    if (values.Any(c => c == DBNull.Value))
                    {
                        for (var i = 0; i < values.Length; i++)
                        {
                            if (values[i] == DBNull.Value)
                            {
                                //var fieldInfo = type.GetField($"Item{i + 1}");
                                //values[i] = fieldInfo != null ? fieldInfo.FieldType.GetDefaultValue() : null;
                                values[i] = null;
                            }
                        }
                    }
                    model = (T)Activator.CreateInstance(typeof(T), values);
                }
            }
        }
        else if (type.IsValueType || type == typeof(string))// 值类型、字符串
        {
            var value = dataReader[0];
            if (value != DBNull.Value)
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
            var json = DbContextConfiguration.Options.JsonSerializer.Serialize(dic);
            model = DbContextConfiguration.Options.JsonSerializer.Deserialize<T>(json);
        }
        else if (type.IsClass && type.GetConstructor(Type.EmptyTypes) != null)// 实体类
        {
            model = Activator.CreateInstance<T>();
            var properties = type.GetProperties();
            for (var i = 0; i < dataReader.FieldCount; i++)
            {
                var fieldName = dataReader.GetName(i);
                var propertyInfo = properties.FirstOrDefault(c => fieldName.Equals(c.Name, StringComparison.OrdinalIgnoreCase));
                if (propertyInfo != null && propertyInfo.CanWrite)
                {
                    var value = dataReader[i];
                    if (value != DBNull.Value)
                    {
                        propertyInfo.SetValue(model, ObjectConvert.ChangeType(value, propertyInfo.PropertyType), null);
                    }
                }
            }
        }
        else
        {
            throw new NotSupportedException($"Unsupported type: {type.FullName}");
        }

        return model;
    }
}