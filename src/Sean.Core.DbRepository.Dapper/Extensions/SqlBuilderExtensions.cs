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
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="outputExecutedSql"></param>
        /// <returns></returns>
        public static bool ExecuteCommandSuccessful(this IInsertableSql insertableSql, IDbConnection connection, IDbTransaction transaction = null, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            return insertableSql.ExecuteCommand(connection, transaction, commandTimeout, outputExecutedSql) > 0;
        }
        /// <summary>
        /// 新增数据
        /// </summary>
        /// <param name="insertableSql"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="outputExecutedSql"></param>
        /// <returns></returns>
        public static int ExecuteCommand(this IInsertableSql insertableSql, IDbConnection connection, IDbTransaction transaction = null, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var sql = insertableSql.InsertSql;
            var result = connection.Execute(sql, insertableSql.Parameter, transaction, commandTimeout);
            outputExecutedSql?.Invoke(sql, insertableSql.Parameter);
            return result;
        }

        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="deleteableSql"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="outputExecutedSql"></param>
        /// <returns></returns>
        public static int ExecuteCommand(this IDeleteableSql deleteableSql, IDbConnection connection, IDbTransaction transaction = null, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var sql = deleteableSql.DeleteSql;
            var result = connection.Execute(sql, deleteableSql.Parameter, transaction, commandTimeout);
            outputExecutedSql?.Invoke(sql, deleteableSql.Parameter);
            return result;
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="updateableSql"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="outputExecutedSql"></param>
        /// <returns></returns>
        public static int ExecuteCommand(this IUpdateableSql updateableSql, IDbConnection connection, IDbTransaction transaction = null, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var sql = updateableSql.UpdateSql;
            var result = connection.Execute(sql, updateableSql.Parameter, transaction, commandTimeout);
            outputExecutedSql?.Invoke(sql, updateableSql.Parameter);
            return result;
        }

        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="queryableSql"></param>
        /// <param name="connection"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="outputExecutedSql"></param>
        /// <returns></returns>
        public static IEnumerable<TEntity> ExecuteCommand<TEntity>(this IQueryableSql queryableSql, IDbConnection connection, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var sql = queryableSql.QuerySql;
            var result = connection.Query<TEntity>(sql, queryableSql.Parameter, null, commandTimeout: commandTimeout);
            outputExecutedSql?.Invoke(sql, queryableSql.Parameter);
            return result;
        }
        /// <summary>
        /// 查询单个数据
        /// </summary>
        /// <param name="queryableSql"></param>
        /// <param name="connection"></param>
        /// <param name="singleCheck">是否执行单一结果检查。true：如果查询到多个结果会抛出异常，false：默认取第一个结果或默认值</param>
        /// <param name="commandTimeout"></param>
        /// <param name="outputExecutedSql"></param>
        /// <returns></returns>
        public static TEntity ExecuteCommandSingleResult<TEntity>(this IQueryableSql queryableSql, IDbConnection connection, bool singleCheck = false, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var sql = queryableSql.QuerySql;
            var result = singleCheck
                ? connection.QuerySingleOrDefault<TEntity>(sql, queryableSql.Parameter, null, commandTimeout: commandTimeout)
                : connection.QueryFirstOrDefault<TEntity>(sql, queryableSql.Parameter, null, commandTimeout: commandTimeout);
            outputExecutedSql?.Invoke(sql, queryableSql.Parameter);
            return result;
        }

        /// <summary>
        /// 统计数量
        /// </summary>
        /// <param name="countableSql"></param>
        /// <param name="connection"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="outputExecutedSql"></param>
        /// <returns></returns>
        public static int ExecuteCommand(this ICountableSql countableSql, IDbConnection connection, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var sql = countableSql.CountSql;
            var result = connection.QueryFirstOrDefault<int>(sql, countableSql.Parameter, null, commandTimeout);
            outputExecutedSql?.Invoke(sql, countableSql.Parameter);
            return result;
        }
        #endregion

        #region Asynchronous method
#if NETSTANDARD || NET45_OR_GREATER
        /// <summary>
        /// 新增数据
        /// </summary>
        /// <param name="insertableSql"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="outputExecutedSql"></param>
        /// <returns></returns>
        public static async Task<bool> ExecuteCommandSuccessfulAsync(this IInsertableSql insertableSql, IDbConnection connection, IDbTransaction transaction = null, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            return await insertableSql.ExecuteCommandAsync(connection, transaction, commandTimeout, outputExecutedSql) > 0;
        }
        /// <summary>
        /// 新增数据
        /// </summary>
        /// <param name="insertableSql"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="outputExecutedSql"></param>
        /// <returns></returns>
        public static async Task<int> ExecuteCommandAsync(this IInsertableSql insertableSql, IDbConnection connection, IDbTransaction transaction = null, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var sql = insertableSql.InsertSql;
            var result = await connection.ExecuteAsync(sql, insertableSql.Parameter, transaction, commandTimeout);
            outputExecutedSql?.Invoke(sql, insertableSql.Parameter);
            return result;
        }

        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="deleteableSql"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="outputExecutedSql"></param>
        /// <returns></returns>
        public static async Task<int> ExecuteCommandAsync(this IDeleteableSql deleteableSql, IDbConnection connection, IDbTransaction transaction = null, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var sql = deleteableSql.DeleteSql;
            var result = await connection.ExecuteAsync(sql, deleteableSql.Parameter, transaction, commandTimeout);
            outputExecutedSql?.Invoke(sql, deleteableSql.Parameter);
            return result;
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="updateableSql"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="outputExecutedSql"></param>
        /// <returns></returns>
        public static async Task<int> ExecuteCommandAsync(this IUpdateableSql updateableSql, IDbConnection connection, IDbTransaction transaction = null, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var sql = updateableSql.UpdateSql;
            var result = await connection.ExecuteAsync(sql, updateableSql.Parameter, transaction, commandTimeout);
            outputExecutedSql?.Invoke(sql, updateableSql.Parameter);
            return result;
        }

        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="queryableSql"></param>
        /// <param name="connection"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="outputExecutedSql"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<TEntity>> ExecuteCommandAsync<TEntity>(this IQueryableSql queryableSql, IDbConnection connection, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var sql = queryableSql.QuerySql;
            var result = await connection.QueryAsync<TEntity>(sql, queryableSql.Parameter, null, commandTimeout: commandTimeout);
            outputExecutedSql?.Invoke(sql, queryableSql.Parameter);
            return result;
        }
        /// <summary>
        /// 查询单个数据
        /// </summary>
        /// <param name="queryableSql"></param>
        /// <param name="connection"></param>
        /// <param name="singleCheck">是否执行单一结果检查。true：如果查询到多个结果会抛出异常，false：默认取第一个结果或默认值</param>
        /// <param name="commandTimeout"></param>
        /// <param name="outputExecutedSql"></param>
        /// <returns></returns>
        public static async Task<TEntity> ExecuteCommandSingleResultAsync<TEntity>(this IQueryableSql queryableSql, IDbConnection connection, bool singleCheck = false, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var sql = queryableSql.QuerySql;
            var result = await (singleCheck
                ? connection.QuerySingleOrDefaultAsync<TEntity>(sql, queryableSql.Parameter, null, commandTimeout: commandTimeout)
                : connection.QueryFirstOrDefaultAsync<TEntity>(sql, queryableSql.Parameter, null, commandTimeout: commandTimeout));
            outputExecutedSql?.Invoke(sql, queryableSql.Parameter);
            return result;
        }

        /// <summary>
        /// 统计数量
        /// </summary>
        /// <param name="countableSql"></param>
        /// <param name="connection"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="outputExecutedSql"></param>
        /// <returns></returns>
        public static async Task<int> ExecuteCommandAsync(this ICountableSql countableSql, IDbConnection connection, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var sql = countableSql.CountSql;
            var result = await connection.QueryFirstOrDefaultAsync<int>(sql, countableSql.Parameter, null, commandTimeout);
            outputExecutedSql?.Invoke(sql, countableSql.Parameter);
            return result;
        }
#endif
        #endregion
    }
}
