using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace Sean.Core.DbRepository.Extensions
{
    /// <summary>
    /// Extensions for <see cref="DbCommand"/>
    /// </summary>
    public static class DbCommandExtensions
    {
        public static T Execute<T>(this DbCommand command, ISqlMonitor sqlMonitor, Func<DbCommand, T> func)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(command.Connection, command.CommandText, command.Parameters));
            var result = func(command);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(command.Connection, command.CommandText, command.Parameters));
            return result;
        }
        public static int ExecuteNonQuery(this DbCommand command, ISqlMonitor sqlMonitor)
        {
            return command.Execute(sqlMonitor, c => c.ExecuteNonQuery());
        }
        public static DbDataReader ExecuteReader(this DbCommand command, ISqlMonitor sqlMonitor)
        {
            return command.Execute(sqlMonitor, c => c.ExecuteReader());
        }
        public static DbDataReader ExecuteReader(this DbCommand command, CommandBehavior behavior, ISqlMonitor sqlMonitor)
        {
            return command.Execute(sqlMonitor, c => c.ExecuteReader(behavior));
        }
        public static object ExecuteScalar(this DbCommand command, ISqlMonitor sqlMonitor)
        {
            return command.Execute(sqlMonitor, c => c.ExecuteScalar());
        }

        public static DataSet ExecuteDataSet(this DbCommand command, ISqlMonitor sqlMonitor, DbDataAdapter adapter = null)
        {
            if (adapter != null)
            {
                return adapter.ExecuteDataSet(command, sqlMonitor);
            }

            // 如果创建 DbDataAdapter 失败，则尝试通过 DbDataReader 读取器来获取数据
            using (var reader = command.ExecuteReader(CommandBehavior.Default, sqlMonitor))
            {
                return reader.GetDataSet();
            }
        }
        public static DataTable ExecuteDataTable(this DbCommand command, ISqlMonitor sqlMonitor, DbDataAdapter adapter = null)
        {
            if (adapter != null)
            {
                return adapter.ExecuteDataTable(command, sqlMonitor);
            }

            // 如果创建 DbDataAdapter 失败，则尝试通过 DbDataReader 读取器来获取数据
            using (var reader = command.ExecuteReader(CommandBehavior.Default, sqlMonitor))
            {
                return reader.GetDataTable();
            }
        }

        public static async Task<T> ExecuteAsync<T>(this DbCommand command, ISqlMonitor sqlMonitor, Func<DbCommand, Task<T>> func)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(command.Connection, command.CommandText, command.Parameters));
            var result = await func(command);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(command.Connection, command.CommandText, command.Parameters));
            return result;
        }
        public static async Task<int> ExecuteNonQueryAsync(this DbCommand command, ISqlMonitor sqlMonitor)
        {
            return await command.ExecuteAsync(sqlMonitor, async c => await c.ExecuteNonQueryAsync());
        }
        public static async Task<DbDataReader> ExecuteReaderAsync(this DbCommand command, ISqlMonitor sqlMonitor)
        {
            return await command.ExecuteAsync(sqlMonitor, async c => await c.ExecuteReaderAsync());
        }
        public static async Task<DbDataReader> ExecuteReaderAsync(this DbCommand command, CommandBehavior behavior, ISqlMonitor sqlMonitor)
        {
            return await command.ExecuteAsync(sqlMonitor, async c => await c.ExecuteReaderAsync(behavior));
        }
        public static async Task<object> ExecuteScalarAsync(this DbCommand command, ISqlMonitor sqlMonitor)
        {
            return await command.ExecuteAsync(sqlMonitor, async c => await c.ExecuteScalarAsync());
        }

        public static async Task<DataSet> ExecuteDataSetAsync(this DbCommand command, ISqlMonitor sqlMonitor, DbDataAdapter adapter = null)
        {
            if (adapter != null)
            {
                return await adapter.ExecuteDataSetAsync(command, sqlMonitor);
            }

            // 如果创建 DbDataAdapter 失败，则尝试通过 DbDataReader 读取器来获取数据
            using (var reader = await command.ExecuteReaderAsync(CommandBehavior.Default, sqlMonitor))
            {
                return reader.GetDataSet();
            }
        }
        public static async Task<DataTable> ExecuteDataTableAsync(this DbCommand command, ISqlMonitor sqlMonitor, DbDataAdapter adapter = null)
        {
            if (adapter != null)
            {
                return await adapter.ExecuteDataTableAsync(command, sqlMonitor);
            }

            // 如果创建 DbDataAdapter 失败，则尝试通过 DbDataReader 读取器来获取数据
            using (var reader = await command.ExecuteReaderAsync(CommandBehavior.Default, sqlMonitor))
            {
                return reader.GetDataTable();
            }
        }
    }
}
