using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;

namespace Sean.Core.DbRepository.Dapper.Extensions
{
    public static class SqlExtensions
    {
        #region Synchronous method
        public static int Execute(this ISqlWithParameter sql, IDbConnection connection, IDbTransaction transaction = null, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(connection, sql.Sql, sql.Parameter));
            var result = connection.Execute(sql.Sql, sql.Parameter, transaction, commandTimeout);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(connection, sql.Sql, sql.Parameter));
            return result;
        }
        public static int Execute(this ISqlWithParameter sql, IBaseRepository repository, bool master = true, IDbTransaction transaction = null)
        {
            return repository.Execute(connection => sql.Execute(connection, transaction, repository, repository.CommandTimeout), master, transaction);
        }

        public static IEnumerable<T> Query<T>(this ISqlWithParameter sql, IDbConnection connection, IDbTransaction transaction = null, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(connection, sql.Sql, sql.Parameter));
            var result = connection.Query<T>(sql.Sql, sql.Parameter, transaction, commandTimeout: commandTimeout);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(connection, sql.Sql, sql.Parameter));
            return result;
        }
        public static IEnumerable<T> Query<T>(this ISqlWithParameter sql, IBaseRepository repository, bool master = true, IDbTransaction transaction = null)
        {
            return repository.Execute(connection => sql.Query<T>(connection, transaction, repository, repository.CommandTimeout), master, transaction);
        }

        public static T QueryFirstOrDefault<T>(this ISqlWithParameter sql, IDbConnection connection, IDbTransaction transaction = null, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(connection, sql.Sql, sql.Parameter));
            //var result = singleCheck// Whether a single result is checked. If the value is true and multiple results are found, an exception will be thrown, otherwise the first result or the default value is returned.
            //    ? connection.QuerySingleOrDefault<T>(sql.Sql, sql.Parameter, transaction, commandTimeout: commandTimeout)
            //    : connection.QueryFirstOrDefault<T>(sql.Sql, sql.Parameter, transaction, commandTimeout: commandTimeout);
            var result = connection.QueryFirstOrDefault<T>(sql.Sql, sql.Parameter, transaction, commandTimeout: commandTimeout);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(connection, sql.Sql, sql.Parameter));
            return result;
        }
        public static T QueryFirstOrDefault<T>(this ISqlWithParameter sql, IBaseRepository repository, bool master = true, IDbTransaction transaction = null)
        {
            return repository.Execute(connection => sql.QueryFirstOrDefault<T>(connection, transaction, repository, repository.CommandTimeout), master, transaction);
        }

        public static T ExecuteScalar<T>(this ISqlWithParameter sql, IDbConnection connection, IDbTransaction transaction = null, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(connection, sql.Sql, sql.Parameter));
            var result = connection.ExecuteScalar<T>(sql.Sql, sql.Parameter, transaction, commandTimeout);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(connection, sql.Sql, sql.Parameter));
            return result;
        }
        public static T ExecuteScalar<T>(this ISqlWithParameter sql, IBaseRepository repository, bool master = true, IDbTransaction transaction = null)
        {
            return repository.Execute(connection => sql.ExecuteScalar<T>(connection, transaction, repository, repository.CommandTimeout), master, transaction);
        }

        public static object ExecuteScalar(this ISqlWithParameter sql, IDbConnection connection, IDbTransaction transaction = null, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(connection, sql.Sql, sql.Parameter));
            var result = connection.ExecuteScalar(sql.Sql, sql.Parameter, transaction, commandTimeout);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(connection, sql.Sql, sql.Parameter));
            return result;
        }
        public static object ExecuteScalar(this ISqlWithParameter sql, IBaseRepository repository, bool master = true, IDbTransaction transaction = null)
        {
            return repository.Execute(connection => sql.ExecuteScalar(connection, transaction, repository, repository.CommandTimeout), master, transaction);
        }
        #endregion

        #region Asynchronous method
#if NETSTANDARD || NET45_OR_GREATER
        public static async Task<int> ExecuteAsync(this ISqlWithParameter sql, IDbConnection connection, IDbTransaction transaction = null, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(connection, sql.Sql, sql.Parameter));
            var result = await connection.ExecuteAsync(sql.Sql, sql.Parameter, transaction, commandTimeout);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(connection, sql.Sql, sql.Parameter));
            return result;
        }
        public static async Task<int> ExecuteAsync(this ISqlWithParameter sql, IBaseRepository repository, bool master = true, IDbTransaction transaction = null)
        {
            return await repository.ExecuteAsync(async connection => await sql.ExecuteAsync(connection, transaction, repository, repository.CommandTimeout), master, transaction);
        }

        public static async Task<IEnumerable<T>> QueryAsync<T>(this ISqlWithParameter sql, IDbConnection connection, IDbTransaction transaction = null, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(connection, sql.Sql, sql.Parameter));
            var result = await connection.QueryAsync<T>(sql.Sql, sql.Parameter, transaction, commandTimeout: commandTimeout);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(connection, sql.Sql, sql.Parameter));
            return result;
        }
        public static async Task<IEnumerable<T>> QueryAsync<T>(this ISqlWithParameter sql, IBaseRepository repository, bool master = true, IDbTransaction transaction = null)
        {
            return await repository.ExecuteAsync(async connection => await sql.QueryAsync<T>(connection, transaction, repository, repository.CommandTimeout), master, transaction);
        }

        public static async Task<T> QueryFirstOrDefaultAsync<T>(this ISqlWithParameter sql, IDbConnection connection, IDbTransaction transaction = null, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(connection, sql.Sql, sql.Parameter));
            //var result = await (singleCheck// Whether a single result is checked. If the value is true and multiple results are found, an exception will be thrown, otherwise the first result or the default value is returned.
            //    ? connection.QuerySingleOrDefaultAsync<T>(sql.Sql, sql.Parameter, transaction, commandTimeout: commandTimeout)
            //    : connection.QueryFirstOrDefaultAsync<T>(sql.Sql, sql.Parameter, transaction, commandTimeout: commandTimeout));
            var result = await connection.QueryFirstOrDefaultAsync<T>(sql.Sql, sql.Parameter, transaction, commandTimeout: commandTimeout);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(connection, sql.Sql, sql.Parameter));
            return result;
        }
        public static async Task<T> QueryFirstOrDefaultAsync<T>(this ISqlWithParameter sql, IBaseRepository repository, bool master = true, IDbTransaction transaction = null)
        {
            return await repository.ExecuteAsync(async connection => await sql.QueryFirstOrDefaultAsync<T>(connection, transaction, repository, repository.CommandTimeout), master, transaction);
        }

        public static async Task<T> ExecuteScalarAsync<T>(this ISqlWithParameter sql, IDbConnection connection, IDbTransaction transaction = null, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(connection, sql.Sql, sql.Parameter));
            var result = await connection.ExecuteScalarAsync<T>(sql.Sql, sql.Parameter, transaction, commandTimeout);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(connection, sql.Sql, sql.Parameter));
            return result;
        }
        public static async Task<T> ExecuteScalarAsync<T>(this ISqlWithParameter sql, IBaseRepository repository, bool master = true, IDbTransaction transaction = null)
        {
            return await repository.ExecuteAsync(async connection => await sql.ExecuteScalarAsync<T>(connection, transaction, repository, repository.CommandTimeout), master, transaction);
        }

        public static async Task<object> ExecuteScalarAsync(this ISqlWithParameter sql, IDbConnection connection, IDbTransaction transaction = null, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(connection, sql.Sql, sql.Parameter));
            var result = await connection.ExecuteScalarAsync(sql.Sql, sql.Parameter, transaction, commandTimeout);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(connection, sql.Sql, sql.Parameter));
            return result;
        }
        public static async Task<object> ExecuteScalarAsync(this ISqlWithParameter sql, IBaseRepository repository, bool master = true, IDbTransaction transaction = null)
        {
            return await repository.ExecuteAsync(async connection => await sql.ExecuteScalarAsync(connection, transaction, repository, repository.CommandTimeout), master, transaction);
        }
#endif
        #endregion
    }
}
