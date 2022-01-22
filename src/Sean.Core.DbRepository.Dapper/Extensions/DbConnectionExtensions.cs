using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Sean.Core.DbRepository.Cache;
using Sean.Core.DbRepository.Contracts;
using Sean.Core.DbRepository.Extensions;
using Sean.Core.DbRepository.Factory;

namespace Sean.Core.DbRepository.Dapper.Extensions
{
    /// <summary>
    /// Extension methods of <see cref="IDbConnection"/>
    /// </summary>
    public static class DbConnectionExtensions
    {
        #region 同步方法
        /// <summary>
        /// 新增数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="entity"></param>
        /// <param name="returnId">是否返回自增主键Id</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static bool Add<TEntity>(this IDbConnection connection, IBaseRepository repository, TEntity entity, bool returnId = false, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            var sqlFactory = SqlFactory<TEntity>.Build(repository, true).ReturnLastInsertId(returnId, out var keyIdentityProperty);
            var sql = sqlFactory.InsertSql;
            if (returnId && keyIdentityProperty != null)
            {
                var id = connection.ExecuteScalar<long>(sql, entity, transaction, commandTimeout);
                repository.OutputExecutedSql(sql, entity);
                if (id > 0)
                {
                    keyIdentityProperty.SetValue(entity, id, null);
                    return true;
                }
                return false;
            }
            else
            {
                var result = connection.Execute(sql, entity, transaction, commandTimeout);
                repository.OutputExecutedSql(sql, entity);
                return result > 0;
            }
        }
        /// <summary>
        /// 批量新增数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="entities"></param>
        /// <param name="returnId">是否返回自增主键Id</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static bool Add<TEntity>(this IDbConnection connection, IBaseRepository repository, IList<TEntity> entities, bool returnId = false, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            if (!entities.Any()) return false;

            var sqlFactory = SqlFactory<TEntity>.Build(repository, true).ReturnLastInsertId(returnId, out var keyIdentityProperty);
            if (returnId && keyIdentityProperty != null)
            {
                var success = true;
                if (transaction?.Connection == null)
                {
                    success = repository.ExecuteTransaction(connection, trans =>
                   {
                       foreach (var entity in entities)
                       {
                           success = connection.Add(repository, entity, returnId, trans, commandTimeout);
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
                    foreach (var entity in entities)
                    {
                        success = transaction.Connection.Add(repository, entity, returnId, transaction, commandTimeout);
                        if (!success) break;
                    }
                }
                return success;
            }
            else
            {
                var sql = sqlFactory.InsertSql;
                var result = connection.Execute(sql, entities, transaction, commandTimeout);
                repository.OutputExecutedSql(sql, entities);
                return result == entities.Count;
            }
        }

        /// <summary>
        /// 删除数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="entity"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static bool Delete<TEntity>(this IDbConnection connection, IBaseRepository repository, TEntity entity, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            var sqlFactory = SqlFactory<TEntity>.Build(repository, true).SetParameter(entity);
            return connection.Delete(repository, sqlFactory, transaction, commandTimeout) > 0;
        }
        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="sqlFactory"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static int Delete<TEntity>(this IDbConnection connection, IBaseRepository repository, SqlFactory<TEntity> sqlFactory, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return connection.Delete(repository, (SqlFactory)sqlFactory, transaction, commandTimeout);
        }
        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="sqlFactory"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static int Delete(this IDbConnection connection, IBaseRepository repository, SqlFactory sqlFactory, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            var sql = sqlFactory.DeleteSql;
            var result = connection.Execute(sql, sqlFactory.Parameter, transaction, commandTimeout);
            repository.OutputExecutedSql(sql, sqlFactory.Parameter);
            return result;
        }
        /// <summary>
        /// 删除所有数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static int DeleteAll<TEntity>(this IDbConnection connection, IBaseRepository repository, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            var sqlFactory = SqlFactory<TEntity>.Build(repository, false);
            var sql = sqlFactory.DeleteAllSql;
            var result = connection.Execute(sql, sqlFactory.Parameter, transaction, commandTimeout);
            repository.OutputExecutedSql(sql, sqlFactory.Parameter);
            return result;
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="entity"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static bool Update<TEntity>(this IDbConnection connection, IBaseRepository repository, TEntity entity, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            var sqlFactory = SqlFactory<TEntity>.Build(repository, true).SetParameter(entity);
            return connection.Update<TEntity>(repository, sqlFactory, transaction, commandTimeout) > 0;
        }
        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="sqlFactory"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static int Update<TEntity>(this IDbConnection connection, IBaseRepository repository, SqlFactory<TEntity> sqlFactory, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return connection.Update(repository, (SqlFactory)sqlFactory, transaction, commandTimeout);
        }
        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="sqlFactory"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static int Update(this IDbConnection connection, IBaseRepository repository, SqlFactory sqlFactory, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            var sql = sqlFactory.UpdateSql;
            var result = connection.Execute(sql, sqlFactory.Parameter, transaction, commandTimeout);
            repository.OutputExecutedSql(sql, sqlFactory.Parameter);
            return result;
        }

        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="sqlFactory"></param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static IEnumerable<TEntity> Query<TEntity>(this IDbConnection connection, IBaseRepository repository, SqlFactory sqlFactory, int? commandTimeout = null)
        {
            var sql = sqlFactory.QuerySql;
            var result = connection.Query<TEntity>(sql, sqlFactory.Parameter, null, commandTimeout: commandTimeout);
            repository.OutputExecutedSql(sql, sqlFactory.Parameter);
            return result;
        }

        /// <summary>
        /// 查询单个数据
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="sqlFactory"></param>
        /// <param name="singleCheck">是否执行单一结果检查。true：如果查询到多个结果会抛出异常，false：默认取第一个结果或默认值</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static TEntity Get<TEntity>(this IDbConnection connection, IBaseRepository repository, SqlFactory sqlFactory, bool singleCheck = false, int? commandTimeout = null)
        {
            var sql = sqlFactory.QuerySql;
            var result = singleCheck ? connection.QuerySingleOrDefault<TEntity>(sql, sqlFactory.Parameter, null, commandTimeout: commandTimeout)
                : connection.QueryFirstOrDefault<TEntity>(sql, sqlFactory.Parameter, null, commandTimeout: commandTimeout);
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
        public static int Count<TEntity>(this IDbConnection connection, IBaseRepository repository, SqlFactory<TEntity> sqlFactory, int? commandTimeout = null)
        {
            return connection.Count(repository, (SqlFactory)sqlFactory, commandTimeout);
        }
        /// <summary>
        /// 统计数量
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="sqlFactory"></param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static int Count(this IDbConnection connection, IBaseRepository repository, SqlFactory sqlFactory, int? commandTimeout = null)
        {
            var sql = sqlFactory.CountSql;
            var result = connection.QueryFirstOrDefault<int>(sql, sqlFactory.Parameter, null, commandTimeout);
            repository.OutputExecutedSql(sql, sqlFactory.Parameter);
            return result;
        }
        /// <summary>
        /// 统计所有数量
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static int CountAll<TEntity>(this IDbConnection connection, IBaseRepository repository, int? commandTimeout = null)
        {
            var sqlFactory = SqlFactory<TEntity>.Build(repository, false);
            var sql = sqlFactory.CountAllSql;
            var result = connection.QueryFirstOrDefault<int>(sql, sqlFactory.Parameter, null, commandTimeout);
            repository.OutputExecutedSql(sql, sqlFactory.Parameter);
            return result;
        }

        /// <summary>
        /// 查询指定的表是否存在
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static bool IsTableExists(this IDbConnection connection, IBaseRepository repository, string tableName)
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
            var result = connection.QueryFirstOrDefault<int>(sql);
            repository.OutputExecutedSql(sql, null);
            var tableExists = result > 0;
            if (tableExists)
            {
                TableInfoCache.IsTableExists(tableName, true);
            }
            return tableExists;
        }
        #endregion

        #region 异步方法
#if NETSTANDARD || NET45_OR_GREATER
        /// <summary>
        /// 新增数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="entity"></param>
        /// <param name="returnId">是否返回自增主键Id</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<bool> AddAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, TEntity entity, bool returnId = false, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            var sqlFactory = SqlFactory<TEntity>.Build(repository, true).ReturnLastInsertId(returnId, out var keyIdentityProperty);
            var sql = sqlFactory.InsertSql;
            if (returnId && keyIdentityProperty != null)
            {
                var id = await connection.ExecuteScalarAsync<long>(sql, entity, transaction, commandTimeout);
                repository.OutputExecutedSql(sql, entity);
                if (id > 0)
                {
                    keyIdentityProperty.SetValue(entity, id, null);
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
        /// 批量新增数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="entities"></param>
        /// <param name="returnId">是否返回自增主键Id</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<bool> AddAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, IList<TEntity> entities, bool returnId = false, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            if (!entities.Any()) return false;

            var sqlFactory = SqlFactory<TEntity>.Build(repository, true).ReturnLastInsertId(returnId, out var keyIdentityProperty);
            if (returnId && keyIdentityProperty != null)
            {
                var success = true;
                if (transaction?.Connection == null)
                {
                    success = await repository.ExecuteTransactionAsync(connection, async trans =>
                    {
                        foreach (var entity in entities)
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
                    foreach (var entity in entities)
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
                var result = await connection.ExecuteAsync(sql, entities, transaction, commandTimeout);
                repository.OutputExecutedSql(sql, entities);
                return result == entities.Count;
            }
        }

        /// <summary>
        /// 删除数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="entity"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<bool> DeleteAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, TEntity entity, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            var sqlFactory = SqlFactory<TEntity>.Build(repository, true).SetParameter(entity);
            return await connection.DeleteAsync<TEntity>(repository, sqlFactory, transaction, commandTimeout) > 0;
        }
        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="sqlFactory"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<int> DeleteAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, SqlFactory<TEntity> sqlFactory, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return await connection.DeleteAsync(repository, (SqlFactory)sqlFactory, transaction, commandTimeout);
        }
        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="sqlFactory"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<int> DeleteAsync(this IDbConnection connection, IBaseRepository repository, SqlFactory sqlFactory, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            var sql = sqlFactory.DeleteSql;
            var result = await connection.ExecuteAsync(sql, sqlFactory.Parameter, transaction, commandTimeout);
            repository.OutputExecutedSql(sql, sqlFactory.Parameter);
            return result;
        }
        /// <summary>
        /// 删除所有数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<int> DeleteAllAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            var sqlFactory = SqlFactory<TEntity>.Build(repository, false);
            var sql = sqlFactory.DeleteAllSql;
            var result = await connection.ExecuteAsync(sql, sqlFactory.Parameter, transaction, commandTimeout);
            repository.OutputExecutedSql(sql, sqlFactory.Parameter);
            return result;
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="entity"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<bool> UpdateAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, TEntity entity, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            var sqlFactory = SqlFactory<TEntity>.Build(repository, true).SetParameter(entity);
            return await connection.UpdateAsync<TEntity>(repository, sqlFactory, transaction, commandTimeout) > 0;
        }
        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="sqlFactory"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<int> UpdateAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, SqlFactory<TEntity> sqlFactory, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return await connection.UpdateAsync(repository, (SqlFactory)sqlFactory, transaction, commandTimeout);
        }
        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="sqlFactory"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<int> UpdateAsync(this IDbConnection connection, IBaseRepository repository, SqlFactory sqlFactory, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            var sql = sqlFactory.UpdateSql;
            var result = await connection.ExecuteAsync(sql, sqlFactory.Parameter, transaction, commandTimeout);
            repository.OutputExecutedSql(sql, sqlFactory.Parameter);
            return result;
        }

        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="sqlFactory"></param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<IEnumerable<TEntity>> QueryAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, SqlFactory sqlFactory, int? commandTimeout = null)
        {
            var sql = sqlFactory.QuerySql;
            var result = await connection.QueryAsync<TEntity>(sql, sqlFactory.Parameter, null, commandTimeout: commandTimeout);
            repository.OutputExecutedSql(sql, sqlFactory.Parameter);
            return result;
        }

        /// <summary>
        /// 查询单个数据
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="sqlFactory"></param>
        /// <param name="singleCheck">是否执行单一结果检查。true：如果查询到多个结果会抛出异常，false：默认取第一个结果或默认值</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<TEntity> GetAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, SqlFactory sqlFactory, bool singleCheck = false, int? commandTimeout = null)
        {
            var sql = sqlFactory.QuerySql;
            var result = await (singleCheck ? connection.QuerySingleOrDefaultAsync<TEntity>(sql, sqlFactory.Parameter, null, commandTimeout: commandTimeout)
                : connection.QueryFirstOrDefaultAsync<TEntity>(sql, sqlFactory.Parameter, null, commandTimeout: commandTimeout));
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
        public static async Task<int> CountAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, SqlFactory<TEntity> sqlFactory, int? commandTimeout = null)
        {
            return await connection.CountAsync(repository, (SqlFactory)sqlFactory, commandTimeout);
        }
        /// <summary>
        /// 统计数量
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="sqlFactory"></param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<int> CountAsync(this IDbConnection connection, IBaseRepository repository, SqlFactory sqlFactory, int? commandTimeout = null)
        {
            var sql = sqlFactory.CountSql;
            var result = await connection.QueryFirstOrDefaultAsync<int>(sql, sqlFactory.Parameter, null, commandTimeout);
            repository.OutputExecutedSql(sql, sqlFactory.Parameter);
            return result;
        }
        /// <summary>
        /// 统计所有数量
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<int> CountAllAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, int? commandTimeout = null)
        {
            var sqlFactory = SqlFactory<TEntity>.Build(repository, false);
            var sql = sqlFactory.CountAllSql;
            var result = await connection.QueryFirstOrDefaultAsync<int>(sql, sqlFactory.Parameter, null, commandTimeout);
            repository.OutputExecutedSql(sql, sqlFactory.Parameter);
            return result;
        }

        /// <summary>
        /// 查询指定的表是否存在
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static async Task<bool> IsTableExistsAsync(this IDbConnection connection, IBaseRepository repository, string tableName)
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
#endif
        #endregion
    }
}
