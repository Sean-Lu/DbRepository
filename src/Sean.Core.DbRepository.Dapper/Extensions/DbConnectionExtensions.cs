using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

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
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(connection, sqlCommand.Sql, sqlCommand.Parameter));
            var result = connection.Execute(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, sqlCommand.CommandTimeout, sqlCommand.CommandType);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(connection, sqlCommand.Sql, sqlCommand.Parameter));
            return result;
        }

        public static IEnumerable<T> Query<T>(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(connection, sqlCommand.Sql, sqlCommand.Parameter));
            var result = connection.Query<T>(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, commandTimeout: sqlCommand.CommandTimeout, commandType: sqlCommand.CommandType);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(connection, sqlCommand.Sql, sqlCommand.Parameter));
            return result;
        }

        public static T Get<T>(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(connection, sqlCommand.Sql, sqlCommand.Parameter));
            //var result = singleCheck// Whether a single result is checked. If the value is true and multiple results are found, an exception will be thrown, otherwise the first result or the default value is returned.
            //    ? connection.QuerySingleOrDefault<T>(sqlCommand.Sql, sqlCommand.Parameter, transaction, commandTimeout: commandTimeout)
            //    : connection.QueryFirstOrDefault<T>(sqlCommand.Sql, sqlCommand.Parameter, transaction, commandTimeout: commandTimeout);
            var result = connection.QueryFirstOrDefault<T>(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, sqlCommand.CommandTimeout, sqlCommand.CommandType);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(connection, sqlCommand.Sql, sqlCommand.Parameter));
            return result;
        }

        public static T ExecuteScalar<T>(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(connection, sqlCommand.Sql, sqlCommand.Parameter));
            var result = connection.ExecuteScalar<T>(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, sqlCommand.CommandTimeout, sqlCommand.CommandType);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(connection, sqlCommand.Sql, sqlCommand.Parameter));
            return result;
        }

        public static object ExecuteScalar(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(connection, sqlCommand.Sql, sqlCommand.Parameter));
            var result = connection.ExecuteScalar(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, sqlCommand.CommandTimeout, sqlCommand.CommandType);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(connection, sqlCommand.Sql, sqlCommand.Parameter));
            return result;
        }
        #endregion

        #region Asynchronous method
#if NETSTANDARD || NET45_OR_GREATER
        public static async Task<int> ExecuteAsync(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(connection, sqlCommand.Sql, sqlCommand.Parameter));
            var result = await connection.ExecuteAsync(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, sqlCommand.CommandTimeout, sqlCommand.CommandType);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(connection, sqlCommand.Sql, sqlCommand.Parameter));
            return result;
        }

        public static async Task<IEnumerable<T>> QueryAsync<T>(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(connection, sqlCommand.Sql, sqlCommand.Parameter));
            var result = await connection.QueryAsync<T>(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, commandTimeout: sqlCommand.CommandTimeout, commandType: sqlCommand.CommandType);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(connection, sqlCommand.Sql, sqlCommand.Parameter));
            return result;
        }

        public static async Task<T> GetAsync<T>(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(connection, sqlCommand.Sql, sqlCommand.Parameter));
            //var result = await (singleCheck// Whether a single result is checked. If the value is true and multiple results are found, an exception will be thrown, otherwise the first result or the default value is returned.
            //    ? connection.QuerySingleOrDefaultAsync<T>(sqlCommand.Sql, sqlCommand.Parameter, transaction, commandTimeout: commandTimeout)
            //    : connection.QueryFirstOrDefaultAsync<T>(sqlCommand.Sql, sqlCommand.Parameter, transaction, commandTimeout: commandTimeout));
            var result = await connection.QueryFirstOrDefaultAsync<T>(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, sqlCommand.CommandTimeout, sqlCommand.CommandType);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(connection, sqlCommand.Sql, sqlCommand.Parameter));
            return result;
        }

        public static async Task<T> ExecuteScalarAsync<T>(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(connection, sqlCommand.Sql, sqlCommand.Parameter));
            var result = await connection.ExecuteScalarAsync<T>(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, sqlCommand.CommandTimeout, sqlCommand.CommandType);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(connection, sqlCommand.Sql, sqlCommand.Parameter));
            return result;
        }

        public static async Task<object> ExecuteScalarAsync(this IDbConnection connection, ISqlCommand sqlCommand, ISqlMonitor sqlMonitor = null)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(connection, sqlCommand.Sql, sqlCommand.Parameter));
            var result = await connection.ExecuteScalarAsync(sqlCommand.Sql, sqlCommand.Parameter, sqlCommand.Transaction, sqlCommand.CommandTimeout, sqlCommand.CommandType);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(connection, sqlCommand.Sql, sqlCommand.Parameter));
            return result;
        }
#endif
        #endregion
    }
}
