using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

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
        Func<IDataRecord, T> mapper = null;
        var count = 0;
        // 数量判断必须先于 Read，避免达到上限后仍消费下一条记录。
        while ((!readCount.HasValue || readCount.Value <= 0 || count < readCount.Value)
               && dataReader.Read())
        {
            mapper ??= CreateModelMapper<T>(dataReader);
            T model = mapper(dataReader);
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
        Func<IDataRecord, T> mapper = null;
        var count = 0;
        // 与同步路径保持相同的短路顺序，达到上限后不再调用 ReadAsync。
        while ((!readCount.HasValue || readCount.Value <= 0 || count < readCount.Value)
               && await dataReader.ReadAsync())
        {
            mapper ??= CreateModelMapper<T>(dataReader);
            T model = mapper(dataReader);
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

    private static Func<IDataRecord, T> CreateModelMapper<T>(IDataRecord dataRecord)
    {
        var type = typeof(T);
        if (ResultValueMapping.IsTuple(type))
        {
            return record => (T)ResultValueMapping.CreateTuple(type, record.FieldCount, index => record[index], useConstructorDefaults: false);
        }

        // 泛型不等于元组：Nullable 走标量分支，普通泛型 DTO 继续按实体属性映射。
        if (type.IsValueType || type == typeof(string))// 值类型、字符串
        {
            return record =>
            {
                var value = record[0];
                return value != DBNull.Value ? (T)ResultValueMapping.ConvertValue(value, type) : default;
            };
        }
        else if (type == typeof(object))// dynamic动态类型
        {
            var columnNames = GetColumnNames(dataRecord);
            return record =>
            {
                var dic = new Dictionary<string, object>();
                for (var i = 0; i < record.FieldCount; i++)
                {
                    dic.Add(columnNames[i], record[i]);
                }
                var json = DbContextConfiguration.Options.JsonSerializer.Serialize(dic);
                return DbContextConfiguration.Options.JsonSerializer.Deserialize<T>(json);
            };
        }
        else if (type.IsClass && type.GetConstructor(Type.EmptyTypes) != null)// 实体类
        {
            // 列结构在同一结果集中保持不变，只解析一次即可；值转换和赋值仍逐行执行。
            var properties = EntityPropertyMapping.Create(type, GetColumnNames(dataRecord));
            return record =>
            {
                var model = Activator.CreateInstance<T>();
                for (var i = 0; i < properties.Length; i++)
                {
                    var propertyInfo = properties[i];
                    if (propertyInfo != null)
                    {
                        var value = record[i];
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

    private static IReadOnlyList<string> GetColumnNames(IDataRecord dataRecord)
    {
        var columnNames = new string[dataRecord.FieldCount];
        for (var index = 0; index < columnNames.Length; index++)
        {
            columnNames[index] = dataRecord.GetName(index);
        }
        return columnNames;
    }
}
