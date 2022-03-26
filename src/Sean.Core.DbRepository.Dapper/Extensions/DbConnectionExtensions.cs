using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Dapper;
using Sean.Core.DbRepository.Extensions;

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
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="entity"></param>
        /// <param name="returnId">是否返回自增主键Id</param>
        /// <param name="fieldExpression">指定 INSERT 的字段。如果值为null，实体所有字段都会 INSERT（不包含自增字段和忽略字段）。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static bool Add<TEntity>(this IDbConnection connection, IBaseRepository repository, TEntity entity, bool returnId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            if (entity == null) return false;

            IInsertableSql sqlFactory = SqlFactory<TEntity>.Create(repository, fieldExpression == null)
                .IncludeFields(fieldExpression)
                .ReturnLastInsertId(returnId)
                .SetParameter(entity);
            PropertyInfo keyIdentityProperty;
            if (returnId && (keyIdentityProperty = typeof(TEntity).GetKeyIdentityProperty()) != null)
            {
                var sql = sqlFactory.InsertSql;
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
                return sqlFactory.ExecuteInsertSql(connection, transaction, commandTimeout, repository.OutputExecutedSql);
            }
        }
        /// <summary>
        /// 新增数据
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="sqlFactory"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static bool Add(this IDbConnection connection, IBaseRepository repository, IInsertableSql sqlFactory, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return sqlFactory.ExecuteInsertSql(connection, transaction, commandTimeout, repository.OutputExecutedSql);
        }

        /// <summary>
        /// 批量新增数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="entities"></param>
        /// <param name="returnId">是否返回自增主键Id</param>
        /// <param name="fieldExpression">指定 INSERT 的字段。如果值为null，实体所有字段都会 INSERT（不包含自增字段和忽略字段）。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static bool BulkAdd<TEntity>(this IDbConnection connection, IBaseRepository repository, IEnumerable<TEntity> entities, bool returnId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            if (entities == null || !entities.Any()) return false;

            if (returnId && typeof(TEntity).GetKeyIdentityProperty() != null)
            {
                if (transaction?.Connection == null)
                {
                    return repository.ExecuteTransaction(connection, trans =>
                    {
                        foreach (var entity in entities)
                        {
                            if (!connection.Add(repository, entity, returnId, fieldExpression, trans, commandTimeout))
                            {
                                trans.Rollback();
                                return false;
                            }
                        }

                        trans.Commit();
                        return true;
                    });
                }
                else
                {
                    foreach (var entity in entities)
                    {
                        if (!transaction.Connection.Add(repository, entity, returnId, fieldExpression, transaction, commandTimeout))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }
            else
            {
                IInsertableSql sqlFactory = SqlFactory<TEntity>.Create(repository, fieldExpression == null)
                    .IncludeFields(fieldExpression)
                    .ReturnLastInsertId(returnId)
                    .SetParameter(entities);
                return sqlFactory.ExecuteInsertSql(connection, transaction, commandTimeout, repository.OutputExecutedSql);
            }
        }

        /// <summary>
        /// 删除数据
        /// <para>Delete by primary key.</para>
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
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            IDeleteableSql sqlFactory = SqlFactory<TEntity>.Create(repository, false).SetParameter(entity);
            return connection.Delete(repository, sqlFactory, transaction, commandTimeout) > 0;
        }
        /// <summary>
        /// 删除数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="whereExpression">WHERE过滤条件</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static int Delete<TEntity>(this IDbConnection connection, IBaseRepository repository, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            IDeleteableSql sqlFactory = SqlFactory<TEntity>.Create(repository, false)
                .Where(whereExpression);
            return sqlFactory.ExecuteDeleteSql(connection, transaction, commandTimeout, repository.OutputExecutedSql);
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
        public static int Delete(this IDbConnection connection, IBaseRepository repository, IDeleteableSql sqlFactory, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return sqlFactory.ExecuteDeleteSql(connection, transaction, commandTimeout, repository.OutputExecutedSql);
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="entity">实体</param>
        /// <param name="fieldExpression">指定需要更新的字段。如果值为null，实体所有字段都会更新（不包含自增字段和忽略字段）。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="whereExpression">WHERE过滤条件。如果值为null，默认的过滤条件是实体的主键字段。
        /// <para>注：</para>
        /// <para>1. 如果实体没有主键字段，则必须设置过滤条件，否则会抛出异常（防止错误更新全表数据）。</para>
        /// <para>2. 如果需要更新全表数据，可以设置为：entity => true</para>
        /// </param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static int Update<TEntity>(this IDbConnection connection, IBaseRepository repository, TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, Expression<Func<TEntity, bool>> whereExpression = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            IUpdateableSql sqlFactory = SqlFactory<TEntity>.Create(repository, fieldExpression == null)
                .IncludeFields(fieldExpression, entity)
                .Where(whereExpression);
            return connection.Update(repository, sqlFactory, transaction, commandTimeout);
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
        public static int Update(this IDbConnection connection, IBaseRepository repository, IUpdateableSql sqlFactory, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return sqlFactory.ExecuteUpdateSql(connection, transaction, commandTimeout, repository.OutputExecutedSql);
        }

        /// <summary>
        /// 批量更新数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="entities">实体</param>
        /// <param name="fieldExpression">指定需要更新的字段。如果值为null，实体所有字段都会更新（不包含自增字段和忽略字段）。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="whereExpression">WHERE过滤条件。如果值为null，默认的过滤条件是实体的主键字段。
        /// <para>注：</para>
        /// <para>1. 如果实体没有主键字段，则必须设置过滤条件，否则会抛出异常（防止错误更新全表数据）。</para>
        /// <para>2. 如果需要更新全表数据，可以设置为：entity => true</para>
        /// </param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static bool BulkUpdate<TEntity>(this IDbConnection connection, IBaseRepository repository, IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, Expression<Func<TEntity, bool>> whereExpression = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            if (entities == null || !entities.Any()) return false;

            if (transaction?.Connection == null)
            {
                return repository.ExecuteTransaction(connection, trans =>
                {
                    foreach (var entity in entities)
                    {
                        if (!(connection.Update(repository, entity, fieldExpression, whereExpression, trans, commandTimeout) > 0))
                        {
                            trans.Rollback();
                            return false;
                        }
                    }

                    trans.Commit();
                    return true;
                });
            }
            else
            {
                foreach (var entity in entities)
                {
                    if (!(transaction.Connection.Update(repository, entity, fieldExpression, whereExpression, transaction, commandTimeout) > 0))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// 数值字段递增
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="value"></param>
        /// <param name="fieldExpression"></param>
        /// <param name="whereExpression"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public static bool Incr<TEntity, TValue>(this IDbConnection connection, IBaseRepository repository, TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null, int? commandTimeout = null) where TValue : struct
        {
            IUpdateableSql sqlFactory = SqlFactory<TEntity>.Create(repository, false)
                .IncludeFields(fieldExpression)
                .SetFieldCustomHandler((fieldName, adapter) => $"{adapter.FormatFieldName(fieldName)} = {adapter.FormatFieldName(fieldName)} + {value}")
                .Where(whereExpression);
            return connection.Update(repository, sqlFactory, transaction, commandTimeout) > 0;
        }
        /// <summary>
        /// 数值字段递减
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="value"></param>
        /// <param name="fieldExpression"></param>
        /// <param name="whereExpression"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public static bool Decr<TEntity, TValue>(this IDbConnection connection, IBaseRepository repository, TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null, int? commandTimeout = null) where TValue : struct
        {
            IUpdateableSql sqlFactory = SqlFactory<TEntity>.Create(repository, false)
                .IncludeFields(fieldExpression)
                .SetFieldCustomHandler((fieldName, adapter) => $"{adapter.FormatFieldName(fieldName)} = {adapter.FormatFieldName(fieldName)} - {value}")
                .Where(whereExpression);
            return connection.Update(repository, sqlFactory, transaction, commandTimeout) > 0;
        }

        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="sqlFactory"></param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static IEnumerable<TEntity> Query<TEntity>(this IDbConnection connection, IBaseRepository repository, IQueryableSql sqlFactory, int? commandTimeout = null)
        {
            return sqlFactory.ExecuteQuerySql<TEntity>(connection, commandTimeout, repository.OutputExecutedSql);
        }
        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="whereExpression">WHERE过滤条件</param>
        /// <param name="fieldExpression">指定需要返回的字段。如果值为null，默认会返回所有实体字段。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="orderByCondition">排序条件</param>
        /// <param name="pageIndex">分页参数：当前页号（最小值为1）</param>
        /// <param name="pageSize">分页参数：页大小</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static IEnumerable<TEntity> Query<TEntity>(this IDbConnection connection, IBaseRepository repository, Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderByCondition = null, int? pageIndex = null, int? pageSize = null, Expression<Func<TEntity, object>> fieldExpression = null, int? commandTimeout = null)
        {
            IQueryableSql sqlFactory = SqlFactory<TEntity>.Create(repository, fieldExpression == null)
                .IncludeFields(fieldExpression)
                .Where(whereExpression)
                .OrderBy(orderByCondition)
                .Page(pageIndex, pageSize);
            return sqlFactory.ExecuteQuerySql<TEntity>(connection, commandTimeout, repository.OutputExecutedSql);
        }
        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="whereExpression">WHERE过滤条件</param>
        /// <param name="fieldExpression">指定需要返回的字段。如果值为null，默认会返回所有实体字段。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="orderByCondition">排序条件</param>
        /// <param name="offset">偏移量</param>
        /// <param name="rows">行数</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static IEnumerable<TEntity> QueryOffset<TEntity>(this IDbConnection connection, IBaseRepository repository, Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderByCondition = null, int? offset = null, int? rows = null, Expression<Func<TEntity, object>> fieldExpression = null, int? commandTimeout = null)
        {
            IQueryableSql sqlFactory = SqlFactory<TEntity>.Create(repository, fieldExpression == null)
                .IncludeFields(fieldExpression)
                .Where(whereExpression)
                .OrderBy(orderByCondition)
                .Offset(offset, rows);
            return sqlFactory.ExecuteQuerySql<TEntity>(connection, commandTimeout, repository.OutputExecutedSql);
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
        public static TEntity Get<TEntity>(this IDbConnection connection, IBaseRepository repository, IQueryableSql sqlFactory, bool singleCheck = false, int? commandTimeout = null)
        {
            return sqlFactory.ExecuteQuerySingleSql<TEntity>(connection, singleCheck, commandTimeout, repository.OutputExecutedSql);
        }
        /// <summary>
        /// 查询单个数据
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="whereExpression">WHERE过滤条件</param>
        /// <param name="fieldExpression">指定需要返回的字段。如果值为null，默认会返回所有实体字段。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="singleCheck">是否执行单一结果检查。true：如果查询到多个结果会抛出异常，false：默认取第一个结果或默认值</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static TEntity Get<TEntity>(this IDbConnection connection, IBaseRepository repository, Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, object>> fieldExpression = null, bool singleCheck = false, int? commandTimeout = null)
        {
            IQueryableSql sqlFactory = SqlFactory<TEntity>.Create(repository, fieldExpression == null)
                .IncludeFields(fieldExpression)
                .Where(whereExpression);
            return sqlFactory.ExecuteQuerySingleSql<TEntity>(connection, singleCheck, commandTimeout, repository.OutputExecutedSql);
        }

        /// <summary>
        /// 统计数量
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="sqlFactory"></param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static int Count(this IDbConnection connection, IBaseRepository repository, ICountableSql sqlFactory, int? commandTimeout = null)
        {
            return sqlFactory.ExecuteCountSql(connection, commandTimeout, repository.OutputExecutedSql);
        }
        /// <summary>
        /// 统计数量
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="whereExpression">WHERE过滤条件</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static int Count<TEntity>(this IDbConnection connection, IBaseRepository repository, Expression<Func<TEntity, bool>> whereExpression, int? commandTimeout = null)
        {
            ICountableSql sqlFactory = SqlFactory<TEntity>.Create(repository, false)
                .Where(whereExpression);
            return sqlFactory.ExecuteCountSql(connection, commandTimeout, repository.OutputExecutedSql);
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

            var dbType = repository.DbType;
            var dbName = connection.Database;
            var sql = dbType.GetSqlForIsTableExists(dbName, tableName);
            var result = connection.QueryFirstOrDefault<int>(sql);
            repository.OutputExecutedSql(sql, null);
            return result > 0;
        }
        #endregion

        #region Asynchronous method
#if NETSTANDARD || NET45_OR_GREATER
        /// <summary>
        /// 新增数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="entity"></param>
        /// <param name="returnId">是否返回自增主键Id</param>
        /// <param name="fieldExpression">指定 INSERT 的字段。如果值为null，实体所有字段都会 INSERT（不包含自增字段和忽略字段）。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<bool> AddAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, TEntity entity, bool returnId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            if (entity == null) return false;

            IInsertableSql sqlFactory = SqlFactory<TEntity>.Create(repository, fieldExpression == null)
                .IncludeFields(fieldExpression)
                .ReturnLastInsertId(returnId)
                .SetParameter(entity);
            PropertyInfo keyIdentityProperty;
            if (returnId && (keyIdentityProperty = typeof(TEntity).GetKeyIdentityProperty()) != null)
            {
                var sql = sqlFactory.InsertSql;
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
                return await sqlFactory.ExecuteInsertSqlAsync(connection, transaction, commandTimeout, repository.OutputExecutedSql);
            }
        }
        /// <summary>
        /// 新增数据
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="sqlFactory"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<bool> AddAsync(this IDbConnection connection, IBaseRepository repository, IInsertableSql sqlFactory, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return await sqlFactory.ExecuteInsertSqlAsync(connection, transaction, commandTimeout, repository.OutputExecutedSql);
        }

        /// <summary>
        /// 批量新增数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="entities"></param>
        /// <param name="returnId">是否返回自增主键Id</param>
        /// <param name="fieldExpression">指定 INSERT 的字段。如果值为null，实体所有字段都会 INSERT（不包含自增字段和忽略字段）。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<bool> BulkAddAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, IEnumerable<TEntity> entities, bool returnId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            if (entities == null || !entities.Any()) return false;

            if (returnId && typeof(TEntity).GetKeyIdentityProperty() != null)
            {
                if (transaction?.Connection == null)
                {
                    return await repository.ExecuteTransactionAsync(connection, async trans =>
                    {
                        foreach (var entity in entities)
                        {
                            if (!await connection.AddAsync(repository, entity, returnId, fieldExpression, trans, commandTimeout))
                            {
                                trans.Rollback();
                                return false;
                            }
                        }

                        trans.Commit();
                        return true;
                    });
                }
                else
                {
                    foreach (var entity in entities)
                    {
                        if (!await transaction.Connection.AddAsync(repository, entity, returnId, fieldExpression, transaction, commandTimeout))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }
            else
            {
                IInsertableSql sqlFactory = SqlFactory<TEntity>.Create(repository, fieldExpression == null)
                    .IncludeFields(fieldExpression)
                    .ReturnLastInsertId(returnId)
                    .SetParameter(entities);
                return await sqlFactory.ExecuteInsertSqlAsync(connection, transaction, commandTimeout, repository.OutputExecutedSql);
            }
        }

        /// <summary>
        /// 删除数据
        /// <para>Delete by primary key.</para>
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
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            IDeleteableSql sqlFactory = SqlFactory<TEntity>.Create(repository, true).SetParameter(entity);
            return await connection.DeleteAsync(repository, sqlFactory, transaction, commandTimeout) > 0;
        }
        /// <summary>
        /// 删除数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="whereExpression">WHERE过滤条件</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<int> DeleteAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            IDeleteableSql sqlFactory = SqlFactory<TEntity>.Create(repository, false)
                .Where(whereExpression);
            return await sqlFactory.ExecuteDeleteSqlAsync(connection, transaction, commandTimeout, repository.OutputExecutedSql);
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
        public static async Task<int> DeleteAsync(this IDbConnection connection, IBaseRepository repository, IDeleteableSql sqlFactory, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return await sqlFactory.ExecuteDeleteSqlAsync(connection, transaction, commandTimeout, repository.OutputExecutedSql);
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="entity">实体</param>
        /// <param name="fieldExpression">指定需要更新的字段。如果值为null，实体所有字段都会更新（不包含自增字段和忽略字段）。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="whereExpression">WHERE过滤条件。如果值为null，默认的过滤条件是实体的主键字段。
        /// <para>注：</para>
        /// <para>1. 如果实体没有主键字段，则必须设置过滤条件，否则会抛出异常（防止错误更新全表数据）。</para>
        /// <para>2. 如果需要更新全表数据，可以设置为：entity => true</para>
        /// </param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<int> UpdateAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, Expression<Func<TEntity, bool>> whereExpression = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            IUpdateableSql sqlFactory = SqlFactory<TEntity>.Create(repository, fieldExpression == null)
                .IncludeFields(fieldExpression, entity)
                .Where(whereExpression);
            return await connection.UpdateAsync(repository, sqlFactory, transaction, commandTimeout);
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
        public static async Task<int> UpdateAsync(this IDbConnection connection, IBaseRepository repository, IUpdateableSql sqlFactory, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return await sqlFactory.ExecuteUpdateSqlAsync(connection, transaction, commandTimeout, repository.OutputExecutedSql);
        }

        /// <summary>
        /// 批量更新数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="entities">实体</param>
        /// <param name="fieldExpression">指定需要更新的字段。如果值为null，实体所有字段都会更新（不包含自增字段和忽略字段）。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="whereExpression">WHERE过滤条件。如果值为null，默认的过滤条件是实体的主键字段。
        /// <para>注：</para>
        /// <para>1. 如果实体没有主键字段，则必须设置过滤条件，否则会抛出异常（防止错误更新全表数据）。</para>
        /// <para>2. 如果需要更新全表数据，可以设置为：entity => true</para>
        /// </param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<bool> BulkUpdateAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, Expression<Func<TEntity, bool>> whereExpression = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            if (entities == null || !entities.Any()) return false;

            if (transaction?.Connection == null)
            {
                return await repository.ExecuteTransactionAsync(connection, async trans =>
                {
                    foreach (var entity in entities)
                    {
                        if (!(await connection.UpdateAsync(repository, entity, fieldExpression, whereExpression, trans, commandTimeout) > 0))
                        {
                            trans.Rollback();
                            return false;
                        }
                    }

                    trans.Commit();
                    return true;
                });
            }
            else
            {
                foreach (var entity in entities)
                {
                    if (!(await transaction.Connection.UpdateAsync(repository, entity, fieldExpression, whereExpression, transaction, commandTimeout) > 0))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// 数值字段递增
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="value"></param>
        /// <param name="fieldExpression"></param>
        /// <param name="whereExpression"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public static async Task<bool> IncrAsync<TEntity, TValue>(this IDbConnection connection, IBaseRepository repository, TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null, int? commandTimeout = null) where TValue : struct
        {
            IUpdateableSql sqlFactory = SqlFactory<TEntity>.Create(repository, false)
                .IncludeFields(fieldExpression)
                .SetFieldCustomHandler((fieldName, adapter) => $"{adapter.FormatFieldName(fieldName)} = {adapter.FormatFieldName(fieldName)} + {value}")
                .Where(whereExpression);
            return await connection.UpdateAsync(repository, sqlFactory, transaction, commandTimeout) > 0;
        }
        /// <summary>
        /// 数值字段递减
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="value"></param>
        /// <param name="fieldExpression"></param>
        /// <param name="whereExpression"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public static async Task<bool> DecrAsync<TEntity, TValue>(this IDbConnection connection, IBaseRepository repository, TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null, int? commandTimeout = null) where TValue : struct
        {
            IUpdateableSql sqlFactory = SqlFactory<TEntity>.Create(repository, false)
                .IncludeFields(fieldExpression)
                .SetFieldCustomHandler((fieldName, adapter) => $"{adapter.FormatFieldName(fieldName)} = {adapter.FormatFieldName(fieldName)} - {value}")
                .Where(whereExpression);
            return await connection.UpdateAsync(repository, sqlFactory, transaction, commandTimeout) > 0;
        }

        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="sqlFactory"></param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<IEnumerable<TEntity>> QueryAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, IQueryableSql sqlFactory, int? commandTimeout = null)
        {
            return await sqlFactory.ExecuteQuerySqlAsync<TEntity>(connection, commandTimeout, repository.OutputExecutedSql);
        }
        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="whereExpression">WHERE过滤条件</param>
        /// <param name="fieldExpression">指定需要返回的字段。如果值为null，默认会返回所有实体字段。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="orderByCondition">排序条件</param>
        /// <param name="pageIndex">分页参数：当前页号（最小值为1）</param>
        /// <param name="pageSize">分页参数：页大小</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<IEnumerable<TEntity>> QueryAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderByCondition = null, int? pageIndex = null, int? pageSize = null, Expression<Func<TEntity, object>> fieldExpression = null, int? commandTimeout = null)
        {
            IQueryableSql sqlFactory = SqlFactory<TEntity>.Create(repository, fieldExpression == null)
                .IncludeFields(fieldExpression)
                .Where(whereExpression)
                .OrderBy(orderByCondition)
                .Page(pageIndex, pageSize);
            return await sqlFactory.ExecuteQuerySqlAsync<TEntity>(connection, commandTimeout, repository.OutputExecutedSql);
        }
        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="whereExpression">WHERE过滤条件</param>
        /// <param name="fieldExpression">指定需要返回的字段。如果值为null，默认会返回所有实体字段。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="orderByCondition">排序条件</param>
        /// <param name="offset">偏移量</param>
        /// <param name="rows">行数</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<IEnumerable<TEntity>> QueryOffsetAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderByCondition = null, int? offset = null, int? rows = null, Expression<Func<TEntity, object>> fieldExpression = null, int? commandTimeout = null)
        {
            IQueryableSql sqlFactory = SqlFactory<TEntity>.Create(repository, fieldExpression == null)
                .IncludeFields(fieldExpression)
                .Where(whereExpression)
                .OrderBy(orderByCondition)
                .Offset(offset, rows);
            return await sqlFactory.ExecuteQuerySqlAsync<TEntity>(connection, commandTimeout, repository.OutputExecutedSql);
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
        public static async Task<TEntity> GetAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, IQueryableSql sqlFactory, bool singleCheck = false, int? commandTimeout = null)
        {
            return await sqlFactory.ExecuteQuerySingleSqlAsync<TEntity>(connection, singleCheck, commandTimeout, repository.OutputExecutedSql);
        }
        /// <summary>
        /// 查询单个数据
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="whereExpression">WHERE过滤条件</param>
        /// <param name="fieldExpression">指定需要返回的字段。如果值为null，默认会返回所有实体字段。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="singleCheck">是否执行单一结果检查。true：如果查询到多个结果会抛出异常，false：默认取第一个结果或默认值</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<TEntity> GetAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, object>> fieldExpression = null, bool singleCheck = false, int? commandTimeout = null)
        {
            IQueryableSql sqlFactory = SqlFactory<TEntity>.Create(repository, fieldExpression == null)
                .IncludeFields(fieldExpression)
                .Where(whereExpression);
            return await sqlFactory.ExecuteQuerySingleSqlAsync<TEntity>(connection, singleCheck, commandTimeout, repository.OutputExecutedSql);
        }

        /// <summary>
        /// 统计数量
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="sqlFactory"></param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<int> CountAsync(this IDbConnection connection, IBaseRepository repository, ICountableSql sqlFactory, int? commandTimeout = null)
        {
            return await sqlFactory.ExecuteCountSqlAsync(connection, commandTimeout, repository.OutputExecutedSql);
        }
        /// <summary>
        /// 统计数量
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="repository"></param>
        /// <param name="whereExpression">WHERE过滤条件</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<int> CountAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, Expression<Func<TEntity, bool>> whereExpression, int? commandTimeout = null)
        {
            ICountableSql sqlFactory = SqlFactory<TEntity>.Create(repository, false)
                .Where(whereExpression);
            return await sqlFactory.ExecuteCountSqlAsync(connection, commandTimeout, repository.OutputExecutedSql);
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

            var dbType = repository.DbType;
            var dbName = connection.Database;
            var sql = dbType.GetSqlForIsTableExists(dbName, tableName);
            var result = await connection.QueryFirstOrDefaultAsync<int>(sql);
            repository.OutputExecutedSql(sql, null);
            return result > 0;
        }
#endif
        #endregion
    }
}
