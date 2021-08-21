using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Sean.Core.DbRepository.Contracts;
using Sean.Core.DbRepository.Dapper.Cache;
using Sean.Core.DbRepository.Dapper.Impls;
using Sean.Core.DbRepository.Extensions;
using Sean.Core.DbRepository.Factory;
using Sean.Core.DbRepository.Impls;
using Sean.Utility.Extensions;

namespace Sean.Core.DbRepository.Dapper.Extensions
{
    /// <summary>
    /// Extension methods of <see cref="IDbConnection"/>
    /// </summary>
    public static class DbConnectionExtensions
    {
        #region 同步方法
        /// <summary>
        /// 新增
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="entity"></param>
        /// <param name="returnId">是否返回自增主键Id</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static bool Add<TEntity>(this IDbConnection connection, IBaseRepository<TEntity> repository, TEntity entity, bool returnId = false, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return connection.AddAsync(repository, entity, returnId, transaction, commandTimeout).Result;
        }
        /// <summary>
        /// 新增
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="entitys"></param>
        /// <param name="returnId">是否返回自增主键Id</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static bool Add<TEntity>(this IDbConnection connection, IBaseRepository<TEntity> repository, IList<TEntity> entitys, bool returnId = false, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return connection.AddAsync(repository, entitys, returnId, transaction, commandTimeout).Result;
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="entity"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static bool Delete<TEntity>(this IDbConnection connection, IBaseRepository<TEntity> repository, TEntity entity, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return connection.DeleteAsync(repository, entity, transaction, commandTimeout).Result;
        }
        /// <summary>
        /// 删除
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="sqlFactory"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static bool Delete<TEntity>(this IDbConnection connection, IBaseRepository<TEntity> repository, SqlFactory sqlFactory, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return connection.DeleteAsync(repository, sqlFactory, transaction, commandTimeout).Result;
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="entity"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static bool Update<TEntity>(this IDbConnection connection, IBaseRepository<TEntity> repository, TEntity entity, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return connection.UpdateAsync(repository, entity, transaction, commandTimeout).Result;
        }
        /// <summary>
        /// 更新
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="sqlFactory"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static bool Update<TEntity>(this IDbConnection connection, IBaseRepository<TEntity> repository, SqlFactory sqlFactory, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return connection.UpdateAsync(repository, sqlFactory, transaction, commandTimeout).Result;
        }

        /// <summary>
        /// 查询
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="sqlFactory"></param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static IEnumerable<TEntity> Query<TEntity>(this IDbConnection connection, IBaseRepository<TEntity> repository, SqlFactory sqlFactory, int? commandTimeout = null)
        {
            return connection.QueryAsync<TEntity>(repository, sqlFactory, commandTimeout).Result;
        }

        /// <summary>
        /// 统计数量
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="sqlFactory"></param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static int Count<TEntity>(this IDbConnection connection, IBaseRepository<TEntity> repository, SqlFactory sqlFactory, int? commandTimeout = null)
        {
            return connection.CountAsync(repository, sqlFactory, commandTimeout).Result;
        }

        /// <summary>
        /// 表是否存在
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static bool IsTableExists<TEntity>(this IDbConnection connection, IBaseRepository<TEntity> repository, string tableName)
        {
            return connection.IsTableExistsAsync(repository, tableName).Result;
        }
        #endregion

        #region 异步方法
        /// <summary>
        /// 新增
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="entity"></param>
        /// <param name="returnId">是否返回自增主键Id</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<bool> AddAsync<TEntity>(this IDbConnection connection, IBaseRepository<TEntity> repository, TEntity entity, bool returnId = false, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            var sqlFactory = SqlFactory<TEntity>.Build(repository).ReturnLastInsertId(returnId, out var keyIdentityProperty);
            var sql = sqlFactory.InsertSql;
            if (returnId && keyIdentityProperty != null)
            {
                var id = await connection.ExecuteScalarAsync<long>(sql, entity, transaction, commandTimeout);
                repository.OutputExecutedSql(sql, entity);
                if (id > 0)
                {
                    keyIdentityProperty.SetValue(entity, id);
                    return true;
                }
                return false;
            }
            else
            {
                var result = await connection.ExecuteAsync(sql, entity, transaction, commandTimeout);
                repository.OutputExecutedSql(sql, entity);
                return result > 0;
            }
        }
        /// <summary>
        /// 新增
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="entitys"></param>
        /// <param name="returnId">是否返回自增主键Id</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<bool> AddAsync<TEntity>(this IDbConnection connection, IBaseRepository<TEntity> repository, IList<TEntity> entitys, bool returnId = false, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            if (entitys == null) throw new ArgumentNullException(nameof(entitys));
            if (!entitys.Any()) return false;

            var sqlFactory = SqlFactory<TEntity>.Build(repository).ReturnLastInsertId(returnId, out var keyIdentityProperty);
            if (returnId && keyIdentityProperty != null)
            {
                var success = true;
                if (transaction?.Connection == null)
                {
                    success = await repository.ExecuteTransactionAsync(connection, async trans =>
                    {
                        foreach (var entity in entitys)
                        {
                            success = await connection.AddAsync(repository, entity, returnId, trans, commandTimeout);
                            if (!success) break;
                        }

                        if (success)
                        {
                            trans.Commit();
                        }
                        else
                        {
                            trans.Rollback();
                        }
                        return success;
                    });
                }
                else
                {
                    foreach (var entity in entitys)
                    {
                        success = await transaction.Connection.AddAsync(repository, entity, returnId, transaction, commandTimeout);
                        if (!success) break;
                    }
                }
                return success;
            }
            else
            {
                var sql = sqlFactory.InsertSql;
                var result = await connection.ExecuteAsync(sql, entitys, transaction, commandTimeout);
                repository.OutputExecutedSql(sql, entitys);
                return result == entitys.Count;
            }
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="entity"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<bool> DeleteAsync<TEntity>(this IDbConnection connection, IBaseRepository<TEntity> repository, TEntity entity, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            var sqlFactory = SqlFactory<TEntity>.Build(repository).SetParameter(entity);
            return await connection.DeleteAsync(repository, sqlFactory, transaction, commandTimeout);
        }
        /// <summary>
        /// 删除
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="sqlFactory"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<bool> DeleteAsync<TEntity>(this IDbConnection connection, IBaseRepository<TEntity> repository, SqlFactory sqlFactory, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            var sql = sqlFactory.DeleteSql;
            var result = await connection.ExecuteAsync(sql, sqlFactory.Parameter, transaction, commandTimeout);
            repository.OutputExecutedSql(sql, sqlFactory.Parameter);
            return result > 0;
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="entity"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<bool> UpdateAsync<TEntity>(this IDbConnection connection, IBaseRepository<TEntity> repository, TEntity entity, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            var sqlFactory = SqlFactory<TEntity>.Build(repository).SetParameter(entity);
            return await connection.UpdateAsync(repository, sqlFactory, transaction, commandTimeout);
        }
        /// <summary>
        /// 更新
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="sqlFactory"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<bool> UpdateAsync<TEntity>(this IDbConnection connection, IBaseRepository<TEntity> repository, SqlFactory sqlFactory, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            var sql = sqlFactory.UpdateSql;
            var result = await connection.ExecuteAsync(sql, sqlFactory.Parameter, transaction, commandTimeout);
            repository.OutputExecutedSql(sql, sqlFactory.Parameter);
            return result > 0;
        }

        /// <summary>
        /// 查询
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="sqlFactory"></param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<IEnumerable<TEntity>> QueryAsync<TEntity>(this IDbConnection connection, IBaseRepository<TEntity> repository, SqlFactory sqlFactory, int? commandTimeout = null)
        {
            var sql = sqlFactory.QuerySql;
            var result = await connection.QueryAsync<TEntity>(sql, sqlFactory.Parameter, null, commandTimeout);
            repository.OutputExecutedSql(sql, sqlFactory.Parameter);
            return result;
        }

        /// <summary>
        /// 统计数量
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="sqlFactory"></param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<int> CountAsync<TEntity>(this IDbConnection connection, IBaseRepository<TEntity> repository, SqlFactory sqlFactory, int? commandTimeout = null)
        {
            var sql = sqlFactory.CountSql;
            var result = await connection.QueryFirstOrDefaultAsync<int>(sql, sqlFactory.Parameter, null, commandTimeout);
            repository.OutputExecutedSql(sql, sqlFactory.Parameter);
            return result;
        }

        /// <summary>
        /// 表是否存在
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static async Task<bool> IsTableExistsAsync<TEntity>(this IDbConnection connection, IBaseRepository<TEntity> repository, string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return false;
            }

            if (TableInfoCache.IsTableExists(tableName))
            {
                return true;
            }

            var dbType = repository.Factory.DbType;
            var dbName = connection.Database;
            var sql = dbType.GetSqlForIsTableExists(dbName, tableName);
            var result = await connection.QueryFirstOrDefaultAsync<int>(sql);
            repository.OutputExecutedSql(sql, null);
            var tableExists = result > 0;
            if (tableExists)
            {
                TableInfoCache.IsTableExists(tableName, true);
            }
            return tableExists;
        }
        #endregion
    }
}
