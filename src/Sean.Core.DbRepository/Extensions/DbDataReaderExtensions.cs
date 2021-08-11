using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace Sean.Core.DbRepository.Extensions
{
    /// <summary>
    /// Extensions for <see cref="DbDataReader"/>
    /// </summary>
    public static class DbDataReaderExtensions
    {
        /// <summary>
        /// <see cref="DataTable"/>
        /// </summary>
        /// <param name="dataReader"></param>
        /// <returns></returns>
        public static DataTable GetDataTable(this DbDataReader dataReader)
        {
            var dataSet = dataReader.GetDataSet();
            return dataSet != null && dataSet.Tables.Count > 0 ? dataSet.Tables[0] : null;
        }
        /// <summary>
        /// <see cref="DataSet"/>
        /// </summary>
        /// <param name="dataReader"></param>
        /// <returns></returns>
        public static DataSet GetDataSet(this DbDataReader dataReader)
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
#if !NET40
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

                while (await dataReader.ReadAsync())
                {
                    var row = table.NewRow();
                    for (var i = 0; i < dataReader.FieldCount; i++)
                    {
                        row[i] = dataReader[i];
                    }
                    table.Rows.Add(row);
                }
                result.Tables.Add(table);
            } while (await dataReader.NextResultAsync());

            return result;
        }
#endif
    }
}
