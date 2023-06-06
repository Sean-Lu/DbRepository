using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Sean.Core.DbRepository.Extensions;
using System.Linq;
using System.Reflection;
using Sean.Utility.Extensions;
#if NETSTANDARD || NET5_0_OR_GREATER
using Microsoft.Extensions.Configuration;
#endif

namespace Sean.Core.DbRepository;

/// <summary>
/// Database table entity base repository.
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public abstract class EntityBaseRepository<TEntity> : BaseRepository, IBaseRepository<TEntity> where TEntity : class
{
    public virtual string MainTableName => typeof(TEntity).GetMainTableName();

    #region Constructors
#if NETSTANDARD || NET5_0_OR_GREATER
    /// <summary>
    /// Single or clustered database.
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="configName">Configuration ConnectionStrings name</param>
    protected EntityBaseRepository(IConfiguration configuration, string configName = Constants.Master) : base(configuration, configName)
    {
    }
#else
    /// <summary>
    /// Single or clustered database.
    /// </summary>
    /// <param name="configName">Configuration ConnectionStrings name</param>
    protected EntityBaseRepository(string configName = Constants.Master) : base(configName)
    {
    }
#endif
    /// <summary>
    /// Single or clustered database.
    /// </summary>
    /// <param name="connectionSettings"></param>
    protected EntityBaseRepository(MultiConnectionSettings connectionSettings) : base(connectionSettings)
    {
    }
    /// <summary>
    /// Single database.
    /// </summary>
    /// <param name="connString"></param>
    /// <param name="type"></param>
    protected EntityBaseRepository(string connString, DatabaseType type) : base(connString, type)
    {

    }
    /// <summary>
    /// Single database.
    /// </summary>
    /// <param name="connString"></param>
    /// <param name="factory"></param>
    protected EntityBaseRepository(string connString, DbProviderFactory factory) : base(connString, factory)
    {

    }
    /// <summary>
    /// Single database.
    /// </summary>
    /// <param name="connString"></param>
    /// <param name="providerName"></param>
    protected EntityBaseRepository(string connString, string providerName) : base(connString, providerName)
    {

    }
    #endregion

    /// <summary>
    /// The name of the master table is used by default: <see cref="MainTableName"/>. This method can be overridden for the purpose of custom split tables or automatic table creation.
    /// </summary>
    /// <returns></returns>
    public override string TableName()
    {
        return MainTableName;
    }

    #region Synchronous method
    public virtual bool Add(TEntity entity, bool returnAutoIncrementId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
    {
        if (entity == null) return false;

        PropertyInfo keyIdentityProperty;
        if (returnAutoIncrementId && (keyIdentityProperty = typeof(TEntity).GetKeyIdentityProperty()) != null)
        {
            switch (DbType)
            {
                case DatabaseType.MsAccess:
                case DatabaseType.Oracle:
                case DatabaseType.DuckDB:
                case DatabaseType.Informix:
                    {
                        return Execute(connection =>
                        {
                            var sqlCommandInsert = this.GetSqlForAdd(entity, false, fieldExpression);
                            sqlCommandInsert.Connection = connection;
                            sqlCommandInsert.Transaction = transaction;
                            sqlCommandInsert.CommandTimeout = CommandTimeout;
                            var addResult = Execute(sqlCommandInsert) > 0;
                            if (!addResult)
                            {
                                return false;
                            }

                            string returnIdSql = null;
                            switch (DbType)
                            {
                                case DatabaseType.MsAccess:
                                    {
                                        returnIdSql = "SELECT @@IDENTITY AS Id";
                                        break;
                                    }
                                case DatabaseType.Oracle:
                                    {
                                        var sequenceName = typeof(TEntity).GetEntityInfo()?.SequenceName;
                                        returnIdSql = $"SELECT {DatabaseType.Oracle.MarkAsTableOrFieldName(sequenceName)}.CURRVAL AS Id FROM dual";
                                        break;
                                    }
                                case DatabaseType.DuckDB:
                                    {
                                        var sequenceName = typeof(TEntity).GetEntityInfo()?.SequenceName;
                                        returnIdSql = $"SELECT CURRVAL('{sequenceName}')";
                                        break;
                                    }
                                case DatabaseType.Informix:
                                    {
                                        returnIdSql = $"SELECT dbinfo('sqlca.sqlerrd1') AS Id FROM systables WHERE tabname='{TableName()}' AND tabtype='T'";
                                        break;
                                    }
                            }

                            var sqlCommandReturnId = new DefaultSqlCommand
                            {
                                Sql = returnIdSql,
                                Connection = connection,
                                Transaction = transaction,
                                CommandTimeout = CommandTimeout
                            };
                            var id = ExecuteScalar<long>(sqlCommandReturnId);
                            if (id < 1)
                            {
                                return false;
                            }

                            keyIdentityProperty.SetValue(entity, id, null);
                            return true;
                        }, true, transaction);
                    }
                default:
                    {
                        var sqlCommandReturnId = this.GetSqlForAdd(entity, true, fieldExpression);
                        sqlCommandReturnId.Master = true;
                        sqlCommandReturnId.Transaction = transaction;
                        sqlCommandReturnId.CommandTimeout = CommandTimeout;
                        var id = ExecuteScalar<long>(sqlCommandReturnId);
                        if (id < 1)
                        {
                            return false;
                        }

                        keyIdentityProperty.SetValue(entity, id, null);
                        return true;
                    }
            }
        }

        var sqlCommand = this.GetSqlForAdd(entity, false, fieldExpression);
        sqlCommand.Master = true;
        sqlCommand.Transaction = transaction;
        sqlCommand.CommandTimeout = CommandTimeout;
        return Execute(sqlCommand) > 0;
    }
    public virtual bool Add(IEnumerable<TEntity> entities, bool returnAutoIncrementId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
    {
        if (entities == null || !entities.Any()) return false;

        if (returnAutoIncrementId && typeof(TEntity).GetKeyIdentityProperty() != null)
        {
            //if (transaction?.Connection == null)
            //{
            //    return ExecuteAutoTransaction(trans =>
            //    {
            //        foreach (var entity in entities)
            //        {
            //            if (!Add(entity, returnAutoIncrementId, fieldExpression, trans))
            //            {
            //                return false;
            //            }
            //        }
            //        return true;
            //    });
            //}

            foreach (var entity in entities)
            {
                if (!Add(entity, returnAutoIncrementId, fieldExpression, transaction))
                {
                    return false;
                }
            }

            return true;
        }

        var bulkCountLimit = BulkEntityCount ?? DbContextConfiguration.Options.BulkEntityCount;
        if (bulkCountLimit.HasValue && entities.Count() > bulkCountLimit.Value)
        {
            return entities.PagingExecute(bulkCountLimit.Value, (pageIndex, models) =>
            {
                var sqlCommand = this.GetSqlForBulkAdd(models, fieldExpression);
                sqlCommand.Master = true;
                sqlCommand.Transaction = transaction;
                sqlCommand.CommandTimeout = CommandTimeout;
                return Execute(sqlCommand) > 0;
            });
        }

        var sqlCommand = this.GetSqlForBulkAdd(entities, fieldExpression);
        sqlCommand.Master = true;
        sqlCommand.Transaction = transaction;
        sqlCommand.CommandTimeout = CommandTimeout;
        return Execute(sqlCommand) > 0;
    }

    public virtual bool AddOrUpdate(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
    {
        if (entity == null) return false;

        switch (DbType)
        {
            case DatabaseType.MySql:
            case DatabaseType.MariaDB:
            case DatabaseType.TiDB:
            case DatabaseType.SQLite:
                {
                    var sqlCommand = this.GetSqlForAddOrUpdate(entity, fieldExpression);
                    sqlCommand.Master = true;
                    sqlCommand.Transaction = transaction;
                    sqlCommand.CommandTimeout = CommandTimeout;
                    return Execute(sqlCommand) > 0;
                }
            default:
                {
                    var sqlCommand = this.GetSqlForEntityExists(entity);
                    sqlCommand.Master = true;
                    sqlCommand.CommandTimeout = CommandTimeout;
                    if (ExecuteScalar<int>(sqlCommand) < 1)
                    {
                        // INSERT
                        return Add(entity, false, fieldExpression, transaction);
                    }

                    //if (transaction?.Connection == null)
                    //{
                    //    return ExecuteAutoTransaction(trans =>
                    //    {
                    //        // DELETE && INSERT
                    //        return Delete(entity, trans) && Add(entity, false, fieldExpression, trans);
                    //    });
                    //}

                    // DELETE && INSERT
                    return Delete(entity, transaction) && Add(entity, false, fieldExpression, transaction);
                }
        }
    }
    public virtual bool AddOrUpdate(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
    {
        if (entities == null || !entities.Any()) return false;

        switch (DbType)
        {
            case DatabaseType.MySql:
            case DatabaseType.MariaDB:
            case DatabaseType.TiDB:
            case DatabaseType.SQLite:
                {
                    var bulkCountLimit = BulkEntityCount ?? DbContextConfiguration.Options.BulkEntityCount;
                    if (bulkCountLimit.HasValue && entities.Count() > bulkCountLimit.Value)
                    {
                        return entities.PagingExecute(bulkCountLimit.Value, (pageIndex, models) =>
                        {
                            var sqlCommand = this.GetSqlForBulkAddOrUpdate(models, fieldExpression);
                            sqlCommand.Master = true;
                            sqlCommand.Transaction = transaction;
                            sqlCommand.CommandTimeout = CommandTimeout;
                            return Execute(sqlCommand) > 0;
                        });
                    }

                    var sqlCommand = this.GetSqlForBulkAddOrUpdate(entities, fieldExpression);
                    sqlCommand.Master = true;
                    sqlCommand.Transaction = transaction;
                    sqlCommand.CommandTimeout = CommandTimeout;
                    return Execute(sqlCommand) > 0;
                }
            default:
                {
                    //if (transaction?.Connection == null)
                    //{
                    //    return ExecuteAutoTransaction(trans =>
                    //    {
                    //        foreach (var entity in entities)
                    //        {
                    //            if (!AddOrUpdate(entity, fieldExpression, trans))
                    //            {
                    //                return false;
                    //            }
                    //        }
                    //        return true;
                    //    });
                    //}

                    foreach (var entity in entities)
                    {
                        if (!AddOrUpdate(entity, fieldExpression, transaction))
                        {
                            return false;
                        }
                    }

                    return true;
                }
        }
    }

    public virtual bool Delete(TEntity entity, IDbTransaction transaction = null)
    {
        var sqlCommand = this.GetSqlForDelete(entity);
        sqlCommand.Master = true;
        sqlCommand.Transaction = transaction;
        sqlCommand.CommandTimeout = CommandTimeout;
        return Execute(sqlCommand) > 0;
    }
    public virtual bool Delete(IEnumerable<TEntity> entities, IDbTransaction transaction = null)
    {
        if (entities == null || !entities.Any()) return false;

        foreach (var entity in entities)
        {
            if (!Delete(entity, transaction))
            {
                return false;
            }
        }

        return true;
    }
    public virtual int Delete(Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null)
    {
        var sqlCommand = this.GetSqlForDelete(whereExpression);
        sqlCommand.Master = true;
        sqlCommand.Transaction = transaction;
        sqlCommand.CommandTimeout = CommandTimeout;
        return Execute(sqlCommand);
    }
    public virtual int DeleteAll(IDbTransaction transaction = null)
    {
        return DeleteAll(TableName(), transaction);
    }

    public virtual int Update(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, Expression<Func<TEntity, bool>> whereExpression = null, IDbTransaction transaction = null)
    {
        var sqlCommand = this.GetSqlForUpdate(entity, fieldExpression, whereExpression);
        sqlCommand.Master = true;
        sqlCommand.Transaction = transaction;
        sqlCommand.CommandTimeout = CommandTimeout;
        return Execute(sqlCommand);
    }
    public virtual bool Update(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
    {
        if (entities == null || !entities.Any()) return false;

        //if (transaction?.Connection == null)
        //{
        //    return ExecuteAutoTransaction(trans =>
        //    {
        //        foreach (var entity in entities)
        //        {
        //            if (!(Update(entity, fieldExpression, null, trans) > 0))
        //            {
        //                return false;
        //            }
        //        }
        //        return true;
        //    });
        //}

        foreach (var entity in entities)
        {
            if (!(Update(entity, fieldExpression, null, transaction) > 0))
            {
                return false;
            }
        }

        return true;
    }

    public virtual bool Increment<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null) where TValue : struct
    {
        var sqlCommand = this.GetSqlForIncr(value, fieldExpression, whereExpression);
        sqlCommand.Master = true;
        sqlCommand.Transaction = transaction;
        sqlCommand.CommandTimeout = CommandTimeout;
        return Execute(sqlCommand) > 0;
    }
    public virtual bool Decrement<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null) where TValue : struct
    {
        var sqlCommand = this.GetSqlForDecr(value, fieldExpression, whereExpression);
        sqlCommand.Master = true;
        sqlCommand.Transaction = transaction;
        sqlCommand.CommandTimeout = CommandTimeout;
        return Execute(sqlCommand) > 0;
    }

    public virtual bool Save<TEntityState>(TEntityState entity, bool returnAutoIncrementId = false, IDbTransaction transaction = null) where TEntityState : EntityStateBase, TEntity
    {
        if (entity == null)
        {
            return false;
        }

        if (entity.EntityState == EntityStateType.Unchanged)
        {
            return true;
        }

        switch (entity.EntityState)
        {
            case EntityStateType.Added:
                if (!Add(entity, returnAutoIncrementId, transaction: transaction))
                {
                    return false;
                }
                break;
            case EntityStateType.Modified:
                if (Update(entity, transaction: transaction) < 1)
                {
                    return false;
                }
                break;
            case EntityStateType.Deleted:
                if (!Delete(entity, transaction: transaction))
                {
                    return false;
                }
                break;
            default:
                throw new NotImplementedException($"EntityState: {entity.EntityState}");
        }

        if (transaction == null)
        {
            entity.ResetEntityState();
        }

        return true;
    }
    public virtual bool Save<TEntityState>(IEnumerable<TEntityState> entities, bool returnAutoIncrementId = false, IDbTransaction transaction = null) where TEntityState : EntityStateBase, TEntity
    {
        if (entities == null || !entities.Any())
        {
            return false;
        }

        if (entities.All(c => c.EntityState == EntityStateType.Unchanged))
        {
            return true;
        }

        var result = ExecuteAutoTransaction(trans =>
        {
            var addList = entities.Where(t => t.EntityState == EntityStateType.Added).ToList();
            if (addList.Any())
            {
                if (!Add(addList, returnAutoIncrementId, transaction: trans))
                {
                    return false;
                }
            }

            var updateList = entities.Where(t => t.EntityState == EntityStateType.Modified).ToList();
            if (updateList.Any())
            {
                if (!Update(updateList, transaction: trans))
                {
                    return false;
                }
            }

            var deleteList = entities.Where(t => t.EntityState == EntityStateType.Deleted).ToList();
            if (deleteList.Any())
            {
                if (!Delete(deleteList, transaction: trans))
                {
                    return false;
                }
            }

            return true;
        }, transaction);

        if (result && transaction == null)
        {
            entities.ResetEntityState();
        }

        return result;
    }

    public virtual IEnumerable<TEntity> Query(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? pageIndex = null, int? pageSize = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true)
    {
        var sqlCommand = this.GetSqlForQuery(whereExpression, orderBy, pageIndex, pageSize, fieldExpression);
        sqlCommand.Master = master;
        sqlCommand.CommandTimeout = CommandTimeout;
        return Query<TEntity>(sqlCommand);
    }
    public virtual IEnumerable<TEntity> QueryOffset(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? offset = null, int? rows = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true)
    {
        var sqlCommand = this.GetSqlForQueryOffset(whereExpression, orderBy, offset, rows, fieldExpression);
        sqlCommand.Master = master;
        sqlCommand.CommandTimeout = CommandTimeout;
        return Query<TEntity>(sqlCommand);
    }

    public virtual TEntity Get(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true)
    {
        var sqlCommand = this.GetSqlForGet(whereExpression, fieldExpression);
        sqlCommand.Master = master;
        sqlCommand.CommandTimeout = CommandTimeout;
        return Get<TEntity>(sqlCommand);
    }

    public virtual int Count(Expression<Func<TEntity, bool>> whereExpression, bool master = true)
    {
        var sqlCommand = this.GetSqlForCount(whereExpression);
        sqlCommand.Master = master;
        sqlCommand.CommandTimeout = CommandTimeout;
        return ExecuteScalar<int>(sqlCommand);
    }

    public virtual bool Exists(Expression<Func<TEntity, bool>> whereExpression, bool master = true)
    {
        return Count(whereExpression, master) > 0;
    }

    public virtual bool IsTableExists(bool master = true, bool useCache = true)
    {
        return IsTableExists(TableName(), master, useCache);
    }

    public virtual bool IsTableFieldExists(Expression<Func<TEntity, object>> fieldExpression, bool master = true, bool useCache = true)
    {
        var fieldNames = fieldExpression.GetFieldNames();
        if (fieldNames == null || fieldNames.Count < 1)
        {
            return false;
        }

        var tableName = TableName();
        foreach (var fieldName in fieldNames)
        {
            if (!IsTableFieldExists(tableName, fieldName, master, useCache))
            {
                return false;
            }
        }
        return true;
    }

    public virtual void AddTableField(Expression<Func<TEntity, object>> fieldExpression, string fieldType, bool master = true)
    {
        var fieldNames = fieldExpression.GetFieldNames();
        if (fieldNames == null || fieldNames.Count < 1)
        {
            return;
        }

        var tableName = TableName();
        foreach (var fieldName in fieldNames)
        {
            AddTableField(tableName, fieldName, fieldType, master);
        }
    }
    #endregion

    #region Asynchronous method
    public virtual async Task<bool> AddAsync(TEntity entity, bool returnAutoIncrementId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
    {
        if (entity == null) return false;

        PropertyInfo keyIdentityProperty;
        if (returnAutoIncrementId && (keyIdentityProperty = typeof(TEntity).GetKeyIdentityProperty()) != null)
        {
            switch (DbType)
            {
                case DatabaseType.MsAccess:
                case DatabaseType.Oracle:
                case DatabaseType.DuckDB:
                case DatabaseType.Informix:
                    {
                        return await ExecuteAsync(async connection =>
                        {
                            var sqlCommandInsert = this.GetSqlForAdd(entity, false, fieldExpression);
                            sqlCommandInsert.Connection = connection;
                            sqlCommandInsert.Transaction = transaction;
                            sqlCommandInsert.CommandTimeout = CommandTimeout;
                            var addResult = await ExecuteAsync(sqlCommandInsert) > 0;
                            if (!addResult)
                            {
                                return false;
                            }

                            string returnIdSql = null;
                            switch (DbType)
                            {
                                case DatabaseType.MsAccess:
                                    {
                                        returnIdSql = "SELECT @@IDENTITY AS Id";
                                        break;
                                    }
                                case DatabaseType.Oracle:
                                    {
                                        var sequenceName = typeof(TEntity).GetEntityInfo()?.SequenceName;
                                        returnIdSql = $"SELECT {DatabaseType.Oracle.MarkAsTableOrFieldName(sequenceName)}.CURRVAL AS Id FROM dual";
                                        break;
                                    }
                                case DatabaseType.DuckDB:
                                    {
                                        var sequenceName = typeof(TEntity).GetEntityInfo()?.SequenceName;
                                        returnIdSql = $"SELECT CURRVAL('{sequenceName}')";
                                        break;
                                    }
                                case DatabaseType.Informix:
                                    {
                                        returnIdSql = $"SELECT dbinfo('sqlca.sqlerrd1') AS Id FROM systables WHERE tabname='{TableName()}' AND tabtype='T'";
                                        break;
                                    }
                            }

                            var sqlCommandReturnId = new DefaultSqlCommand
                            {
                                Sql = returnIdSql,
                                Connection = connection,
                                Transaction = transaction,
                                CommandTimeout = CommandTimeout
                            };
                            var id = await ExecuteScalarAsync<long>(sqlCommandReturnId);
                            if (id < 1)
                            {
                                return false;
                            }

                            keyIdentityProperty.SetValue(entity, id, null);
                            return true;
                        }, true, transaction);
                    }
                default:
                    {
                        var sqlCommandReturnId = this.GetSqlForAdd(entity, true, fieldExpression);
                        sqlCommandReturnId.Master = true;
                        sqlCommandReturnId.Transaction = transaction;
                        sqlCommandReturnId.CommandTimeout = CommandTimeout;
                        var id = await ExecuteScalarAsync<long>(sqlCommandReturnId);
                        if (id < 1)
                        {
                            return false;
                        }

                        keyIdentityProperty.SetValue(entity, id, null);
                        return true;
                    }
            }
        }

        var sqlCommand = this.GetSqlForAdd(entity, false, fieldExpression);
        sqlCommand.Master = true;
        sqlCommand.Transaction = transaction;
        sqlCommand.CommandTimeout = CommandTimeout;
        return await ExecuteAsync(sqlCommand) > 0;
    }
    public virtual async Task<bool> AddAsync(IEnumerable<TEntity> entities, bool returnAutoIncrementId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
    {
        if (entities == null || !entities.Any()) return false;

        if (returnAutoIncrementId && typeof(TEntity).GetKeyIdentityProperty() != null)
        {
            //if (transaction?.Connection == null)
            //{
            //    return await ExecuteAutoTransactionAsync(async trans =>
            //    {
            //        foreach (var entity in entities)
            //        {
            //            if (!await AddAsync(entity, returnAutoIncrementId, fieldExpression, trans))
            //            {
            //                return false;
            //            }
            //        }
            //        return true;
            //    });
            //}

            foreach (var entity in entities)
            {
                if (!await AddAsync(entity, returnAutoIncrementId, fieldExpression, transaction))
                {
                    return false;
                }
            }

            return true;
        }

        var bulkCountLimit = BulkEntityCount ?? DbContextConfiguration.Options.BulkEntityCount;
        if (bulkCountLimit.HasValue && entities.Count() > bulkCountLimit.Value)
        {
            return await entities.PagingExecuteAsync(bulkCountLimit.Value, async (pageIndex, models) =>
            {
                var sqlCommand = this.GetSqlForBulkAdd(models, fieldExpression);
                sqlCommand.Master = true;
                sqlCommand.Transaction = transaction;
                sqlCommand.CommandTimeout = CommandTimeout;
                return await ExecuteAsync(sqlCommand) > 0;
            });
        }

        var sqlCommand = this.GetSqlForBulkAdd(entities, fieldExpression);
        sqlCommand.Master = true;
        sqlCommand.Transaction = transaction;
        sqlCommand.CommandTimeout = CommandTimeout;
        return await ExecuteAsync(sqlCommand) > 0;
    }

    public virtual async Task<bool> AddOrUpdateAsync(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
    {
        if (entity == null) return false;

        switch (DbType)
        {
            case DatabaseType.MySql:
            case DatabaseType.MariaDB:
            case DatabaseType.TiDB:
            case DatabaseType.SQLite:
                {
                    var sqlCommand = this.GetSqlForAddOrUpdate(entity, fieldExpression);
                    sqlCommand.Master = true;
                    sqlCommand.Transaction = transaction;
                    sqlCommand.CommandTimeout = CommandTimeout;
                    return await ExecuteAsync(sqlCommand) > 0;
                }
            default:
                {
                    var sqlCommand = this.GetSqlForEntityExists(entity);
                    sqlCommand.Master = true;
                    sqlCommand.CommandTimeout = CommandTimeout;
                    if (await ExecuteScalarAsync<int>(sqlCommand) < 1)
                    {
                        // INSERT
                        return await AddAsync(entity, false, fieldExpression, transaction);
                    }

                    //if (transaction?.Connection == null)
                    //{
                    //    return await ExecuteAutoTransactionAsync(async trans =>
                    //    {
                    //        // DELETE && INSERT
                    //        return await DeleteAsync(entity, trans) && await AddAsync(entity, false, fieldExpression, trans);
                    //    });
                    //}

                    // DELETE && INSERT
                    return await DeleteAsync(entity, transaction) && await AddAsync(entity, false, fieldExpression, transaction);
                }
        }
    }
    public virtual async Task<bool> AddOrUpdateAsync(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
    {
        if (entities == null || !entities.Any()) return false;

        switch (DbType)
        {
            case DatabaseType.MySql:
            case DatabaseType.MariaDB:
            case DatabaseType.TiDB:
            case DatabaseType.SQLite:
                {
                    var bulkCountLimit = BulkEntityCount ?? DbContextConfiguration.Options.BulkEntityCount;
                    if (bulkCountLimit.HasValue && entities.Count() > bulkCountLimit.Value)
                    {
                        return await entities.PagingExecuteAsync(bulkCountLimit.Value, async (pageIndex, models) =>
                        {
                            var sqlCommand = this.GetSqlForBulkAddOrUpdate(models, fieldExpression);
                            sqlCommand.Master = true;
                            sqlCommand.Transaction = transaction;
                            sqlCommand.CommandTimeout = CommandTimeout;
                            return await ExecuteAsync(sqlCommand) > 0;
                        });
                    }

                    var sqlCommand = this.GetSqlForBulkAddOrUpdate(entities, fieldExpression);
                    sqlCommand.Master = true;
                    sqlCommand.Transaction = transaction;
                    sqlCommand.CommandTimeout = CommandTimeout;
                    return await ExecuteAsync(sqlCommand) > 0;
                }
            default:
                {
                    //if (transaction?.Connection == null)
                    //{
                    //    return await ExecuteAutoTransactionAsync(async trans =>
                    //    {
                    //        foreach (var entity in entities)
                    //        {
                    //            if (!await AddOrUpdateAsync(entity, fieldExpression, trans))
                    //            {
                    //                return false;
                    //            }
                    //        }
                    //        return true;
                    //    });
                    //}

                    foreach (var entity in entities)
                    {
                        if (!await AddOrUpdateAsync(entity, fieldExpression, transaction))
                        {
                            return false;
                        }
                    }

                    return true;
                }
        }
    }

    public virtual async Task<bool> DeleteAsync(TEntity entity, IDbTransaction transaction = null)
    {
        var sqlCommand = this.GetSqlForDelete(entity);
        sqlCommand.Master = true;
        sqlCommand.Transaction = transaction;
        sqlCommand.CommandTimeout = CommandTimeout;
        return await ExecuteAsync(sqlCommand) > 0;
    }
    public virtual async Task<bool> DeleteAsync(IEnumerable<TEntity> entities, IDbTransaction transaction = null)
    {
        if (entities == null || !entities.Any()) return false;

        foreach (var entity in entities)
        {
            if (!await DeleteAsync(entity, transaction))
            {
                return false;
            }
        }

        return true;
    }
    public virtual async Task<int> DeleteAsync(Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null)
    {
        var sqlCommand = this.GetSqlForDelete(whereExpression);
        sqlCommand.Master = true;
        sqlCommand.Transaction = transaction;
        sqlCommand.CommandTimeout = CommandTimeout;
        return await ExecuteAsync(sqlCommand);
    }
    public virtual async Task<int> DeleteAllAsync(IDbTransaction transaction = null)
    {
        return await DeleteAllAsync(TableName(), transaction);
    }

    public virtual async Task<int> UpdateAsync(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, Expression<Func<TEntity, bool>> whereExpression = null, IDbTransaction transaction = null)
    {
        var sqlCommand = this.GetSqlForUpdate(entity, fieldExpression, whereExpression);
        sqlCommand.Master = true;
        sqlCommand.Transaction = transaction;
        sqlCommand.CommandTimeout = CommandTimeout;
        return await ExecuteAsync(sqlCommand);
    }
    public virtual async Task<bool> UpdateAsync(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
    {
        if (entities == null || !entities.Any()) return false;

        //if (transaction?.Connection == null)
        //{
        //    return await ExecuteAutoTransactionAsync(async trans =>
        //    {
        //        foreach (var entity in entities)
        //        {
        //            if (!(await UpdateAsync(entity, fieldExpression, null, trans) > 0))
        //            {
        //                return false;
        //            }
        //        }
        //        return true;
        //    });
        //}

        foreach (var entity in entities)
        {
            if (!(await UpdateAsync(entity, fieldExpression, null, transaction) > 0))
            {
                return false;
            }
        }

        return true;
    }

    public virtual async Task<bool> IncrementAsync<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null) where TValue : struct
    {
        var sqlCommand = this.GetSqlForIncr(value, fieldExpression, whereExpression);
        sqlCommand.Master = true;
        sqlCommand.Transaction = transaction;
        sqlCommand.CommandTimeout = CommandTimeout;
        return await ExecuteAsync(sqlCommand) > 0;
    }
    public virtual async Task<bool> DecrementAsync<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null) where TValue : struct
    {
        var sqlCommand = this.GetSqlForDecr(value, fieldExpression, whereExpression);
        sqlCommand.Master = true;
        sqlCommand.Transaction = transaction;
        sqlCommand.CommandTimeout = CommandTimeout;
        return await ExecuteAsync(sqlCommand) > 0;
    }

    public virtual async Task<bool> SaveAsync<TEntityState>(TEntityState entity, bool returnAutoIncrementId = false, IDbTransaction transaction = null) where TEntityState : EntityStateBase, TEntity
    {
        if (entity == null)
        {
            return false;
        }

        if (entity.EntityState == EntityStateType.Unchanged)
        {
            return true;
        }

        switch (entity.EntityState)
        {
            case EntityStateType.Added:
                if (!await AddAsync(entity, returnAutoIncrementId, transaction: transaction))
                {
                    return false;
                }
                break;
            case EntityStateType.Modified:
                if (await UpdateAsync(entity, transaction: transaction) < 1)
                {
                    return false;
                }
                break;
            case EntityStateType.Deleted:
                if (!await DeleteAsync(entity, transaction: transaction))
                {
                    return false;
                }
                break;
            default:
                throw new NotImplementedException($"EntityState: {entity.EntityState}");
        }

        if (transaction == null)
        {
            entity.ResetEntityState();
        }

        return true;
    }
    public virtual async Task<bool> SaveAsync<TEntityState>(IEnumerable<TEntityState> entities, bool returnAutoIncrementId = false, IDbTransaction transaction = null) where TEntityState : EntityStateBase, TEntity
    {
        if (entities == null || !entities.Any())
        {
            return false;
        }

        if (entities.All(c => c.EntityState == EntityStateType.Unchanged))
        {
            return true;
        }

        var result = await ExecuteAutoTransactionAsync(async trans =>
        {
            var addList = entities.Where(t => t.EntityState == EntityStateType.Added).ToList();
            if (addList.Any())
            {
                if (!await AddAsync(addList, returnAutoIncrementId, transaction: trans))
                {
                    return false;
                }
            }

            var updateList = entities.Where(t => t.EntityState == EntityStateType.Modified).ToList();
            if (updateList.Any())
            {
                if (!await UpdateAsync(updateList, transaction: trans))
                {
                    return false;
                }
            }

            var deleteList = entities.Where(t => t.EntityState == EntityStateType.Deleted).ToList();
            if (deleteList.Any())
            {
                if (!await DeleteAsync(deleteList, transaction: trans))
                {
                    return false;
                }
            }

            return true;
        }, transaction);

        if (result && transaction == null)
        {
            entities.ResetEntityState();
        }

        return result;
    }

    public virtual async Task<IEnumerable<TEntity>> QueryAsync(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? pageIndex = null, int? pageSize = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true)
    {
        var sqlCommand = this.GetSqlForQuery(whereExpression, orderBy, pageIndex, pageSize, fieldExpression);
        sqlCommand.Master = master;
        sqlCommand.CommandTimeout = CommandTimeout;
        return await QueryAsync<TEntity>(sqlCommand);
    }
    public virtual async Task<IEnumerable<TEntity>> QueryOffsetAsync(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? offset = null, int? rows = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true)
    {
        var sqlCommand = this.GetSqlForQueryOffset(whereExpression, orderBy, offset, rows, fieldExpression);
        sqlCommand.Master = master;
        sqlCommand.CommandTimeout = CommandTimeout;
        return await QueryAsync<TEntity>(sqlCommand);
    }

    public virtual async Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true)
    {
        var sqlCommand = this.GetSqlForGet(whereExpression, fieldExpression);
        sqlCommand.Master = master;
        sqlCommand.CommandTimeout = CommandTimeout;
        return await GetAsync<TEntity>(sqlCommand);
    }

    public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>> whereExpression, bool master = true)
    {
        var sqlCommand = this.GetSqlForCount(whereExpression);
        sqlCommand.Master = master;
        sqlCommand.CommandTimeout = CommandTimeout;
        return await ExecuteScalarAsync<int>(sqlCommand);
    }

    public virtual async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> whereExpression, bool master = true)
    {
        return await CountAsync(whereExpression, master) > 0;
    }

    public virtual async Task<bool> IsTableExistsAsync(bool master = true, bool useCache = true)
    {
        return await IsTableExistsAsync(TableName(), master, useCache);
    }

    public virtual async Task<bool> IsTableFieldExistsAsync(Expression<Func<TEntity, object>> fieldExpression, bool master = true, bool useCache = true)
    {
        var fieldNames = fieldExpression.GetFieldNames();
        if (fieldNames == null || fieldNames.Count < 1)
        {
            return false;
        }

        var tableName = TableName();
        foreach (var fieldName in fieldNames)
        {
            if (!await IsTableFieldExistsAsync(tableName, fieldName, master, useCache))
            {
                return false;
            }
        }
        return true;
    }

    public virtual async Task AddTableFieldAsync(Expression<Func<TEntity, object>> fieldExpression, string fieldType, bool master = true)
    {
        var fieldNames = fieldExpression.GetFieldNames();
        if (fieldNames == null || fieldNames.Count < 1)
        {
            return;
        }

        var tableName = TableName();
        foreach (var fieldName in fieldNames)
        {
            await AddTableFieldAsync(tableName, fieldName, fieldType, master);
        }
    }
    #endregion
}