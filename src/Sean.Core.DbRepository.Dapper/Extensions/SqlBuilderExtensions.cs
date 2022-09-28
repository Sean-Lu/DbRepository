using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;

namespace Sean.Core.DbRepository.Dapper.Extensions
{
    public static class SqlBuilderExtensions
    {
        #region Synchronous method
        /// <summary>
        /// 新增数据
        /// </summary>
        /// <param name="insertableSql"></param>
        /// <param name="connection">Database connection</param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <param name="sqlMonitor">输出执行的SQL语句</param>
        /// <returns>返回受影响的行数</returns>
        public static int ExecuteCommand(this IInsertableSql insertableSql, IDbConnection connection, IDbTransaction transaction = null, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(connection, insertableSql.Sql, insertableSql.Parameter));
            var result = connection.Execute(insertableSql.Sql, insertableSql.Parameter, transaction, commandTimeout);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(connection, insertableSql.Sql, insertableSql.Parameter));
            return result;
        }
        /// <summary>
        /// 新增数据
        /// </summary>
        /// <param name="insertableSql"></param>
        /// <param name="connection">Database connection</param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <param name="sqlMonitor">输出执行的SQL语句</param>
        /// <returns></returns>
        public static T ExecuteScalar<T>(this IInsertableSql insertableSql, IDbConnection connection, IDbTransaction transaction = null, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(connection, insertableSql.Sql, insertableSql.Parameter));
            var result = connection.ExecuteScalar<T>(insertableSql.Sql, insertableSql.Parameter, transaction, commandTimeout);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(connection, insertableSql.Sql, insertableSql.Parameter));
            return result;
        }

        /// <summary>
        /// 新增或更新数据
        /// </summary>
        /// <param name="replaceableSql"></param>
        /// <param name="connection">Database connection</param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <param name="sqlMonitor">输出执行的SQL语句</param>
        /// <returns>返回受影响的行数</returns>
        public static int ExecuteCommand(this IReplaceableSql replaceableSql, IDbConnection connection, IDbTransaction transaction = null, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(connection, replaceableSql.Sql, replaceableSql.Parameter));
            var result = connection.Execute(replaceableSql.Sql, replaceableSql.Parameter, transaction, commandTimeout);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(connection, replaceableSql.Sql, replaceableSql.Parameter));
            return result;
        }

        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="deleteableSql"></param>
        /// <param name="connection">Database connection</param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <param name="sqlMonitor">输出执行的SQL语句</param>
        /// <returns>返回受影响的行数</returns>
        public static int ExecuteCommand(this IDeleteableSql deleteableSql, IDbConnection connection, IDbTransaction transaction = null, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(connection, deleteableSql.Sql, deleteableSql.Parameter));
            var result = connection.Execute(deleteableSql.Sql, deleteableSql.Parameter, transaction, commandTimeout);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(connection, deleteableSql.Sql, deleteableSql.Parameter));
            return result;
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="updateableSql"></param>
        /// <param name="connection">Database connection</param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <param name="sqlMonitor">输出执行的SQL语句</param>
        /// <returns>返回受影响的行数</returns>
        public static int ExecuteCommand(this IUpdateableSql updateableSql, IDbConnection connection, IDbTransaction transaction = null, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(connection, updateableSql.Sql, updateableSql.Parameter));
            var result = connection.Execute(updateableSql.Sql, updateableSql.Parameter, transaction, commandTimeout);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(connection, updateableSql.Sql, updateableSql.Parameter));
            return result;
        }

        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="queryableSql"></param>
        /// <param name="connection">Database connection</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <param name="sqlMonitor">输出执行的SQL语句</param>
        /// <returns></returns>
        public static IEnumerable<TEntity> ExecuteCommand<TEntity>(this IQueryableSql queryableSql, IDbConnection connection, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(connection, queryableSql.Sql, queryableSql.Parameter));
            var result = connection.Query<TEntity>(queryableSql.Sql, queryableSql.Parameter, null, commandTimeout: commandTimeout);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(connection, queryableSql.Sql, queryableSql.Parameter));
            return result;
        }
        /// <summary>
        /// 查询单个数据
        /// </summary>
        /// <param name="queryableSql"></param>
        /// <param name="connection">Database connection</param>
        /// <param name="singleCheck">是否执行单一结果检查。true：如果查询到多个结果会抛出异常，false：默认取第一个结果或默认值</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <param name="sqlMonitor">输出执行的SQL语句</param>
        /// <returns></returns>
        public static TEntity ExecuteCommandSingleResult<TEntity>(this IQueryableSql queryableSql, IDbConnection connection, bool singleCheck = false, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(connection, queryableSql.Sql, queryableSql.Parameter));
            var result = singleCheck
                ? connection.QuerySingleOrDefault<TEntity>(queryableSql.Sql, queryableSql.Parameter, null, commandTimeout: commandTimeout)
                : connection.QueryFirstOrDefault<TEntity>(queryableSql.Sql, queryableSql.Parameter, null, commandTimeout: commandTimeout);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(connection, queryableSql.Sql, queryableSql.Parameter));
            return result;
        }

        /// <summary>
        /// 统计数量
        /// </summary>
        /// <param name="countableSql"></param>
        /// <param name="connection">Database connection</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <param name="sqlMonitor">输出执行的SQL语句</param>
        /// <returns></returns>
        public static int ExecuteCommand(this ICountableSql countableSql, IDbConnection connection, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(connection, countableSql.Sql, countableSql.Parameter));
            var result = connection.QueryFirstOrDefault<int>(countableSql.Sql, countableSql.Parameter, null, commandTimeout);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(connection, countableSql.Sql, countableSql.Parameter));
            return result;
        }
        #endregion

        #region Asynchronous method
#if NETSTANDARD || NET45_OR_GREATER
        /// <summary>
        /// 新增数据
        /// </summary>
        /// <param name="insertableSql"></param>
        /// <param name="connection">Database connection</param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <param name="sqlMonitor">输出执行的SQL语句</param>
        /// <returns>返回受影响的行数</returns>
        public static async Task<int> ExecuteCommandAsync(this IInsertableSql insertableSql, IDbConnection connection, IDbTransaction transaction = null, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(connection, insertableSql.Sql, insertableSql.Parameter));
            var result = await connection.ExecuteAsync(insertableSql.Sql, insertableSql.Parameter, transaction, commandTimeout);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(connection, insertableSql.Sql, insertableSql.Parameter));
            return result;
        }
        /// <summary>
        /// 新增数据
        /// </summary>
        /// <param name="insertableSql"></param>
        /// <param name="connection">Database connection</param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <param name="sqlMonitor">输出执行的SQL语句</param>
        /// <returns></returns>
        public static async Task<T> ExecuteScalarAsync<T>(this IInsertableSql insertableSql, IDbConnection connection, IDbTransaction transaction = null, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(connection, insertableSql.Sql, insertableSql.Parameter));
            var result = await connection.ExecuteScalarAsync<T>(insertableSql.Sql, insertableSql.Parameter, transaction, commandTimeout);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(connection, insertableSql.Sql, insertableSql.Parameter));
            return result;
        }

        /// <summary>
        /// 新增或更新数据
        /// </summary>
        /// <param name="replaceableSql"></param>
        /// <param name="connection">Database connection</param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <param name="sqlMonitor">输出执行的SQL语句</param>
        /// <returns>返回受影响的行数</returns>
        public static async Task<int> ExecuteCommandAsync(this IReplaceableSql replaceableSql, IDbConnection connection, IDbTransaction transaction = null, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(connection, replaceableSql.Sql, replaceableSql.Parameter));
            var result = await connection.ExecuteAsync(replaceableSql.Sql, replaceableSql.Parameter, transaction, commandTimeout);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(connection, replaceableSql.Sql, replaceableSql.Parameter));
            return result;
        }

        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="deleteableSql"></param>
        /// <param name="connection">Database connection</param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <param name="sqlMonitor">输出执行的SQL语句</param>
        /// <returns>返回受影响的行数</returns>
        public static async Task<int> ExecuteCommandAsync(this IDeleteableSql deleteableSql, IDbConnection connection, IDbTransaction transaction = null, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(connection, deleteableSql.Sql, deleteableSql.Parameter));
            var result = await connection.ExecuteAsync(deleteableSql.Sql, deleteableSql.Parameter, transaction, commandTimeout);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(connection, deleteableSql.Sql, deleteableSql.Parameter));
            return result;
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="updateableSql"></param>
        /// <param name="connection">Database connection</param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <param name="sqlMonitor">输出执行的SQL语句</param>
        /// <returns>返回受影响的行数</returns>
        public static async Task<int> ExecuteCommandAsync(this IUpdateableSql updateableSql, IDbConnection connection, IDbTransaction transaction = null, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(connection, updateableSql.Sql, updateableSql.Parameter));
            var result = await connection.ExecuteAsync(updateableSql.Sql, updateableSql.Parameter, transaction, commandTimeout);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(connection, updateableSql.Sql, updateableSql.Parameter));
            return result;
        }

        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="queryableSql"></param>
        /// <param name="connection">Database connection</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <param name="sqlMonitor">输出执行的SQL语句</param>
        /// <returns></returns>
        public static async Task<IEnumerable<TEntity>> ExecuteCommandAsync<TEntity>(this IQueryableSql queryableSql, IDbConnection connection, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(connection, queryableSql.Sql, queryableSql.Parameter));
            var result = await connection.QueryAsync<TEntity>(queryableSql.Sql, queryableSql.Parameter, null, commandTimeout: commandTimeout);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(connection, queryableSql.Sql, queryableSql.Parameter));
            return result;
        }
        /// <summary>
        /// 查询单个数据
        /// </summary>
        /// <param name="queryableSql"></param>
        /// <param name="connection">Database connection</param>
        /// <param name="singleCheck">是否执行单一结果检查。true：如果查询到多个结果会抛出异常，false：默认取第一个结果或默认值</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <param name="sqlMonitor">输出执行的SQL语句</param>
        /// <returns></returns>
        public static async Task<TEntity> ExecuteCommandSingleResultAsync<TEntity>(this IQueryableSql queryableSql, IDbConnection connection, bool singleCheck = false, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(connection, queryableSql.Sql, queryableSql.Parameter));
            var result = await (singleCheck
                ? connection.QuerySingleOrDefaultAsync<TEntity>(queryableSql.Sql, queryableSql.Parameter, null, commandTimeout: commandTimeout)
                : connection.QueryFirstOrDefaultAsync<TEntity>(queryableSql.Sql, queryableSql.Parameter, null, commandTimeout: commandTimeout));
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(connection, queryableSql.Sql, queryableSql.Parameter));
            return result;
        }

        /// <summary>
        /// 统计数量
        /// </summary>
        /// <param name="countableSql"></param>
        /// <param name="connection">Database connection</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <param name="sqlMonitor">输出执行的SQL语句</param>
        /// <returns></returns>
        public static async Task<int> ExecuteCommandAsync(this ICountableSql countableSql, IDbConnection connection, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            sqlMonitor?.OnSqlExecuting(new SqlExecutingContext(connection, countableSql.Sql, countableSql.Parameter));
            var result = await connection.QueryFirstOrDefaultAsync<int>(countableSql.Sql, countableSql.Parameter, null, commandTimeout);
            sqlMonitor?.OnSqlExecuted(new SqlExecutedContext(connection, countableSql.Sql, countableSql.Parameter));
            return result;
        }
#endif
        #endregion
    }
}
