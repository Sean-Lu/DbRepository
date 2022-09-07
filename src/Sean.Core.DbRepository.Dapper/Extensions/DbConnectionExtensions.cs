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
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="entity"></param>
        /// <param name="returnId">是否返回自增主键Id</param>
        /// <param name="fieldExpression">指定 INSERT 的字段。如果值为null，实体所有字段都会 INSERT（不包含忽略字段）。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>是否执行成功</returns>
        public static bool Add<TEntity>(this IDbConnection connection, IBaseRepository repository, TEntity entity, bool returnId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            if (entity == null) return false;

            IInsertableSql insertableSql;

            PropertyInfo keyIdentityProperty;
            if (returnId && (keyIdentityProperty = typeof(TEntity).GetKeyIdentityProperty()) != null)
            {
                insertableSql = repository.CreateInsertableBuilder<TEntity>(fieldExpression == null)
                    .IncludeFields(fieldExpression)
                    .ReturnAutoIncrementId(returnId)
                    .SetParameter(entity)
                    .Build();
                var id = insertableSql.ExecuteScalar<long>(connection, transaction, repository, commandTimeout);
                if (id < 1) return false;

                keyIdentityProperty.SetValue(entity, id, null);
                return true;
            }

            insertableSql = repository.CreateInsertableBuilder<TEntity>(fieldExpression == null)
                .IncludeFields(fieldExpression)
                //.ReturnAutoIncrementId(returnId)
                .SetParameter(entity)
                .Build();
            return insertableSql.ExecuteCommand(connection, transaction, repository, commandTimeout) > 0;
        }
        /// <summary>
        /// 新增数据
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="sqlMonitor"></param>
        /// <param name="insertableSql"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>是否执行成功</returns>
        public static bool Add(this IDbConnection connection, IInsertableSql insertableSql, IDbTransaction transaction = null, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            return insertableSql.ExecuteCommand(connection, transaction, sqlMonitor, commandTimeout) > 0;
        }

        /// <summary>
        /// 批量新增数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="entities"></param>
        /// <param name="returnId">是否返回自增主键Id</param>
        /// <param name="fieldExpression">指定 INSERT 的字段。如果值为null，实体所有字段都会 INSERT（不包含忽略字段）。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>是否执行成功</returns>
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

            IInsertableSql insertableSql = repository.CreateInsertableBuilder<TEntity>(fieldExpression == null)
                .IncludeFields(fieldExpression)
                //.ReturnAutoIncrementId(returnId)
                .SetParameter(entities)// BulkInsert
                .Build();
            return insertableSql.ExecuteCommand(connection, transaction, repository, commandTimeout) > 0;
        }

        /// <summary>
        /// 新增或更新数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="entity"></param>
        /// <param name="fieldExpression">指定 INSERT OR UPDATE 的字段。如果值为null，实体所有字段都会 INSERT OR UPDATE（不包含忽略字段）。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>是否执行成功</returns>
        public static bool AddOrUpdate<TEntity>(this IDbConnection connection, IBaseRepository repository, TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            if (entity == null) return false;

            switch (repository.DbType)
            {
                case DatabaseType.MySql:
                case DatabaseType.SQLite:
                    IReplaceableSql replaceableSql = repository.CreateReplaceableBuilder<TEntity>(fieldExpression == null)
                        .IncludeFields(fieldExpression)
                        .SetParameter(entity)
                        .Build();
                    return replaceableSql.ExecuteCommand(connection, transaction, repository, commandTimeout) > 0;
                default:
                    var pkFields = typeof(TEntity).GetPrimaryKeys();
                    if (pkFields == null || !pkFields.Any()) throw new InvalidOperationException($"The entity '{typeof(TEntity).Name}' is missing a primary key field.");

                    ICountable<TEntity> countableBuilder = repository.CreateCountableBuilder<TEntity>();
                    pkFields.ForEach(pkField => countableBuilder.WhereField(entity1 => pkField, SqlOperation.Equal));
                    countableBuilder.SetParameter(entity);
                    ICountableSql countableSql = countableBuilder.Build();
                    var count = countableSql.ExecuteCommand(connection, repository, commandTimeout);
                    if (count < 1)
                    {
                        // INSERT
                        return connection.Add(repository, entity, false, fieldExpression, transaction, commandTimeout);
                    }
                    else
                    {
                        //// UPDATE
                        //IUpdateable<TEntity> updateableBuilder = repository.CreateUpdateableBuilder<TEntity>(fieldExpression == null)
                        //    .IncludeFields(fieldExpression, entity);
                        //pkFields.ForEach(pkField => updateableBuilder.WhereField(entity1 => pkField, SqlOperation.Equal));
                        //IUpdateableSql updateableSql = updateableBuilder.Build();
                        //return updateableSql.ExecuteCommand(connection, transaction, commandTimeout, repository.OutputExecutedSql) > 0;


                        if (transaction?.Connection != null)
                        {
                            // DELETE
                            if (!transaction.Connection.Delete(repository, entity, transaction, commandTimeout))
                            {
                                return false;
                            }

                            // INSERT
                            return transaction.Connection.Add(repository, entity, false, fieldExpression, transaction, commandTimeout);
                        }
                        else
                        {
                            return repository.ExecuteTransaction(connection, trans =>
                            {
                                // DELETE
                                if (!connection.Delete(repository, entity, trans, commandTimeout))
                                {
                                    trans.Rollback();
                                    return false;
                                }

                                // INSERT
                                if (!connection.Add(repository, entity, false, fieldExpression, trans, commandTimeout))
                                {
                                    trans.Rollback();
                                    return false;
                                }

                                trans.Commit();
                                return true;
                            });
                        }
                    }
            }
        }
        /// <summary>
        /// 新增或更新数据
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="sqlMonitor"></param>
        /// <param name="replaceableSql"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>是否执行成功</returns>
        public static bool AddOrUpdate(this IDbConnection connection, IReplaceableSql replaceableSql, IDbTransaction transaction = null, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            return replaceableSql.ExecuteCommand(connection, transaction, sqlMonitor, commandTimeout) > 0;
        }

        /// <summary>
        /// 批量新增或更新数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="entities"></param>
        /// <param name="fieldExpression">指定 INSERT OR UPDATE 的字段。如果值为null，实体所有字段都会 INSERT OR UPDATE（不包含忽略字段）。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>是否执行成功</returns>
        public static bool BulkAddOrUpdate<TEntity>(this IDbConnection connection, IBaseRepository repository, IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            if (entities == null || !entities.Any()) return false;

            switch (repository.DbType)
            {
                case DatabaseType.MySql:
                case DatabaseType.SQLite:
                    IReplaceableSql replaceableSql = repository.CreateReplaceableBuilder<TEntity>(fieldExpression == null)
                        .IncludeFields(fieldExpression)
                        .SetParameter(entities)
                        .Build();
                    return replaceableSql.ExecuteCommand(connection, transaction, repository, commandTimeout) > 0;
                default:
                    if (transaction?.Connection != null)
                    {
                        foreach (var entity in entities)
                        {
                            if (!transaction.Connection.AddOrUpdate(repository, entity, fieldExpression, transaction, commandTimeout))
                            {
                                return false;
                            }
                        }

                        return true;
                    }
                    else
                    {
                        return repository.ExecuteTransaction(connection, trans =>
                        {
                            foreach (var entity in entities)
                            {
                                if (!connection.AddOrUpdate(repository, entity, fieldExpression, trans, commandTimeout))
                                {
                                    trans.Rollback();
                                    return false;
                                }
                            }

                            trans.Commit();
                            return true;
                        });
                    }
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
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>是否执行成功</returns>
        public static bool Delete<TEntity>(this IDbConnection connection, IBaseRepository repository, TEntity entity, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            IDeleteableSql deleteableSql = repository.CreateDeleteableBuilder<TEntity>()
                .SetParameter(entity)
                .Build();
            return deleteableSql.ExecuteCommand(connection, transaction, repository, commandTimeout) > 0;
        }
        /// <summary>
        /// 删除数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="whereExpression">WHERE过滤条件</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>返回受影响的行数</returns>
        public static int Delete<TEntity>(this IDbConnection connection, IBaseRepository repository, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            IDeleteableSql deleteableSql = repository.CreateDeleteableBuilder<TEntity>()
                .Where(whereExpression)
                .Build();
            return deleteableSql.ExecuteCommand(connection, transaction, repository, commandTimeout);
        }
        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="sqlMonitor"></param>
        /// <param name="deleteableSql"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>返回受影响的行数</returns>
        public static int Delete(this IDbConnection connection, IDeleteableSql deleteableSql, IDbTransaction transaction = null, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            return deleteableSql.ExecuteCommand(connection, transaction, sqlMonitor, commandTimeout);
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="entity">实体</param>
        /// <param name="fieldExpression">指定 UPDATE 的字段。如果值为null，实体所有字段都会 UPDATE（不包含主键字段和忽略字段）。示例：
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
        /// <returns>返回受影响的行数</returns>
        public static int Update<TEntity>(this IDbConnection connection, IBaseRepository repository, TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, Expression<Func<TEntity, bool>> whereExpression = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            IUpdateableSql updateableSql = repository.CreateUpdateableBuilder<TEntity>(fieldExpression == null)
                .IncludeFields(fieldExpression, entity)
                .Where(whereExpression)
                .Build();
            return updateableSql.ExecuteCommand(connection, transaction, repository, commandTimeout);
        }
        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="sqlMonitor"></param>
        /// <param name="updateableSql"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>返回受影响的行数</returns>
        public static int Update(this IDbConnection connection, IUpdateableSql updateableSql, IDbTransaction transaction = null, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            return updateableSql.ExecuteCommand(connection, transaction, sqlMonitor, commandTimeout);
        }

        /// <summary>
        /// 批量更新数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="entities">实体</param>
        /// <param name="fieldExpression">指定 UPDATE 的字段。如果值为null，实体所有字段都会 UPDATE（不包含主键字段和忽略字段）。示例：
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
        /// <returns>是否执行成功</returns>
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
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="value"></param>
        /// <param name="fieldExpression"></param>
        /// <param name="whereExpression"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>是否执行成功</returns>
        public static bool Incr<TEntity, TValue>(this IDbConnection connection, IBaseRepository repository, TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null, int? commandTimeout = null) where TValue : struct
        {
            IUpdateableSql updateableSql = repository.CreateUpdateableBuilder<TEntity>(false)
                .IncrFields(fieldExpression, value)
                .Where(whereExpression)
                .Build();
            return updateableSql.ExecuteCommand(connection, transaction, repository, commandTimeout) > 0;
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
        /// <param name="whereExpression"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>是否执行成功</returns>
        public static bool Decr<TEntity, TValue>(this IDbConnection connection, IBaseRepository repository, TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null, int? commandTimeout = null) where TValue : struct
        {
            IUpdateableSql updateableSql = repository.CreateUpdateableBuilder<TEntity>(false)
                .DecrFields(fieldExpression, value)
                .Where(whereExpression)
                .Build();
            return updateableSql.ExecuteCommand(connection, transaction, repository, commandTimeout) > 0;
        }

        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="sqlMonitor"></param>
        /// <param name="queryableSql"></param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static IEnumerable<TEntity> Query<TEntity>(this IDbConnection connection, IQueryableSql queryableSql, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            return queryableSql.ExecuteCommand<TEntity>(connection, sqlMonitor, commandTimeout);
        }
        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="connection">Database connection</param>
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
            IQueryableSql queryableSql = repository.CreateQueryableBuilder<TEntity>(fieldExpression == null)
                .IncludeFields(fieldExpression)
                .Where(whereExpression)
                .OrderBy(orderByCondition)
                .Page(pageIndex, pageSize)
                .Build();
            return queryableSql.ExecuteCommand<TEntity>(connection, repository, commandTimeout);
        }
        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="connection">Database connection</param>
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
            IQueryableSql queryableSql = repository.CreateQueryableBuilder<TEntity>(fieldExpression == null)
                .IncludeFields(fieldExpression)
                .Where(whereExpression)
                .OrderBy(orderByCondition)
                .Offset(offset, rows)
                .Build();
            return queryableSql.ExecuteCommand<TEntity>(connection, repository, commandTimeout);
        }

        /// <summary>
        /// 查询单个数据
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="sqlMonitor"></param>
        /// <param name="queryableSql"></param>
        /// <param name="singleCheck">是否执行单一结果检查。true：如果查询到多个结果会抛出异常，false：默认取第一个结果或默认值</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static TEntity Get<TEntity>(this IDbConnection connection, IQueryableSql queryableSql, bool singleCheck = false, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            return queryableSql.ExecuteCommandSingleResult<TEntity>(connection, singleCheck, sqlMonitor, commandTimeout);
        }
        /// <summary>
        /// 查询单个数据
        /// </summary>
        /// <param name="connection">Database connection</param>
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
            IQueryableSql queryableSql = repository.CreateQueryableBuilder<TEntity>(fieldExpression == null)
                .IncludeFields(fieldExpression)
                .Where(whereExpression)
                .Build();
            return queryableSql.ExecuteCommandSingleResult<TEntity>(connection, singleCheck, repository, commandTimeout);
        }

        /// <summary>
        /// 统计数量
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="countableSql"></param>
        /// <param name="sqlMonitor"></param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static int Count(this IDbConnection connection, ICountableSql countableSql, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            return countableSql.ExecuteCommand(connection, sqlMonitor, commandTimeout);
        }
        /// <summary>
        /// 统计数量
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="whereExpression">WHERE过滤条件</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static int Count<TEntity>(this IDbConnection connection, IBaseRepository repository, Expression<Func<TEntity, bool>> whereExpression, int? commandTimeout = null)
        {
            ICountableSql countableSql = repository.CreateCountableBuilder<TEntity>()
                .Where(whereExpression)
                .Build();
            return countableSql.ExecuteCommand(connection, repository, commandTimeout);
        }

        /// <summary>
        /// 查询指定的表是否存在
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="tableName">表名称</param>
        /// <returns></returns>
        public static bool IsTableExists(this IDbConnection connection, IBaseRepository repository, string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return false;
            }

            var dbType = repository.DbType;
            var dbName = connection.Database;
            var sql = dbType.GetSqlForCountTable(dbName, tableName);
            IQueryableSql queryableSql = new DefaultQueryableSql
            {
                Sql = sql
            };
            return queryableSql.ExecuteCommandSingleResult<int>(connection, false, sqlMonitor: repository) > 0;
        }

        public static bool IsTableFieldExists(this IDbConnection connection, IBaseRepository repository, string tableName, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            var dbType = repository.DbType;
            var dbName = connection.Database;
            var sql = dbType.GetSqlForCountTableField(dbName, tableName, fieldName);
            IQueryableSql queryableSql = new DefaultQueryableSql
            {
                Sql = sql
            };
            return queryableSql.ExecuteCommandSingleResult<int>(connection, false, sqlMonitor: repository) > 0;
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
        /// <param name="returnId">是否返回自增主键Id</param>
        /// <param name="fieldExpression">指定 INSERT 的字段。如果值为null，实体所有字段都会 INSERT（不包含忽略字段）。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>是否执行成功</returns>
        public static async Task<bool> AddAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, TEntity entity, bool returnId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            if (entity == null) return false;

            IInsertableSql insertableSql;

            PropertyInfo keyIdentityProperty;
            if (returnId && (keyIdentityProperty = typeof(TEntity).GetKeyIdentityProperty()) != null)
            {
                insertableSql = repository.CreateInsertableBuilder<TEntity>(fieldExpression == null)
                    .IncludeFields(fieldExpression)
                    .ReturnAutoIncrementId(returnId)
                    .SetParameter(entity)
                    .Build();
                var id = await insertableSql.ExecuteScalarAsync<long>(connection, transaction, repository, commandTimeout);
                if (id < 1) return false;

                keyIdentityProperty.SetValue(entity, id, null);
                return true;
            }

            insertableSql = repository.CreateInsertableBuilder<TEntity>(fieldExpression == null)
                .IncludeFields(fieldExpression)
                //.ReturnAutoIncrementId(returnId)
                .SetParameter(entity)
                .Build();
            return await insertableSql.ExecuteCommandAsync(connection, transaction, repository, commandTimeout) > 0;
        }
        /// <summary>
        /// 新增数据
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="sqlMonitor"></param>
        /// <param name="insertableSql"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>是否执行成功</returns>
        public static async Task<bool> AddAsync(this IDbConnection connection, IInsertableSql insertableSql, IDbTransaction transaction = null, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            return await insertableSql.ExecuteCommandAsync(connection, transaction, sqlMonitor, commandTimeout) > 0;
        }

        /// <summary>
        /// 批量新增数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="entities"></param>
        /// <param name="returnId">是否返回自增主键Id</param>
        /// <param name="fieldExpression">指定 INSERT 的字段。如果值为null，实体所有字段都会 INSERT（不包含忽略字段）。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>是否执行成功</returns>
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

            IInsertableSql insertableSql = repository.CreateInsertableBuilder<TEntity>(fieldExpression == null)
                .IncludeFields(fieldExpression)
                //.ReturnAutoIncrementId(returnId)
                .SetParameter(entities)// BulkInsert
                .Build();
            return await insertableSql.ExecuteCommandAsync(connection, transaction, repository, commandTimeout) > 0;
        }

        /// <summary>
        /// 新增或更新数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="entity"></param>
        /// <param name="fieldExpression">指定 INSERT OR UPDATE 的字段。如果值为null，实体所有字段都会 INSERT OR UPDATE（不包含忽略字段）。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>是否执行成功</returns>
        public static async Task<bool> AddOrUpdateAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            if (entity == null) return false;

            switch (repository.DbType)
            {
                case DatabaseType.MySql:
                case DatabaseType.SQLite:
                    IReplaceableSql replaceableSql = repository.CreateReplaceableBuilder<TEntity>(fieldExpression == null)
                        .IncludeFields(fieldExpression)
                        .SetParameter(entity)
                        .Build();
                    return await replaceableSql.ExecuteCommandAsync(connection, transaction, repository, commandTimeout) > 0;
                default:
                    var pkFields = typeof(TEntity).GetPrimaryKeys();
                    if (pkFields == null || !pkFields.Any()) throw new InvalidOperationException($"The entity '{typeof(TEntity).Name}' is missing a primary key field.");

                    ICountable<TEntity> countableBuilder = repository.CreateCountableBuilder<TEntity>();
                    pkFields.ForEach(pkField => countableBuilder.WhereField(entity1 => pkField, SqlOperation.Equal));
                    countableBuilder.SetParameter(entity);
                    ICountableSql countableSql = countableBuilder.Build();
                    var count = await countableSql.ExecuteCommandAsync(connection, repository, commandTimeout);
                    if (count < 1)
                    {
                        // INSERT
                        return await connection.AddAsync(repository, entity, false, fieldExpression, transaction, commandTimeout);
                    }
                    else
                    {
                        //// UPDATE
                        //IUpdateable<TEntity> updateableBuilder = repository.CreateUpdateableBuilder<TEntity>(fieldExpression == null)
                        //    .IncludeFields(fieldExpression, entity);
                        //pkFields.ForEach(pkField => updateableBuilder.WhereField(entity1 => pkField, SqlOperation.Equal));
                        //IUpdateableSql updateableSql = updateableBuilder.Build();
                        //return await updateableSql.ExecuteCommandAsync(connection, transaction, commandTimeout, repository.OutputExecutedSql) > 0;


                        if (transaction?.Connection != null)
                        {
                            // DELETE
                            if (!await transaction.Connection.DeleteAsync(repository, entity, transaction, commandTimeout))
                            {
                                return false;
                            }

                            // INSERT
                            return await transaction.Connection.AddAsync(repository, entity, false, fieldExpression, transaction, commandTimeout);
                        }
                        else
                        {
                            return await repository.ExecuteTransactionAsync(connection, async trans =>
                            {
                                // DELETE
                                if (!await connection.DeleteAsync(repository, entity, trans, commandTimeout))
                                {
                                    trans.Rollback();
                                    return false;
                                }

                                // INSERT
                                if (!await connection.AddAsync(repository, entity, false, fieldExpression, trans, commandTimeout))
                                {
                                    trans.Rollback();
                                    return false;
                                }

                                trans.Commit();
                                return true;
                            });
                        }
                    }
            }
        }
        /// <summary>
        /// 新增或更新数据
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="sqlMonitor"></param>
        /// <param name="replaceableSql"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>是否执行成功</returns>
        public static async Task<bool> AddOrUpdateAsync(this IDbConnection connection, IReplaceableSql replaceableSql, IDbTransaction transaction = null, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            return await replaceableSql.ExecuteCommandAsync(connection, transaction, sqlMonitor, commandTimeout) > 0;
        }

        /// <summary>
        /// 批量新增或更新数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="entities"></param>
        /// <param name="fieldExpression">指定 INSERT OR UPDATE 的字段。如果值为null，实体所有字段都会 INSERT OR UPDATE（不包含忽略字段）。示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>是否执行成功</returns>
        public static async Task<bool> BulkAddOrUpdateAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            if (entities == null || !entities.Any()) return false;

            switch (repository.DbType)
            {
                case DatabaseType.MySql:
                case DatabaseType.SQLite:
                    IReplaceableSql replaceableSql = repository.CreateReplaceableBuilder<TEntity>(fieldExpression == null)
                        .IncludeFields(fieldExpression)
                        .SetParameter(entities)
                        .Build();
                    return await replaceableSql.ExecuteCommandAsync(connection, transaction, repository, commandTimeout) > 0;
                default:
                    if (transaction?.Connection != null)
                    {
                        foreach (var entity in entities)
                        {
                            if (!await transaction.Connection.AddOrUpdateAsync(repository, entity, fieldExpression, transaction, commandTimeout))
                            {
                                return false;
                            }
                        }

                        return true;
                    }
                    else
                    {
                        return await repository.ExecuteTransactionAsync(connection, async trans =>
                        {
                            foreach (var entity in entities)
                            {
                                if (!await connection.AddOrUpdateAsync(repository, entity, fieldExpression, trans, commandTimeout))
                                {
                                    trans.Rollback();
                                    return false;
                                }
                            }

                            trans.Commit();
                            return true;
                        });
                    }
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
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>是否执行成功</returns>
        public static async Task<bool> DeleteAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, TEntity entity, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            IDeleteableSql deleteableSql = repository.CreateDeleteableBuilder<TEntity>()
                .SetParameter(entity)
                .Build();
            return await deleteableSql.ExecuteCommandAsync(connection, transaction, repository, commandTimeout) > 0;
        }
        /// <summary>
        /// 删除数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="whereExpression">WHERE过滤条件</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>返回受影响的行数</returns>
        public static async Task<int> DeleteAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            IDeleteableSql deleteableSql = repository.CreateDeleteableBuilder<TEntity>()
                .Where(whereExpression)
                .Build();
            return await deleteableSql.ExecuteCommandAsync(connection, transaction, repository, commandTimeout);
        }
        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="sqlMonitor"></param>
        /// <param name="deleteableSql"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>返回受影响的行数</returns>
        public static async Task<int> DeleteAsync(this IDbConnection connection, IDeleteableSql deleteableSql, IDbTransaction transaction = null, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            return await deleteableSql.ExecuteCommandAsync(connection, transaction, sqlMonitor, commandTimeout);
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="entity">实体</param>
        /// <param name="fieldExpression">指定 UPDATE 的字段。如果值为null，实体所有字段都会 UPDATE（不包含主键字段和忽略字段）。示例：
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
        /// <returns>返回受影响的行数</returns>
        public static async Task<int> UpdateAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, Expression<Func<TEntity, bool>> whereExpression = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            IUpdateableSql updateableSql = repository.CreateUpdateableBuilder<TEntity>(fieldExpression == null)
                .IncludeFields(fieldExpression, entity)
                .Where(whereExpression)
                .Build();
            return await updateableSql.ExecuteCommandAsync(connection, transaction, repository, commandTimeout);
        }
        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="sqlMonitor"></param>
        /// <param name="updateableSql"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>返回受影响的行数</returns>
        public static async Task<int> UpdateAsync(this IDbConnection connection, IUpdateableSql updateableSql, IDbTransaction transaction = null, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            return await updateableSql.ExecuteCommandAsync(connection, transaction, sqlMonitor, commandTimeout);
        }

        /// <summary>
        /// 批量更新数据
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="entities">实体</param>
        /// <param name="fieldExpression">指定 UPDATE 的字段。如果值为null，实体所有字段都会 UPDATE（不包含主键字段和忽略字段）。示例：
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
        /// <returns>是否执行成功</returns>
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
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="value"></param>
        /// <param name="fieldExpression"></param>
        /// <param name="whereExpression"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>是否执行成功</returns>
        public static async Task<bool> IncrAsync<TEntity, TValue>(this IDbConnection connection, IBaseRepository repository, TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null, int? commandTimeout = null) where TValue : struct
        {
            IUpdateableSql updateableSql = repository.CreateUpdateableBuilder<TEntity>(false)
                .IncrFields(fieldExpression, value)
                .Where(whereExpression)
                .Build();
            return await updateableSql.ExecuteCommandAsync(connection, transaction, repository, commandTimeout) > 0;
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
        /// <param name="whereExpression"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns>是否执行成功</returns>
        public static async Task<bool> DecrAsync<TEntity, TValue>(this IDbConnection connection, IBaseRepository repository, TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null, int? commandTimeout = null) where TValue : struct
        {
            IUpdateableSql updateableSql = repository.CreateUpdateableBuilder<TEntity>(false)
                .DecrFields(fieldExpression, value)
                .Where(whereExpression)
                .Build();
            return await updateableSql.ExecuteCommandAsync(connection, transaction, repository, commandTimeout) > 0;
        }

        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="sqlMonitor"></param>
        /// <param name="queryableSql"></param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<IEnumerable<TEntity>> QueryAsync<TEntity>(this IDbConnection connection, IQueryableSql queryableSql, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            return await queryableSql.ExecuteCommandAsync<TEntity>(connection, sqlMonitor, commandTimeout);
        }
        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="connection">Database connection</param>
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
            IQueryableSql queryableSql = repository.CreateQueryableBuilder<TEntity>(fieldExpression == null)
                .IncludeFields(fieldExpression)
                .Where(whereExpression)
                .OrderBy(orderByCondition)
                .Page(pageIndex, pageSize)
                .Build();
            return await queryableSql.ExecuteCommandAsync<TEntity>(connection, repository, commandTimeout);
        }
        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="connection">Database connection</param>
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
            IQueryableSql queryableSql = repository.CreateQueryableBuilder<TEntity>(fieldExpression == null)
                .IncludeFields(fieldExpression)
                .Where(whereExpression)
                .OrderBy(orderByCondition)
                .Offset(offset, rows)
                .Build();
            return await queryableSql.ExecuteCommandAsync<TEntity>(connection, repository, commandTimeout);
        }

        /// <summary>
        /// 查询单个数据
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="sqlMonitor"></param>
        /// <param name="queryableSql"></param>
        /// <param name="singleCheck">是否执行单一结果检查。true：如果查询到多个结果会抛出异常，false：默认取第一个结果或默认值</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<TEntity> GetAsync<TEntity>(this IDbConnection connection, IQueryableSql queryableSql, bool singleCheck = false, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            return await queryableSql.ExecuteCommandSingleResultAsync<TEntity>(connection, singleCheck, sqlMonitor, commandTimeout);
        }
        /// <summary>
        /// 查询单个数据
        /// </summary>
        /// <param name="connection">Database connection</param>
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
            IQueryableSql queryableSql = repository.CreateQueryableBuilder<TEntity>(fieldExpression == null)
                .IncludeFields(fieldExpression)
                .Where(whereExpression)
                .Build();
            return await queryableSql.ExecuteCommandSingleResultAsync<TEntity>(connection, singleCheck, repository, commandTimeout);
        }

        /// <summary>
        /// 统计数量
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="countableSql"></param>
        /// <param name="sqlMonitor"></param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<int> CountAsync(this IDbConnection connection, ICountableSql countableSql, ISqlMonitor sqlMonitor = null, int? commandTimeout = null)
        {
            return await countableSql.ExecuteCommandAsync(connection, sqlMonitor, commandTimeout);
        }
        /// <summary>
        /// 统计数量
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="whereExpression">WHERE过滤条件</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        public static async Task<int> CountAsync<TEntity>(this IDbConnection connection, IBaseRepository repository, Expression<Func<TEntity, bool>> whereExpression, int? commandTimeout = null)
        {
            ICountableSql countableSql = repository.CreateCountableBuilder<TEntity>()
                .Where(whereExpression)
                .Build();
            return await countableSql.ExecuteCommandAsync(connection, repository, commandTimeout);
        }

        /// <summary>
        /// 查询指定的表是否存在
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="repository"></param>
        /// <param name="tableName">表名称</param>
        /// <returns></returns>
        public static async Task<bool> IsTableExistsAsync(this IDbConnection connection, IBaseRepository repository, string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return false;
            }

            var dbType = repository.DbType;
            var dbName = connection.Database;
            var sql = dbType.GetSqlForCountTable(dbName, tableName);
            IQueryableSql queryableSql = new DefaultQueryableSql
            {
                Sql = sql
            };
            return await queryableSql.ExecuteCommandSingleResultAsync<int>(connection, false, sqlMonitor: repository) > 0;
        }

        public static async Task<bool> IsTableFieldExistsAsync(this IDbConnection connection, IBaseRepository repository, string tableName, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            var dbType = repository.DbType;
            var dbName = connection.Database;
            var sql = dbType.GetSqlForCountTableField(dbName, tableName, fieldName);
            IQueryableSql queryableSql = new DefaultQueryableSql
            {
                Sql = sql
            };
            return await queryableSql.ExecuteCommandSingleResultAsync<int>(connection, false, sqlMonitor: repository) > 0;
        }
#endif
        #endregion
    }
}
