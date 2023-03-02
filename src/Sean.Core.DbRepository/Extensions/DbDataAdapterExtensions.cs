using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace Sean.Core.DbRepository.Extensions
{
    /// <summary>
    /// Extensions for <see cref="DbDataAdapter"/>
    /// </summary>
    public static class DbDataAdapterExtensions
    {
        public static DataSet ExecuteDataSet(this DbDataAdapter adapter, DbCommand selectCommand, ISqlMonitor sqlMonitor)
        {
            return selectCommand.Execute(sqlMonitor, dbCommand =>
            {
                var result = new DataSet();
                adapter.SelectCommand = dbCommand;
                adapter.Fill(result);
                return result;
            });
        }
        public static DataTable ExecuteDataTable(this DbDataAdapter adapter, DbCommand selectCommand, ISqlMonitor sqlMonitor)
        {
            return selectCommand.Execute(sqlMonitor, dbCommand =>
            {
                var result = new DataTable();
                adapter.SelectCommand = dbCommand;
                adapter.Fill(result);
                return result;
            });
        }

        public static async Task<DataSet> ExecuteDataSetAsync(this DbDataAdapter adapter, DbCommand selectCommand, ISqlMonitor sqlMonitor)
        {
            return await selectCommand.ExecuteAsync(sqlMonitor, async dbCommand =>
            {
                var result = new DataSet();
                adapter.SelectCommand = dbCommand;
                adapter.Fill(result);
                return await Task.FromResult(result);
            });
        }
        public static async Task<DataTable> ExecuteDataTableAsync(this DbDataAdapter adapter, DbCommand selectCommand, ISqlMonitor sqlMonitor)
        {
            return await selectCommand.ExecuteAsync(sqlMonitor, async dbCommand =>
            {
                var result = new DataTable();
                adapter.SelectCommand = dbCommand;
                adapter.Fill(result);
                return await Task.FromResult(result);
            });
        }
    }
}
