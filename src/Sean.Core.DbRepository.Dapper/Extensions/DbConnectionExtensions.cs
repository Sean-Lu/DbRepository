using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Sean.Core.DbRepository.Extensions;
using Sean.Utility.AOP;

namespace Sean.Core.DbRepository.Dapper.Extensions
{
    /// <summary>
    /// Extensions for <see cref="IDbConnection"/>
    /// </summary>
    public static class DbConnectionExtensions
    {
        #region Synchronous method
        public static int Execute(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
        {
            return AspectF.Define
                .SqlMonitor(sqlMonitor, connection, sqlCommand)
                .Return(() => connection.Execute(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, sqlCommand.CommandTimeout, sqlCommand.CommandType));
        }

        public static IEnumerable<T> Query<T>(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
        {
            return AspectF.Define
                .SqlMonitor(sqlMonitor, connection, sqlCommand)
                .Return(() => connection.Query<T>(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, commandTimeout: sqlCommand.CommandTimeout, commandType: sqlCommand.CommandType));
        }

        public static T Get<T>(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
        {
            return AspectF.Define
                .SqlMonitor(sqlMonitor, connection, sqlCommand)
                .Return(() => connection.QueryFirstOrDefault<T>(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, sqlCommand.CommandTimeout, sqlCommand.CommandType));
        }

        public static T ExecuteScalar<T>(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
        {
            return AspectF.Define
                .SqlMonitor(sqlMonitor, connection, sqlCommand)
                .Return(() => connection.ExecuteScalar<T>(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, sqlCommand.CommandTimeout, sqlCommand.CommandType));
        }

        public static object ExecuteScalar(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
        {
            return AspectF.Define
                .SqlMonitor(sqlMonitor, connection, sqlCommand)
                .Return(() => connection.ExecuteScalar(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, sqlCommand.CommandTimeout, sqlCommand.CommandType));
        }

        public static DataTable ExecuteDataTable(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
        {
            return AspectF.Define
                .SqlMonitor(sqlMonitor, connection, sqlCommand)
                .Return(() =>
                {
                    using (var reader = connection.ExecuteReader(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, sqlCommand.CommandTimeout, sqlCommand.CommandType))
                    {
                        return reader.GetDataTable();
                    }
                });
        }

        public static DataSet ExecuteDataSet(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
        {
            return AspectF.Define
                .SqlMonitor(sqlMonitor, connection, sqlCommand)
                .Return(() =>
                {
                    using (var reader = connection.ExecuteReader(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, sqlCommand.CommandTimeout, sqlCommand.CommandType))
                    {
                        return reader.GetDataSet();
                    }
                });
        }

        public static IDataReader ExecuteReader(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
        {
            return AspectF.Define
                .SqlMonitor(sqlMonitor, connection, sqlCommand)
                .Return(() => connection.ExecuteReader(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, sqlCommand.CommandTimeout, sqlCommand.CommandType));
        }
        #endregion

        #region Asynchronous method
        public static async Task<int> ExecuteAsync(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
        {
            return await AspectF.Define
                .SqlMonitor(sqlMonitor, connection, sqlCommand)
                .Return(async () => await connection.ExecuteAsync(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, sqlCommand.CommandTimeout, sqlCommand.CommandType));
        }

        public static async Task<IEnumerable<T>> QueryAsync<T>(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
        {
            return await AspectF.Define
                .SqlMonitor(sqlMonitor, connection, sqlCommand)
                .Return(async () => await connection.QueryAsync<T>(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, commandTimeout: sqlCommand.CommandTimeout, commandType: sqlCommand.CommandType));
        }

        public static async Task<T> GetAsync<T>(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
        {
            return await AspectF.Define
                .SqlMonitor(sqlMonitor, connection, sqlCommand)
                .Return(async () => await connection.QueryFirstOrDefaultAsync<T>(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, sqlCommand.CommandTimeout, sqlCommand.CommandType));
        }

        public static async Task<T> ExecuteScalarAsync<T>(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
        {
            return await AspectF.Define
                .SqlMonitor(sqlMonitor, connection, sqlCommand)
                .Return(async () => await connection.ExecuteScalarAsync<T>(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, sqlCommand.CommandTimeout, sqlCommand.CommandType));
        }

        public static async Task<object> ExecuteScalarAsync(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
        {
            return await AspectF.Define
                .SqlMonitor(sqlMonitor, connection, sqlCommand)
                .Return(async () => await connection.ExecuteScalarAsync(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, sqlCommand.CommandTimeout, sqlCommand.CommandType));
        }

        public static async Task<DataTable> ExecuteDataTableAsync(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
        {
            return await AspectF.Define
                .SqlMonitor(sqlMonitor, connection, sqlCommand)
                .Return(async () =>
                {
                    using (var reader = await connection.ExecuteReaderAsync(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, sqlCommand.CommandTimeout, sqlCommand.CommandType))
                    {
                        if (reader is DbDataReader dbDataReader)
                        {
                            return await dbDataReader.GetDataTableAsync();
                        }

                        return reader.GetDataTable();
                    }
                });
        }

        public static async Task<DataSet> ExecuteDataSetAsync(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
        {
            return await AspectF.Define
                .SqlMonitor(sqlMonitor, connection, sqlCommand)
                .Return(async () =>
                {
                    using (var reader = await connection.ExecuteReaderAsync(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, sqlCommand.CommandTimeout, sqlCommand.CommandType))
                    {
                        if (reader is DbDataReader dbDataReader)
                        {
                            return await dbDataReader.GetDataSetAsync();
                        }

                        return reader.GetDataSet();
                    }
                });
        }

        public static async Task<IDataReader> ExecuteReaderAsync(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
        {
            return await AspectF.Define
                .SqlMonitor(sqlMonitor, connection, sqlCommand)
                .Return(async () => await connection.ExecuteReaderAsync(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, sqlCommand.CommandTimeout, sqlCommand.CommandType));
        }
        #endregion
    }
}
