using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;

namespace Sean.Core.DbRepository.Dapper.Extensions
{
    /// <summary>
    /// Extensions for <see cref="SqlFactory"/>
    /// </summary>
    public static class SqlFactoryExtensions
    {
        #region Synchronous method
        /// <summary>
        /// 新增数据：<see cref="SqlFactory.InsertSql"/>
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="outputExecutedSql"></param>
        /// <returns></returns>
        public static bool ExecuteInsertSql(this IInsertableSql sqlFactory, IDbConnection connection, IDbTransaction transaction = null, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var sql = sqlFactory.InsertSql;
            var result = connection.Execute(sql, sqlFactory.Parameter, transaction, commandTimeout);
            outputExecutedSql?.Invoke(sql, sqlFactory.Parameter);
            return result > 0;
        }

        /// <summary>
        /// 删除数据：<see cref="SqlFactory.DeleteSql"/>
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="outputExecutedSql"></param>
        /// <returns></returns>
        public static int ExecuteDeleteSql(this IDeleteableSql sqlFactory, IDbConnection connection, IDbTransaction transaction = null, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var sql = sqlFactory.DeleteSql;
            var result = connection.Execute(sql, sqlFactory.Parameter, transaction, commandTimeout);
            outputExecutedSql?.Invoke(sql, sqlFactory.Parameter);
            return result;
        }

        /// <summary>
        /// 更新数据：<see cref="SqlFactory.UpdateSql"/>
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="outputExecutedSql"></param>
        /// <returns></returns>
        public static int ExecuteUpdateSql(this IUpdateableSql sqlFactory, IDbConnection connection, IDbTransaction transaction = null, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var sql = sqlFactory.UpdateSql;
            var result = connection.Execute(sql, sqlFactory.Parameter, transaction, commandTimeout);
            outputExecutedSql?.Invoke(sql, sqlFactory.Parameter);
            return result;
        }

        /// <summary>
        /// 查询数据：<see cref="SqlFactory.QuerySql"/>
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="connection"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="outputExecutedSql"></param>
        /// <returns></returns>
        public static IEnumerable<TEntity> ExecuteQuerySql<TEntity>(this IQueryableSql sqlFactory, IDbConnection connection, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var sql = sqlFactory.QuerySql;
            var result = connection.Query<TEntity>(sql, sqlFactory.Parameter, null, commandTimeout: commandTimeout);
            outputExecutedSql?.Invoke(sql, sqlFactory.Parameter);
            return result;
        }
        /// <summary>
        /// 查询单个数据：<see cref="SqlFactory.QuerySql"/>
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="connection"></param>
        /// <param name="singleCheck">是否执行单一结果检查。true：如果查询到多个结果会抛出异常，false：默认取第一个结果或默认值</param>
        /// <param name="commandTimeout"></param>
        /// <param name="outputExecutedSql"></param>
        /// <returns></returns>
        public static TEntity ExecuteQuerySingleSql<TEntity>(this IQueryableSql sqlFactory, IDbConnection connection, bool singleCheck = false, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var sql = sqlFactory.QuerySql;
            var result = singleCheck
                ? connection.QuerySingleOrDefault<TEntity>(sql, sqlFactory.Parameter, null, commandTimeout: commandTimeout)
                : connection.QueryFirstOrDefault<TEntity>(sql, sqlFactory.Parameter, null, commandTimeout: commandTimeout);
            outputExecutedSql?.Invoke(sql, sqlFactory.Parameter);
            return result;
        }

        /// <summary>
        /// 统计数量：<see cref="SqlFactory.CountSql"/>
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="connection"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="outputExecutedSql"></param>
        /// <returns></returns>
        public static int ExecuteCountSql(this ICountableSql sqlFactory, IDbConnection connection, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var sql = sqlFactory.CountSql;
            var result = connection.QueryFirstOrDefault<int>(sql, sqlFactory.Parameter, null, commandTimeout);
            outputExecutedSql?.Invoke(sql, sqlFactory.Parameter);
            return result;
        }
        #endregion

        #region Asynchronous method
#if NETSTANDARD || NET45_OR_GREATER
        /// <summary>
        /// 新增数据：<see cref="SqlFactory.InsertSql"/>
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="outputExecutedSql"></param>
        /// <returns></returns>
        public static async Task<bool> ExecuteInsertSqlAsync(this IInsertableSql sqlFactory, IDbConnection connection, IDbTransaction transaction = null, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var sql = sqlFactory.InsertSql;
            var result = await connection.ExecuteAsync(sql, sqlFactory.Parameter, transaction, commandTimeout);
            outputExecutedSql?.Invoke(sql, sqlFactory.Parameter);
            return result > 0;
        }

        /// <summary>
        /// 删除数据：<see cref="SqlFactory.DeleteSql"/>
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="outputExecutedSql"></param>
        /// <returns></returns>
        public static async Task<int> ExecuteDeleteSqlAsync(this IDeleteableSql sqlFactory, IDbConnection connection, IDbTransaction transaction = null, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var sql = sqlFactory.DeleteSql;
            var result = await connection.ExecuteAsync(sql, sqlFactory.Parameter, transaction, commandTimeout);
            outputExecutedSql?.Invoke(sql, sqlFactory.Parameter);
            return result;
        }

        /// <summary>
        /// 更新数据：<see cref="SqlFactory.UpdateSql"/>
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="outputExecutedSql"></param>
        /// <returns></returns>
        public static async Task<int> ExecuteUpdateSqlAsync(this IUpdateableSql sqlFactory, IDbConnection connection, IDbTransaction transaction = null, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var sql = sqlFactory.UpdateSql;
            var result = await connection.ExecuteAsync(sql, sqlFactory.Parameter, transaction, commandTimeout);
            outputExecutedSql?.Invoke(sql, sqlFactory.Parameter);
            return result;
        }

        /// <summary>
        /// 查询数据：<see cref="SqlFactory.QuerySql"/>
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="connection"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="outputExecutedSql"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<TEntity>> ExecuteQuerySqlAsync<TEntity>(this IQueryableSql sqlFactory, IDbConnection connection, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var sql = sqlFactory.QuerySql;
            var result = await connection.QueryAsync<TEntity>(sql, sqlFactory.Parameter, null, commandTimeout: commandTimeout);
            outputExecutedSql?.Invoke(sql, sqlFactory.Parameter);
            return result;
        }
        /// <summary>
        /// 查询单个数据：<see cref="SqlFactory.QuerySql"/>
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="connection"></param>
        /// <param name="singleCheck">是否执行单一结果检查。true：如果查询到多个结果会抛出异常，false：默认取第一个结果或默认值</param>
        /// <param name="commandTimeout"></param>
        /// <param name="outputExecutedSql"></param>
        /// <returns></returns>
        public static async Task<TEntity> ExecuteQuerySingleSqlAsync<TEntity>(this IQueryableSql sqlFactory, IDbConnection connection, bool singleCheck = false, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var sql = sqlFactory.QuerySql;
            var result = await (singleCheck
                ? connection.QuerySingleOrDefaultAsync<TEntity>(sql, sqlFactory.Parameter, null, commandTimeout: commandTimeout)
                : connection.QueryFirstOrDefaultAsync<TEntity>(sql, sqlFactory.Parameter, null, commandTimeout: commandTimeout));
            outputExecutedSql?.Invoke(sql, sqlFactory.Parameter);
            return result;
        }

        /// <summary>
        /// 统计数量：<see cref="SqlFactory.CountSql"/>
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="connection"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="outputExecutedSql"></param>
        /// <returns></returns>
        public static async Task<int> ExecuteCountSqlAsync(this ICountableSql sqlFactory, IDbConnection connection, int? commandTimeout = null, Action<string, object> outputExecutedSql = null)
        {
            var sql = sqlFactory.CountSql;
            var result = await connection.QueryFirstOrDefaultAsync<int>(sql, sqlFactory.Parameter, null, commandTimeout);
            outputExecutedSql?.Invoke(sql, sqlFactory.Parameter);
            return result;
        }
#endif
        #endregion
    }
}
