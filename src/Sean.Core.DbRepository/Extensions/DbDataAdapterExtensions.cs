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
            adapter.SelectCommand = selectCommand;

            return adapter.SelectCommand.Execute(sqlMonitor, c =>
            {
                var result = new DataSet();
                adapter.Fill(result);
                return result;
            });
        }
        public static DataTable ExecuteDataTable(this DbDataAdapter adapter, DbCommand selectCommand, ISqlMonitor sqlMonitor)
        {
            adapter.SelectCommand = selectCommand;

            return adapter.SelectCommand.Execute(sqlMonitor, c =>
            {
                var result = new DataTable();
                adapter.Fill(result);
                return result;
            });
        }

        public static async Task<DataSet> ExecuteDataSetAsync(this DbDataAdapter adapter, DbCommand selectCommand, ISqlMonitor sqlMonitor)
        {
            adapter.SelectCommand = selectCommand;

            return await adapter.SelectCommand.ExecuteAsync(sqlMonitor, c =>
            {
                var result = new DataSet();
                adapter.Fill(result);
                return Task.FromResult(result);
            });
        }
        public static async Task<DataTable> ExecuteDataTableAsync(this DbDataAdapter adapter, DbCommand selectCommand, ISqlMonitor sqlMonitor)
        {
            adapter.SelectCommand = selectCommand;

            return await adapter.SelectCommand.ExecuteAsync(sqlMonitor, c =>
            {
                var result = new DataTable();
                adapter.Fill(result);
                return Task.FromResult(result);
            });
        }
    }
}
