using System;
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
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <param name="outputExecutedSql">输出执行的SQL语句</param>
        /// <returns>是否执行成功</returns>
        public static bool ExecuteCommandSuccessful(this IInsertableSql insertableSql, IDbConnection connection, IDbTransaction transaction = null, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            return insertableSql.ExecuteCommand(connection, transaction, commandTimeout, outputExecutedSql) > 0;
        }
        /// <summary>
        /// 新增数据
        /// </summary>
        /// <param name="insertableSql"></param>
        /// <param name="connection">Database connection</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <param name="outputExecutedSql">输出执行的SQL语句</param>
        /// <returns>返回受影响的行数</returns>
        public static int ExecuteCommand(this IInsertableSql insertableSql, IDbConnection connection, IDbTransaction transaction = null, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var result = connection.Execute(insertableSql.Sql, insertableSql.Parameter, transaction, commandTimeout);
            outputExecutedSql?.Invoke(insertableSql.Sql, insertableSql.Parameter);
            return result;
        }
        /// <summary>
        /// 新增数据
        /// </summary>
        /// <param name="insertableSql"></param>
        /// <param name="connection">Database connection</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <param name="outputExecutedSql">输出执行的SQL语句</param>
        /// <returns></returns>
        public static T ExecuteScalar<T>(this IInsertableSql insertableSql, IDbConnection connection, IDbTransaction transaction = null, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var result = connection.ExecuteScalar<T>(insertableSql.Sql, insertableSql.Parameter, transaction, commandTimeout);
            outputExecutedSql?.Invoke(insertableSql.Sql, insertableSql.Parameter);
            return result;
        }

        /// <summary>
        /// 新增或更新数据
        /// </summary>
        /// <param name="replaceableSql"></param>
        /// <param name="connection">Database connection</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <param name="outputExecutedSql">输出执行的SQL语句</param>
        /// <returns>是否执行成功</returns>
        public static bool ExecuteCommandSuccessful(this IReplaceableSql replaceableSql, IDbConnection connection, IDbTransaction transaction = null, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            return replaceableSql.ExecuteCommand(connection, transaction, commandTimeout, outputExecutedSql) > 0;
        }
        /// <summary>
        /// 新增或更新数据
        /// </summary>
        /// <param name="replaceableSql"></param>
        /// <param name="connection">Database connection</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <param name="outputExecutedSql">输出执行的SQL语句</param>
        /// <returns>返回受影响的行数</returns>
        public static int ExecuteCommand(this IReplaceableSql replaceableSql, IDbConnection connection, IDbTransaction transaction = null, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var result = connection.Execute(replaceableSql.Sql, replaceableSql.Parameter, transaction, commandTimeout);
            outputExecutedSql?.Invoke(replaceableSql.Sql, replaceableSql.Parameter);
            return result;
        }

        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="deleteableSql"></param>
        /// <param name="connection">Database connection</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <param name="outputExecutedSql">输出执行的SQL语句</param>
        /// <returns>返回受影响的行数</returns>
        public static int ExecuteCommand(this IDeleteableSql deleteableSql, IDbConnection connection, IDbTransaction transaction = null, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var result = connection.Execute(deleteableSql.Sql, deleteableSql.Parameter, transaction, commandTimeout);
            outputExecutedSql?.Invoke(deleteableSql.Sql, deleteableSql.Parameter);
            return result;
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="updateableSql"></param>
        /// <param name="connection">Database connection</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <param name="outputExecutedSql">输出执行的SQL语句</param>
        /// <returns>返回受影响的行数</returns>
        public static int ExecuteCommand(this IUpdateableSql updateableSql, IDbConnection connection, IDbTransaction transaction = null, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var result = connection.Execute(updateableSql.Sql, updateableSql.Parameter, transaction, commandTimeout);
            outputExecutedSql?.Invoke(updateableSql.Sql, updateableSql.Parameter);
            return result;
        }

        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="queryableSql"></param>
        /// <param name="connection">Database connection</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <param name="outputExecutedSql">输出执行的SQL语句</param>
        /// <returns></returns>
        public static IEnumerable<TEntity> ExecuteCommand<TEntity>(this IQueryableSql queryableSql, IDbConnection connection, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var result = connection.Query<TEntity>(queryableSql.Sql, queryableSql.Parameter, null, commandTimeout: commandTimeout);
            outputExecutedSql?.Invoke(queryableSql.Sql, queryableSql.Parameter);
            return result;
        }
        /// <summary>
        /// 查询单个数据
        /// </summary>
        /// <param name="queryableSql"></param>
        /// <param name="connection">Database connection</param>
        /// <param name="singleCheck">是否执行单一结果检查。true：如果查询到多个结果会抛出异常，false：默认取第一个结果或默认值</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <param name="outputExecutedSql">输出执行的SQL语句</param>
        /// <returns></returns>
        public static TEntity ExecuteCommandSingleResult<TEntity>(this IQueryableSql queryableSql, IDbConnection connection, bool singleCheck = false, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var result = singleCheck
                ? connection.QuerySingleOrDefault<TEntity>(queryableSql.Sql, queryableSql.Parameter, null, commandTimeout: commandTimeout)
                : connection.QueryFirstOrDefault<TEntity>(queryableSql.Sql, queryableSql.Parameter, null, commandTimeout: commandTimeout);
            outputExecutedSql?.Invoke(queryableSql.Sql, queryableSql.Parameter);
            return result;
        }

        /// <summary>
        /// 统计数量
        /// </summary>
        /// <param name="countableSql"></param>
        /// <param name="connection">Database connection</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <param name="outputExecutedSql">输出执行的SQL语句</param>
        /// <returns></returns>
        public static int ExecuteCommand(this ICountableSql countableSql, IDbConnection connection, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var result = connection.QueryFirstOrDefault<int>(countableSql.Sql, countableSql.Parameter, null, commandTimeout);
            outputExecutedSql?.Invoke(countableSql.Sql, countableSql.Parameter);
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
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <param name="outputExecutedSql">输出执行的SQL语句</param>
        /// <returns>是否执行成功</returns>
        public static async Task<bool> ExecuteCommandSuccessfulAsync(this IInsertableSql insertableSql, IDbConnection connection, IDbTransaction transaction = null, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            return await insertableSql.ExecuteCommandAsync(connection, transaction, commandTimeout, outputExecutedSql) > 0;
        }
        /// <summary>
        /// 新增数据
        /// </summary>
        /// <param name="insertableSql"></param>
        /// <param name="connection">Database connection</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <param name="outputExecutedSql">输出执行的SQL语句</param>
        /// <returns>返回受影响的行数</returns>
        public static async Task<int> ExecuteCommandAsync(this IInsertableSql insertableSql, IDbConnection connection, IDbTransaction transaction = null, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var result = await connection.ExecuteAsync(insertableSql.Sql, insertableSql.Parameter, transaction, commandTimeout);
            outputExecutedSql?.Invoke(insertableSql.Sql, insertableSql.Parameter);
            return result;
        }
        /// <summary>
        /// 新增数据
        /// </summary>
        /// <param name="insertableSql"></param>
        /// <param name="connection">Database connection</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <param name="outputExecutedSql">输出执行的SQL语句</param>
        /// <returns></returns>
        public static async Task<T> ExecuteScalarAsync<T>(this IInsertableSql insertableSql, IDbConnection connection, IDbTransaction transaction = null, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var result = await connection.ExecuteScalarAsync<T>(insertableSql.Sql, insertableSql.Parameter, transaction, commandTimeout);
            outputExecutedSql?.Invoke(insertableSql.Sql, insertableSql.Parameter);
            return result;
        }

        /// <summary>
        /// 新增或更新数据
        /// </summary>
        /// <param name="replaceableSql"></param>
        /// <param name="connection">Database connection</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <param name="outputExecutedSql">输出执行的SQL语句</param>
        /// <returns>是否执行成功</returns>
        public static async Task<bool> ExecuteCommandSuccessfulAsync(this IReplaceableSql replaceableSql, IDbConnection connection, IDbTransaction transaction = null, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            return await replaceableSql.ExecuteCommandAsync(connection, transaction, commandTimeout, outputExecutedSql) > 0;
        }
        /// <summary>
        /// 新增或更新数据
        /// </summary>
        /// <param name="replaceableSql"></param>
        /// <param name="connection">Database connection</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <param name="outputExecutedSql">输出执行的SQL语句</param>
        /// <returns>返回受影响的行数</returns>
        public static async Task<int> ExecuteCommandAsync(this IReplaceableSql replaceableSql, IDbConnection connection, IDbTransaction transaction = null, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var result = await connection.ExecuteAsync(replaceableSql.Sql, replaceableSql.Parameter, transaction, commandTimeout);
            outputExecutedSql?.Invoke(replaceableSql.Sql, replaceableSql.Parameter);
            return result;
        }

        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="deleteableSql"></param>
        /// <param name="connection">Database connection</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <param name="outputExecutedSql">输出执行的SQL语句</param>
        /// <returns>返回受影响的行数</returns>
        public static async Task<int> ExecuteCommandAsync(this IDeleteableSql deleteableSql, IDbConnection connection, IDbTransaction transaction = null, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var result = await connection.ExecuteAsync(deleteableSql.Sql, deleteableSql.Parameter, transaction, commandTimeout);
            outputExecutedSql?.Invoke(deleteableSql.Sql, deleteableSql.Parameter);
            return result;
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="updateableSql"></param>
        /// <param name="connection">Database connection</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <param name="outputExecutedSql">输出执行的SQL语句</param>
        /// <returns>返回受影响的行数</returns>
        public static async Task<int> ExecuteCommandAsync(this IUpdateableSql updateableSql, IDbConnection connection, IDbTransaction transaction = null, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var result = await connection.ExecuteAsync(updateableSql.Sql, updateableSql.Parameter, transaction, commandTimeout);
            outputExecutedSql?.Invoke(updateableSql.Sql, updateableSql.Parameter);
            return result;
        }

        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="queryableSql"></param>
        /// <param name="connection">Database connection</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <param name="outputExecutedSql">输出执行的SQL语句</param>
        /// <returns></returns>
        public static async Task<IEnumerable<TEntity>> ExecuteCommandAsync<TEntity>(this IQueryableSql queryableSql, IDbConnection connection, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var result = await connection.QueryAsync<TEntity>(queryableSql.Sql, queryableSql.Parameter, null, commandTimeout: commandTimeout);
            outputExecutedSql?.Invoke(queryableSql.Sql, queryableSql.Parameter);
            return result;
        }
        /// <summary>
        /// 查询单个数据
        /// </summary>
        /// <param name="queryableSql"></param>
        /// <param name="connection">Database connection</param>
        /// <param name="singleCheck">是否执行单一结果检查。true：如果查询到多个结果会抛出异常，false：默认取第一个结果或默认值</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <param name="outputExecutedSql">输出执行的SQL语句</param>
        /// <returns></returns>
        public static async Task<TEntity> ExecuteCommandSingleResultAsync<TEntity>(this IQueryableSql queryableSql, IDbConnection connection, bool singleCheck = false, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var result = await (singleCheck
                ? connection.QuerySingleOrDefaultAsync<TEntity>(queryableSql.Sql, queryableSql.Parameter, null, commandTimeout: commandTimeout)
                : connection.QueryFirstOrDefaultAsync<TEntity>(queryableSql.Sql, queryableSql.Parameter, null, commandTimeout: commandTimeout));
            outputExecutedSql?.Invoke(queryableSql.Sql, queryableSql.Parameter);
            return result;
        }

        /// <summary>
        /// 统计数量
        /// </summary>
        /// <param name="countableSql"></param>
        /// <param name="connection">Database connection</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <param name="outputExecutedSql">输出执行的SQL语句</param>
        /// <returns></returns>
        public static async Task<int> ExecuteCommandAsync(this ICountableSql countableSql, IDbConnection connection, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var result = await connection.QueryFirstOrDefaultAsync<int>(countableSql.Sql, countableSql.Parameter, null, commandTimeout);
            outputExecutedSql?.Invoke(countableSql.Sql, countableSql.Parameter);
            return result;
        }
#endif
        #endregion
    }
}
