using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository.Dapper.Extensions;

/// <summary>
/// Extensions for <see cref="IDbConnection"/>
/// </summary>
public static class DbConnectionExtensions
{
    #region Synchronous method
    public static int Execute(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
    {
        if (sqlCommand.OutputParameterOptions != null)
        {
            var dynamicParameters = new DynamicParameters(sqlCommand.Parameter);
            var result = sqlMonitor.Execute(connection, sqlCommand, () => connection.Execute(sqlCommand.Sql, dynamicParameters, sqlCommand.Transaction, sqlCommand.CommandTimeout, sqlCommand.CommandType));
            sqlCommand.OutputParameterOptions.ExecuteOutput(paramName => dynamicParameters.Get<object>(paramName));
            return result;
        }

        return sqlMonitor.Execute(connection, sqlCommand, () => connection.Execute(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, sqlCommand.CommandTimeout, sqlCommand.CommandType));
    }

    public static IEnumerable<T> Query<T>(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
    {
        return sqlMonitor.Execute(connection, sqlCommand, () => connection.Query<T>(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, commandTimeout: sqlCommand.CommandTimeout, commandType: sqlCommand.CommandType));
    }

    public static T Get<T>(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
    {
        return sqlMonitor.Execute(connection, sqlCommand, () => connection.QueryFirstOrDefault<T>(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, sqlCommand.CommandTimeout, sqlCommand.CommandType));
    }

    public static T ExecuteScalar<T>(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
    {
        return sqlMonitor.Execute(connection, sqlCommand, () => connection.ExecuteScalar<T>(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, sqlCommand.CommandTimeout, sqlCommand.CommandType));
    }

    public static object ExecuteScalar(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
    {
        return sqlMonitor.Execute(connection, sqlCommand, () => connection.ExecuteScalar(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, sqlCommand.CommandTimeout, sqlCommand.CommandType));
    }

    public static DataTable ExecuteDataTable(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
    {
        return sqlMonitor.Execute(connection, sqlCommand, () =>
        {
            using (var reader = connection.ExecuteReader(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, sqlCommand.CommandTimeout, sqlCommand.CommandType))
            {
                return reader.GetDataTable();
            }
        });
    }

    public static DataSet ExecuteDataSet(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
    {
        return sqlMonitor.Execute(connection, sqlCommand, () =>
        {
            using (var reader = connection.ExecuteReader(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, sqlCommand.CommandTimeout, sqlCommand.CommandType))
            {
                return reader.GetDataSet();
            }
        });
    }

    /// <summary>
    /// 创建 Dapper 原生 Reader，由其管理命令的释放。
    /// </summary>
    /// <param name="connection">执行命令的连接。</param>
    /// <param name="sqlCommand">SQL、参数及命令选项。</param>
    /// <param name="sqlMonitor">执行监控。</param>
    /// <param name="commandBehavior">Reader 行为；仅在允许 Reader 关闭此连接时指定 CloseConnection。</param>
    /// <returns>由调用方关闭或释放的 Reader。</returns>
    public static IDataReader ExecuteReader(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null, CommandBehavior commandBehavior = CommandBehavior.Default)
    {
        IDataReader reader = null;
        try
        {
            return sqlMonitor.Execute(connection, sqlCommand, () =>
                reader = connection.ExecuteReader(new CommandDefinition(sqlCommand.Sql, sqlCommand.Parameter,
                    sqlCommand.Transaction, sqlCommand.CommandTimeout, sqlCommand.CommandType), commandBehavior));
        }
        catch
        {
            // 完成监控也可能抛异常，此时 Reader 尚未返回，必须连同其持有的命令一起清理。
            try
            {
                reader?.Dispose();
            }
            catch
            {
                // 清理失败不能覆盖 Reader 创建或监控回调的原始异常。
            }
            throw;
        }
    }
    #endregion

    #region Asynchronous method
    public static async Task<int> ExecuteAsync(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
    {
        if (sqlCommand.OutputParameterOptions != null)
        {
            var dynamicParameters = new DynamicParameters(sqlCommand.Parameter);
            var result = await sqlMonitor.ExecuteAsync(connection, sqlCommand, async () => await connection.ExecuteAsync(sqlCommand.Sql, dynamicParameters, sqlCommand.Transaction, sqlCommand.CommandTimeout, sqlCommand.CommandType));
            sqlCommand.OutputParameterOptions.ExecuteOutput(paramName => dynamicParameters.Get<object>(paramName));
            return result;
        }

        return await sqlMonitor.ExecuteAsync(connection, sqlCommand, async () => await connection.ExecuteAsync(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, sqlCommand.CommandTimeout, sqlCommand.CommandType));
    }

    public static async Task<IEnumerable<T>> QueryAsync<T>(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
    {
        return await sqlMonitor.ExecuteAsync(connection, sqlCommand, async () => await connection.QueryAsync<T>(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, commandTimeout: sqlCommand.CommandTimeout, commandType: sqlCommand.CommandType));
    }

    public static async Task<T> GetAsync<T>(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
    {
        return await sqlMonitor.ExecuteAsync(connection, sqlCommand, async () => await connection.QueryFirstOrDefaultAsync<T>(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, sqlCommand.CommandTimeout, sqlCommand.CommandType));
    }

    public static async Task<T> ExecuteScalarAsync<T>(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
    {
        return await sqlMonitor.ExecuteAsync(connection, sqlCommand, async () => await connection.ExecuteScalarAsync<T>(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, sqlCommand.CommandTimeout, sqlCommand.CommandType));
    }

    public static async Task<object> ExecuteScalarAsync(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
    {
        return await sqlMonitor.ExecuteAsync(connection, sqlCommand, async () => await connection.ExecuteScalarAsync(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, sqlCommand.CommandTimeout, sqlCommand.CommandType));
    }

    public static async Task<DataTable> ExecuteDataTableAsync(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
    {
        return await sqlMonitor.ExecuteAsync(connection, sqlCommand, async () =>
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
        return await sqlMonitor.ExecuteAsync(connection, sqlCommand, async () =>
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

    /// <summary>
    /// 异步创建 Dapper 原生 Reader，由其管理命令的释放。
    /// </summary>
    /// <param name="connection">执行命令的连接。</param>
    /// <param name="sqlCommand">SQL、参数及命令选项。</param>
    /// <param name="sqlMonitor">执行监控。</param>
    /// <param name="commandBehavior">Reader 行为；仅在允许 Reader 关闭此连接时指定 CloseConnection。</param>
    /// <returns>由调用方关闭或释放的 Reader。</returns>
    public static async Task<IDataReader> ExecuteReaderAsync(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null, CommandBehavior commandBehavior = CommandBehavior.Default)
    {
        IDataReader reader = null;
        try
        {
            return await sqlMonitor.ExecuteAsync(connection, sqlCommand, async () =>
                reader = await connection.ExecuteReaderAsync(new CommandDefinition(sqlCommand.Sql, sqlCommand.Parameter,
                    sqlCommand.Transaction, sqlCommand.CommandTimeout, sqlCommand.CommandType), commandBehavior));
        }
        catch
        {
            // 与同步路径一致：监控失败不能把已经创建的 Reader 和命令遗留在调用方不可见的位置。
            try
            {
                reader?.Dispose();
            }
            catch
            {
                // 清理失败不能覆盖 Reader 创建或监控回调的原始异常。
            }
            throw;
        }
    }
    #endregion
}
