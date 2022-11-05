using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Sean.Core.DbRepository.Extensions;
using Sean.Core.DbRepository.Util;

namespace Sean.Core.DbRepository.Dapper.Extensions
{
    /// <summary>
    /// Extensions for <see cref="IDbConnection"/>
    /// </summary>
    public static class DbConnectionExtensions
    {
        #region Synchronous method
        /// <summary>
        /// 新增数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="entity"></param>
        /// <param name="returnAutoIncrementId">Whether to return the auto-increment primary key Id.</param>
        /// <param name="fieldExpression">The fields to insert. If the value is null, all fields of the entity are inserted (excluding NotMapped fields). Example: 
        /// <para>1. Single field: entity => entity.Status</para>
        /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>Whether the command is executed successfully.</returns>
        public static bool Add<TEntity>(this IDbConnection connection, IBaseRepository repository, TEntity entity, bool returnAutoIncrementId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
        {
            if (entity == null) return false;

            PropertyInfo keyIdentityProperty;
            if (returnAutoIncrementId && (keyIdentityProperty = typeof(TEntity).GetKeyIdentityProperty()) != null)
            {
                var id = repository.GetSqlForAdd(entity, true, fieldExpression).ExecuteScalar<long>(connection, transaction, repository, repository.CommandTimeout);
                if (id < 1) return false;

                keyIdentityProperty.SetValue(entity, id, null);
                return true;
            }

            return repository.GetSqlForAdd(entity, false, fieldExpression).Execute(connection, transaction, repository, repository.CommandTimeout) > 0;
        }

        /// <summary>
        /// 批量新增数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="entities"></param>
        /// <param name="returnAutoIncrementId">Whether to return the auto-increment primary key Id.</param>
        /// <param name="fieldExpression">The fields to insert. If the value is null, all fields of the entity are inserted (excluding NotMapped fields). Example: 
        /// <para>1. Single field: entity => entity.Status</para>
        /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>Whether the command is executed successfully.</returns>
        public static bool BulkAdd<TEntity>(this IDbConnection connection, IBaseRepository repository, IEnumerable<TEntity> entities, bool returnAutoIncrementId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
        {
            if (entities == null || !entities.Any()) return false;

            if (returnAutoIncrementId && typeof(TEntity).GetKeyIdentityProperty() != null)
            {
                if (transaction?.Connection == null)
                {
                    return repository.ExecuteAutoTransaction(connection, trans =>
                    {
                        foreach (var entity in entities)
                        {
                            if (!connection.Add(repository, entity, returnAutoIncrementId, fieldExpression, trans))
                            {
                                return false;
                            }
                        }

                        return true;
                    });
                }

                foreach (var entity in entities)
                {
                    if (!transaction.Connection.Add(repository, entity, returnAutoIncrementId, fieldExpression, transaction))
                    {
                        return false;
                    }
                }

                return true;
            }

            ISqlWithParameter sql = repository.CreateInsertableBuilder<TEntity>(fieldExpression == null)
                .IncludeFields(fieldExpression)
                //.ReturnAutoIncrementId(returnAutoIncrementId)
                .SetParameter(entities)// BulkInsert
                .Build();
            return sql.Execute(connection, transaction, repository, repository.CommandTimeout) > 0;
        }

        /// <summary>
        /// 新增或更新数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="entity"></param>
        /// <param name="fieldExpression">The fields to insert or update. If the value is null, all fields of the entity are inserted or updated (excluding NotMapped fields). Example: 
        /// <para>1. Single field: entity => entity.Status</para>
        /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>Whether the command is executed successfully.</returns>
        public static bool AddOrUpdate<TEntity>(this IDbConnection connection, IBaseRepository repository, TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
        {
            if (entity == null) return false;

            switch (repository.DbType)
            {
                case DatabaseType.MySql:
                case DatabaseType.SQLite:
                    {
                        return repository.GetSqlForAddOrUpdate(entity, fieldExpression).Execute(connection, transaction, repository, repository.CommandTimeout) > 0;
                    }
                default:
                    {
                        if (repository.GetSqlForEntityExists(entity).ExecuteScalar<int>(connection, null, repository, repository.CommandTimeout) < 1)
                        {
                            // INSERT
                            return connection.Add(repository, entity, false, fieldExpression, transaction);
                        }

                        if (transaction?.Connection != null)
                        {
                            // DELETE
                            if (!transaction.Connection.Delete(repository, entity, transaction))
                            {
                                return false;
                            }

                            // INSERT
                            return transaction.Connection.Add(repository, entity, false, fieldExpression, transaction);
                        }

                        return repository.ExecuteAutoTransaction(connection, trans =>
                        {
                            // DELETE
                            if (!connection.Delete(repository, entity, trans))
                            {
                                return false;
                            }

                            // INSERT
                            if (!connection.Add(repository, entity, false, fieldExpression, trans))
                            {
                                return false;
                            }

                            return true;
                        });
                    }
            }
        }

        /// <summary>
        /// 批量新增或更新数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="entities"></param>
        /// <param name="fieldExpression">The fields to insert or update. If the value is null, all fields of the entity are inserted or updated (excluding NotMapped fields). Example: 
        /// <para>1. Single field: entity => entity.Status</para>
        /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>Whether the command is executed successfully.</returns>
        public static bool BulkAddOrUpdate<TEntity>(this IDbConnection connection, IBaseRepository repository, IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
        {
            if (entities == null || !entities.Any()) return false;

            switch (repository.DbType)
            {
                case DatabaseType.MySql:
                case DatabaseType.SQLite:
                    ISqlWithParameter sql = repository.CreateReplaceableBuilder<TEntity>(fieldExpression == null)
                        .IncludeFields(fieldExpression)
                        .SetParameter(entities)
                        .Build();
                    return sql.Execute(connection, transaction, repository, repository.CommandTimeout) > 0;
                default:
                    if (transaction?.Connection == null)
                    {
                        return repository.ExecuteAutoTransaction(connection, trans =>
                        {
                            foreach (var entity in entities)
                            {
                                if (!connection.AddOrUpdate(repository, entity, fieldExpression, trans))
                                {
                                    return false;
                                }
                            }

                            return true;
                        });
                    }

                    foreach (var entity in entities)
                    {
                        if (!transaction.Connection.AddOrUpdate(repository, entity, fieldExpression, transaction))
                        {
                            return false;
                        }
                    }

                    return true;
            }
        }

        /// <summary>
        /// 删除数据
        /// <para>Delete by primary key.</para>
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="entity"></param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>Whether the command is executed successfully.</returns>
        public static bool Delete<TEntity>(this IDbConnection connection, IBaseRepository repository, TEntity entity, IDbTransaction transaction = null)
        {
            return repository.GetSqlForDelete(entity).Execute(connection, transaction, repository, repository.CommandTimeout) > 0;
        }
        /// <summary>
        /// 删除数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>The number of rows affected.</returns>
        public static int Delete<TEntity>(this IDbConnection connection, IBaseRepository repository, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null)
        {
            return repository.GetSqlForDelete(whereExpression).Execute(connection, transaction, repository, repository.CommandTimeout);
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="entity"></param>
        /// <param name="fieldExpression">The fields to update. If the value is null, all fields of the entity are updated (excluding primary key fields and NotMapped fields). Example: 
        /// <para>1. Single field: entity => entity.Status</para>
        /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition. If the value is null, the SQL WHERE condition will default to the primary key field of the entity.
        /// <para>1. If the entity does not contain primary key field and the value is null, an exception will be thrown (to prevent all data in the table from being incorrectly updated).</para>
        /// <para>2. Code example for update all data in the table: entity => true.</para>
        /// </param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>The number of rows affected.</returns>
        public static int Update<TEntity>(this IDbConnection connection, IBaseRepository repository, TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, Expression<Func<TEntity, bool>> whereExpression = null, IDbTransaction transaction = null)
        {
            return repository.GetSqlForUpdate(entity, fieldExpression, whereExpression).Execute(connection, transaction, repository, repository.CommandTimeout);
        }

        /// <summary>
        /// 批量更新数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="entities"></param>
        /// <param name="fieldExpression">The fields to update. If the value is null, all fields of the entity are updated (excluding primary key fields and NotMapped fields). Example: 
        /// <para>1. Single field: entity => entity.Status</para>
        /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>Whether the command is executed successfully.</returns>
        public static bool BulkUpdate<TEntity>(this IDbConnection connection, IBaseRepository repository, IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
        {
            if (entities == null || !entities.Any()) return false;

            if (transaction?.Connection == null)
            {
                return repository.ExecuteAutoTransaction(connection, trans =>
                {
                    foreach (var entity in entities)
                    {
                        if (!(connection.Update(repository, entity, fieldExpression, null, trans) > 0))
                        {
                            return false;
                        }
                    }

                    return true;
                });
            }

            foreach (var entity in entities)
            {
                if (!(transaction.Connection.Update(repository, entity, fieldExpression, null, transaction) > 0))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 数值字段递增
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="value"></param>
        /// <param name="fieldExpression"></param>
        /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>Whether the command is executed successfully.</returns>
        public static bool Incr<TEntity, TValue>(this IDbConnection connection, IBaseRepository repository, TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null) where TValue : struct
        {
            return repository.GetSqlForIncr(value, fieldExpression, whereExpression).Execute(connection, transaction, repository, repository.CommandTimeout) > 0;
        }
        /// <summary>
        /// 数值字段递减
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="value"></param>
        /// <param name="fieldExpression"></param>
        /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>Whether the command is executed successfully.</returns>
        public static bool Decr<TEntity, TValue>(this IDbConnection connection, IBaseRepository repository, TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null) where TValue : struct
        {
            return repository.GetSqlForDecr(value, fieldExpression, whereExpression).Execute(connection, transaction, repository, repository.CommandTimeout) > 0;
        }

        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
        /// <param name="fieldExpression">The fields to query. If the value is null, all fields of the entity are queried. Example: 
        /// <para>1. Single field: entity => entity.Status</para>
        /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="orderBy">SQL ORDER BY condition.</param>
        /// <param name="pageIndex">The current page index for paging query, the minimum value is 1.</param>
        /// <param name="pageSize">The page size for paging query.</param>
        /// <returns></returns>
        public static IEnumerable<TEntity> Query<TEntity>(this IDbConnection connection, IBaseRepository repository, Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? pageIndex = null, int? pageSize = null, Expression<Func<TEntity, object>> fieldExpression = null)
        {
            return repository.GetSqlForQuery(whereExpression, orderBy, pageIndex, pageSize, fieldExpression).Query<TEntity>(connection, null, repository, repository.CommandTimeout);
        }
        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
        /// <param name="fieldExpression">The fields to query. If the value is null, all fields of the entity are queried. Example: 
        /// <para>1. Single field: entity => entity.Status</para>
        /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="orderBy">SQL ORDER BY condition.</param>
        /// <param name="offset">Offset to use for this query.</param>
        /// <param name="rows">The number of rows queried.</param>
        /// <returns></returns>
        public static IEnumerable<TEntity> QueryOffset<TEntity>(this IDbConnection connection, IBaseRepository repository, Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? offset = null, int? rows = null, Expression<Func<TEntity, object>> fieldExpression = null)
        {
            return repository.GetSqlForQueryOffset(whereExpression, orderBy, offset, rows, fieldExpression).Query<TEntity>(connection, null, repository, repository.CommandTimeout);
        }

        /// <summary>
        /// 查询单个数据
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
        /// <param name="fieldExpression">The fields to query. If the value is null, all fields of the entity are queried. Example: 
        /// <para>1. Single field: entity => entity.Status</para>
        /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <returns></returns>
        public static TEntity Get<TEntity>(this IDbConnection connection, IBaseRepository repository, Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, object>> fieldExpression = null)
        {
            return repository.GetSqlForGet(whereExpression, fieldExpression).QueryFirstOrDefault<TEntity>(connection, null, repository, repository.CommandTimeout);
        }

        /// <summary>
        /// 统计数量
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
        /// <returns></returns>
        public static int Count<TEntity>(this IDbConnection connection, IBaseRepository repository, Expression<Func<TEntity, bool>> whereExpression)
        {
            return repository.GetSqlForCount(whereExpression).ExecuteScalar<int>(connection, null, repository, repository.CommandTimeout);
        }

        /// <summary>
        /// Whether the specified table exists.
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="tableName">The table name.</param>
        /// <returns></returns>
        public static bool IsTableExists(this IDbConnection connection, IBaseRepository repository, string tableName, bool useCache = true)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return false;
            }

            if (useCache && TableInfoCache.Exists(tableName))
            {
                return true;
            }

            ISqlWithParameter sql = new DefaultSqlWithParameter
            {
                Sql = SqlUtil.GetSqlForCountTable(repository.DbType, connection.Database, tableName)
            };
            var exist = sql.ExecuteScalar<int>(connection, null, repository, repository.CommandTimeout) > 0;

            if (useCache && exist)
            {
                TableInfoCache.Add(tableName);
            }
            return exist;
        }

        public static bool IsTableFieldExists(this IDbConnection connection, IBaseRepository repository, string tableName, string fieldName, bool useCache = true)
        {
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            if (useCache && TableInfoCache.Exists(tableName, fieldName))
            {
                return true;
            }

            ISqlWithParameter sql = new DefaultSqlWithParameter
            {
                Sql = SqlUtil.GetSqlForCountTableField(repository.DbType, connection.Database, tableName, fieldName)
            };
            var exist = sql.ExecuteScalar<int>(connection, null, repository, repository.CommandTimeout) > 0;

            if (useCache && exist)
            {
                TableInfoCache.Add(tableName, fieldName);
            }
            return exist;
        }
        #endregion

        #region Asynchronous method
#if NETSTANDARD || NET45_OR_GREATER
        /// <summary>
        /// 新增数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="entity"></param>
        /// <param name="returnAutoIncrementId">Whether to return the auto-increment primary key Id.</param>
        /// <param name="fieldExpression">The fields to insert. If the value is null, all fields of the entity are inserted (excluding NotMapped fields). Example: 
        /// <para>1. Single field: entity => entity.Status</para>
        /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>Whether the command is executed successfully.</returns>
        public static async Task<bool> AddAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, TEntity entity, bool returnAutoIncrementId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
        {
            if (entity == null) return false;

            PropertyInfo keyIdentityProperty;
            if (returnAutoIncrementId && (keyIdentityProperty = typeof(TEntity).GetKeyIdentityProperty()) != null)
            {
                var id = await repository.GetSqlForAdd(entity, true, fieldExpression).ExecuteScalarAsync<long>(connection, transaction, repository, repository.CommandTimeout);
                if (id < 1) return false;

                keyIdentityProperty.SetValue(entity, id, null);
                return true;
            }

            return await repository.GetSqlForAdd(entity, false, fieldExpression).ExecuteAsync(connection, transaction, repository, repository.CommandTimeout) > 0;
        }

        /// <summary>
        /// 批量新增数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="entities"></param>
        /// <param name="returnAutoIncrementId">Whether to return the auto-increment primary key Id.</param>
        /// <param name="fieldExpression">The fields to insert. If the value is null, all fields of the entity are inserted (excluding NotMapped fields). Example: 
        /// <para>1. Single field: entity => entity.Status</para>
        /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>Whether the command is executed successfully.</returns>
        public static async Task<bool> BulkAddAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, IEnumerable<TEntity> entities, bool returnAutoIncrementId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
        {
            if (entities == null || !entities.Any()) return false;

            if (returnAutoIncrementId && typeof(TEntity).GetKeyIdentityProperty() != null)
            {
                if (transaction?.Connection == null)
                {
                    return await repository.ExecuteAutoTransactionAsync(connection, async trans =>
                    {
                        foreach (var entity in entities)
                        {
                            if (!await connection.AddAsync(repository, entity, returnAutoIncrementId, fieldExpression, trans))
                            {
                                return false;
                            }
                        }

                        return true;
                    });
                }

                foreach (var entity in entities)
                {
                    if (!await transaction.Connection.AddAsync(repository, entity, returnAutoIncrementId, fieldExpression, transaction))
                    {
                        return false;
                    }
                }

                return true;
            }

            ISqlWithParameter sql = repository.CreateInsertableBuilder<TEntity>(fieldExpression == null)
                .IncludeFields(fieldExpression)
                //.ReturnAutoIncrementId(returnAutoIncrementId)
                .SetParameter(entities)// BulkInsert
                .Build();
            return await sql.ExecuteAsync(connection, transaction, repository, repository.CommandTimeout) > 0;
        }

        /// <summary>
        /// 新增或更新数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="entity"></param>
        /// <param name="fieldExpression">The fields to insert or update. If the value is null, all fields of the entity are inserted or updated (excluding NotMapped fields). Example: 
        /// <para>1. Single field: entity => entity.Status</para>
        /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>Whether the command is executed successfully.</returns>
        public static async Task<bool> AddOrUpdateAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
        {
            if (entity == null) return false;

            switch (repository.DbType)
            {
                case DatabaseType.MySql:
                case DatabaseType.SQLite:
                    {
                        return await repository.GetSqlForAddOrUpdate(entity, fieldExpression).ExecuteAsync(connection, transaction, repository, repository.CommandTimeout) > 0;
                    }
                default:
                    {
                        if (await repository.GetSqlForEntityExists(entity).ExecuteScalarAsync<int>(connection, null, repository, repository.CommandTimeout) < 1)
                        {
                            // INSERT
                            return await connection.AddAsync(repository, entity, false, fieldExpression, transaction);
                        }

                        if (transaction?.Connection != null)
                        {
                            // DELETE
                            if (!await transaction.Connection.DeleteAsync(repository, entity, transaction))
                            {
                                return false;
                            }

                            // INSERT
                            return await transaction.Connection.AddAsync(repository, entity, false, fieldExpression, transaction);
                        }

                        return await repository.ExecuteAutoTransactionAsync(connection, async trans =>
                        {
                            // DELETE
                            if (!await connection.DeleteAsync(repository, entity, trans))
                            {
                                return false;
                            }

                            // INSERT
                            if (!await connection.AddAsync(repository, entity, false, fieldExpression, trans))
                            {
                                return false;
                            }

                            return true;
                        });
                    }
            }
        }

        /// <summary>
        /// 批量新增或更新数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="entities"></param>
        /// <param name="fieldExpression">The fields to insert or update. If the value is null, all fields of the entity are inserted or updated (excluding NotMapped fields). Example: 
        /// <para>1. Single field: entity => entity.Status</para>
        /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>Whether the command is executed successfully.</returns>
        public static async Task<bool> BulkAddOrUpdateAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
        {
            if (entities == null || !entities.Any()) return false;

            switch (repository.DbType)
            {
                case DatabaseType.MySql:
                case DatabaseType.SQLite:
                    ISqlWithParameter sql = repository.CreateReplaceableBuilder<TEntity>(fieldExpression == null)
                        .IncludeFields(fieldExpression)
                        .SetParameter(entities)
                        .Build();
                    return await sql.ExecuteAsync(connection, transaction, repository, repository.CommandTimeout) > 0;
                default:
                    if (transaction?.Connection == null)
                    {
                        return await repository.ExecuteAutoTransactionAsync(connection, async trans =>
                        {
                            foreach (var entity in entities)
                            {
                                if (!await connection.AddOrUpdateAsync(repository, entity, fieldExpression, trans))
                                {
                                    return false;
                                }
                            }

                            return true;
                        });
                    }

                    foreach (var entity in entities)
                    {
                        if (!await transaction.Connection.AddOrUpdateAsync(repository, entity, fieldExpression, transaction))
                        {
                            return false;
                        }
                    }

                    return true;
            }
        }

        /// <summary>
        /// 删除数据
        /// <para>Delete by primary key.</para>
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="entity"></param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>Whether the command is executed successfully.</returns>
        public static async Task<bool> DeleteAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, TEntity entity, IDbTransaction transaction = null)
        {
            return await repository.GetSqlForDelete(entity).ExecuteAsync(connection, transaction, repository, repository.CommandTimeout) > 0;
        }
        /// <summary>
        /// 删除数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>The number of rows affected.</returns>
        public static async Task<int> DeleteAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null)
        {
            return await repository.GetSqlForDelete(whereExpression).ExecuteAsync(connection, transaction, repository, repository.CommandTimeout);
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="entity"></param>
        /// <param name="fieldExpression">The fields to update. If the value is null, all fields of the entity are updated (excluding primary key fields and NotMapped fields). Example: 
        /// <para>1. Single field: entity => entity.Status</para>
        /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition. If the value is null, the SQL WHERE condition will default to the primary key field of the entity.
        /// <para>1. If the entity does not contain primary key field and the value is null, an exception will be thrown (to prevent all data in the table from being incorrectly updated).</para>
        /// <para>2. Code example for update all data in the table: entity => true.</para>
        /// </param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>The number of rows affected.</returns>
        public static async Task<int> UpdateAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, Expression<Func<TEntity, bool>> whereExpression = null, IDbTransaction transaction = null)
        {
            return await repository.GetSqlForUpdate(entity, fieldExpression, whereExpression).ExecuteAsync(connection, transaction, repository, repository.CommandTimeout);
        }

        /// <summary>
        /// 批量更新数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="entities"></param>
        /// <param name="fieldExpression">The fields to update. If the value is null, all fields of the entity are updated (excluding primary key fields and NotMapped fields). Example: 
        /// <para>1. Single field: entity => entity.Status</para>
        /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>Whether the command is executed successfully.</returns>
        public static async Task<bool> BulkUpdateAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
        {
            if (entities == null || !entities.Any()) return false;

            if (transaction?.Connection == null)
            {
                return await repository.ExecuteAutoTransactionAsync(connection, async trans =>
                {
                    foreach (var entity in entities)
                    {
                        if (!(await connection.UpdateAsync(repository, entity, fieldExpression, null, trans) > 0))
                        {
                            return false;
                        }
                    }

                    return true;
                });
            }

            foreach (var entity in entities)
            {
                if (!(await transaction.Connection.UpdateAsync(repository, entity, fieldExpression, null, transaction) > 0))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 数值字段递增
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="value"></param>
        /// <param name="fieldExpression"></param>
        /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>Whether the command is executed successfully.</returns>
        public static async Task<bool> IncrAsync<TEntity, TValue>(this IDbConnection connection, IBaseRepository repository, TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null) where TValue : struct
        {
            return await repository.GetSqlForIncr(value, fieldExpression, whereExpression).ExecuteAsync(connection, transaction, repository, repository.CommandTimeout) > 0;
        }
        /// <summary>
        /// 数值字段递减
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="value"></param>
        /// <param name="fieldExpression"></param>
        /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <returns>Whether the command is executed successfully.</returns>
        public static async Task<bool> DecrAsync<TEntity, TValue>(this IDbConnection connection, IBaseRepository repository, TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null) where TValue : struct
        {
            return await repository.GetSqlForDecr(value, fieldExpression, whereExpression).ExecuteAsync(connection, transaction, repository, repository.CommandTimeout) > 0;
        }

        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
        /// <param name="fieldExpression">The fields to query. If the value is null, all fields of the entity are queried. Example: 
        /// <para>1. Single field: entity => entity.Status</para>
        /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="orderBy">SQL ORDER BY condition.</param>
        /// <param name="pageIndex">The current page index for paging query, the minimum value is 1.</param>
        /// <param name="pageSize">The page size for paging query.</param>
        /// <returns></returns>
        public static async Task<IEnumerable<TEntity>> QueryAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? pageIndex = null, int? pageSize = null, Expression<Func<TEntity, object>> fieldExpression = null)
        {
            return await repository.GetSqlForQuery(whereExpression, orderBy, pageIndex, pageSize, fieldExpression).QueryAsync<TEntity>(connection, null, repository, repository.CommandTimeout);
        }
        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
        /// <param name="fieldExpression">The fields to query. If the value is null, all fields of the entity are queried. Example: 
        /// <para>1. Single field: entity => entity.Status</para>
        /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="orderBy">SQL ORDER BY condition.</param>
        /// <param name="offset">Offset to use for this query.</param>
        /// <param name="rows">The number of rows queried.</param>
        /// <returns></returns>
        public static async Task<IEnumerable<TEntity>> QueryOffsetAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? offset = null, int? rows = null, Expression<Func<TEntity, object>> fieldExpression = null)
        {
            return await repository.GetSqlForQueryOffset(whereExpression, orderBy, offset, rows, fieldExpression).QueryAsync<TEntity>(connection, null, repository, repository.CommandTimeout);
        }

        /// <summary>
        /// 查询单个数据
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
        /// <param name="fieldExpression">The fields to query. If the value is null, all fields of the entity are queried. Example: 
        /// <para>1. Single field: entity => entity.Status</para>
        /// <para>2. Multiple fields: entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <returns></returns>
        public static async Task<TEntity> GetAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, object>> fieldExpression = null)
        {
            return await repository.GetSqlForGet(whereExpression, fieldExpression).QueryFirstOrDefaultAsync<TEntity>(connection, null, repository, repository.CommandTimeout);
        }

        /// <summary>
        /// 统计数量
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
        /// <returns></returns>
        public static async Task<int> CountAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, Expression<Func<TEntity, bool>> whereExpression)
        {
            return await repository.GetSqlForCount(whereExpression).ExecuteScalarAsync<int>(connection, null, repository, repository.CommandTimeout);
        }

        /// <summary>
        /// Whether the specified table exists.
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="tableName">The table name.</param>
        /// <returns></returns>
        public static async Task<bool> IsTableExistsAsync(this IDbConnection connection, IBaseRepository repository, string tableName, bool useCache = true)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return false;
            }

            if (useCache && TableInfoCache.Exists(tableName))
            {
                return true;
            }

            ISqlWithParameter sql = new DefaultSqlWithParameter
            {
                Sql = SqlUtil.GetSqlForCountTable(repository.DbType, connection.Database, tableName)
            };
            var exist = await sql.ExecuteScalarAsync<int>(connection, null, repository, repository.CommandTimeout) > 0;

            if (useCache && exist)
            {
                TableInfoCache.Add(tableName);
            }
            return exist;
        }

        public static async Task<bool> IsTableFieldExistsAsync(this IDbConnection connection, IBaseRepository repository, string tableName, string fieldName, bool useCache = true)
        {
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            if (useCache && TableInfoCache.Exists(tableName, fieldName))
            {
                return true;
            }

            ISqlWithParameter sql = new DefaultSqlWithParameter
            {
                Sql = SqlUtil.GetSqlForCountTableField(repository.DbType, connection.Database, tableName, fieldName)
            };
            var exist = await sql.ExecuteScalarAsync<int>(connection, null, repository, repository.CommandTimeout) > 0;

            if (useCache && exist)
            {
                TableInfoCache.Add(tableName, fieldName);
            }
            return exist;
        }
#endif
        #endregion
    }
}
