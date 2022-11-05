using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Sean.Core.DbRepository.Extensions;
using Sean.Core.DbRepository.Util;
using System.Linq;
using System.Collections.ObjectModel;
using System.Reflection;
#if NETSTANDARD
using Microsoft.Extensions.Configuration;
#endif

namespace Sean.Core.DbRepository;

/// <summary>
/// Database table base repository
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public abstract class EntityBaseRepository<TEntity> : BaseRepository, IBaseRepository<TEntity> where TEntity : class
{
    public virtual string MainTableName => typeof(TEntity).GetMainTableName();

    #region Constructors
#if NETSTANDARD
    /// <summary>
    /// Single or clustered database.
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="configName">Configuration ConnectionStrings name</param>
    protected EntityBaseRepository(IConfiguration configuration = null, string configName = Constants.Master) : base(configuration, configName)
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
    protected EntityBaseRepository(string connString, DatabaseType type) : this(new MultiConnectionSettings(new ConnectionStringOptions(connString, type)))
    {

    }
    /// <summary>
    /// Single database.
    /// </summary>
    /// <param name="connString"></param>
    /// <param name="factory"></param>
    protected EntityBaseRepository(string connString, DbProviderFactory factory) : this(new MultiConnectionSettings(new ConnectionStringOptions(connString, factory)))
    {

    }
    /// <summary>
    /// Single database.
    /// </summary>
    /// <param name="connString"></param>
    /// <param name="providerName"></param>
    protected EntityBaseRepository(string connString, string providerName) : this(new MultiConnectionSettings(new ConnectionStringOptions(connString, providerName)))
    {

    }
    #endregion

    /// <summary>
    /// 表名：默认返回主表表名 <see cref="MainTableName"/>
    /// </summary>
    /// <returns></returns>
    public override string TableName()
    {
        return MainTableName;
    }

    public virtual void AutoCreateTable(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(tableName));

        if (IsTableExists(tableName))
        {
            return;
        }

        var sql = CreateTableSql(tableName);
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(CreateTableSql));
        }

        Execute(sql);
    }

    #region Synchronous method
    public virtual bool Add(TEntity entity, bool returnAutoIncrementId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
    {
        if (entity == null) return false;

        PropertyInfo keyIdentityProperty;
        if (returnAutoIncrementId && (keyIdentityProperty = typeof(TEntity).GetKeyIdentityProperty()) != null)
        {
            var id = this.GetSqlForAdd(entity, true, fieldExpression).ExecuteScalar<long>(Factory, transaction, true);
            if (id < 1) return false;

            keyIdentityProperty.SetValue(entity, id, null);
            return true;
        }

        return this.GetSqlForAdd(entity, false, fieldExpression).Execute(Factory, transaction, true) > 0;
    }
    public virtual bool Add(IEnumerable<TEntity> entities, bool returnAutoIncrementId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
    {
        if (entities == null || !entities.Any()) return false;

        if (transaction?.Connection == null)
        {
            return ExecuteAutoTransaction(trans =>
            {
                foreach (var entity in entities)
                {
                    if (!Add(entity, returnAutoIncrementId, fieldExpression, trans))
                    {
                        return false;
                    }
                }

                return true;
            });
        }

        foreach (var entity in entities)
        {
            if (!Add(entity, returnAutoIncrementId, fieldExpression, transaction))
            {
                return false;
            }
        }

        return true;
    }

    public virtual bool AddOrUpdate(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
    {
        if (entity == null) return false;

        switch (DbType)
        {
            case DatabaseType.MySql:
            case DatabaseType.SQLite:
                {
                    return this.GetSqlForAddOrUpdate(entity, fieldExpression).Execute(Factory, transaction, true) > 0;
                }
            default:
                {
                    if (this.GetSqlForEntityExists(entity).ExecuteScalar<int>(Factory, null, true) < 1)
                    {
                        // INSERT
                        return Add(entity, false, fieldExpression, transaction);
                    }

                    if (transaction?.Connection != null)
                    {
                        // DELETE
                        if (!Delete(entity, transaction))
                        {
                            return false;
                        }

                        // INSERT
                        return Add(entity, false, fieldExpression, transaction);
                    }

                    return ExecuteAutoTransaction(trans =>
                    {
                        // DELETE
                        if (!Delete(entity, trans))
                        {
                            return false;
                        }

                        // INSERT
                        if (!Add(entity, false, fieldExpression, trans))
                        {
                            return false;
                        }

                        return true;
                    });
                }
        }
    }
    public virtual bool AddOrUpdate(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
    {
        if (entities == null || !entities.Any()) return false;

        if (transaction?.Connection == null)
        {
            return ExecuteAutoTransaction(trans =>
            {
                foreach (var entity in entities)
                {
                    if (!AddOrUpdate(entity, fieldExpression, trans))
                    {
                        return false;
                    }
                }

                return true;
            });
        }

        foreach (var entity in entities)
        {
            if (!AddOrUpdate(entity, fieldExpression, transaction))
            {
                return false;
            }
        }

        return true;
    }

    public virtual bool Delete(TEntity entity, IDbTransaction transaction = null)
    {
        return this.GetSqlForDelete(entity).Execute(Factory, transaction, true) > 0;
    }
    public virtual int Delete(Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null)
    {
        return this.GetSqlForDelete(whereExpression).Execute(Factory, transaction, true);
    }

    public virtual int Update(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, Expression<Func<TEntity, bool>> whereExpression = null, IDbTransaction transaction = null)
    {
        return this.GetSqlForUpdate(entity, fieldExpression, whereExpression).Execute(Factory, transaction, true);
    }
    public virtual bool Update(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
    {
        if (entities == null || !entities.Any()) return false;

        if (transaction?.Connection == null)
        {
            return ExecuteAutoTransaction(trans =>
            {
                foreach (var entity in entities)
                {
                    if (!(Update(entity, fieldExpression, null, trans) > 0))
                    {
                        return false;
                    }
                }

                return true;
            });
        }

        foreach (var entity in entities)
        {
            if (!(Update(entity, fieldExpression, null, transaction) > 0))
            {
                return false;
            }
        }

        return true;
    }

    public virtual bool Incr<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null) where TValue : struct
    {
        return this.GetSqlForIncr(value, fieldExpression, whereExpression).Execute(Factory, transaction, true) > 0;
    }
    public virtual bool Decr<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null) where TValue : struct
    {
        return this.GetSqlForDecr(value, fieldExpression, whereExpression).Execute(Factory, transaction, true) > 0;
    }

    public virtual IEnumerable<TEntity> Query(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? pageIndex = null, int? pageSize = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true)
    {
        return this.GetSqlForQuery(whereExpression, orderBy, pageIndex, pageSize, fieldExpression).Query<TEntity>(Factory, null, master);
    }
    public virtual IEnumerable<TEntity> QueryOffset(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? offset = null, int? rows = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true)
    {
        return this.GetSqlForQueryOffset(whereExpression, orderBy, offset, rows, fieldExpression).Query<TEntity>(Factory, null, master);
    }

    public virtual TEntity Get(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true)
    {
        return this.GetSqlForGet(whereExpression, fieldExpression).QueryFirstOrDefault<TEntity>(Factory, null, master);
    }

    public virtual int Count(Expression<Func<TEntity, bool>> whereExpression, bool master = true)
    {
        return this.GetSqlForCount(whereExpression).ExecuteScalar<int>(Factory, null, master);
    }

    public virtual bool IsTableExists(string tableName, bool master = true, bool useCache = true)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            return false;
        }

        if (useCache && TableInfoCache.Exists(tableName))
        {
            return true;
        }

        var exists = Execute(connection =>
        {
            var sql = SqlUtil.GetSqlForCountTable(DbType, connection.Database, tableName);
            return Factory.Get<int>(connection, sql) > 0;
        }, master);

        if (useCache && exists)
        {
            TableInfoCache.Add(tableName);
        }
        return exists;
    }

    public virtual bool IsTableFieldExists(string tableName, string fieldName, bool master = true, bool useCache = true)
    {
        if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(fieldName))
        {
            return false;
        }

        if (useCache && TableInfoCache.Exists(tableName, fieldName))
        {
            return true;
        }

        var exists = Execute(connection =>
        {
            var sql = SqlUtil.GetSqlForCountTableField(DbType, connection.Database, tableName, fieldName);
            return Factory.Get<int>(connection, sql) > 0;
        }, master);

        if (useCache && exists)
        {
            TableInfoCache.Add(tableName, fieldName);
        }
        return exists;
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
    #endregion

    #region Asynchronous method
#if NETSTANDARD || NET45_OR_GREATER
    public virtual async Task<bool> AddAsync(TEntity entity, bool returnAutoIncrementId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
    {
        if (entity == null) return false;

        PropertyInfo keyIdentityProperty;
        if (returnAutoIncrementId && (keyIdentityProperty = typeof(TEntity).GetKeyIdentityProperty()) != null)
        {
            var id = await this.GetSqlForAdd(entity, true, fieldExpression).ExecuteScalarAsync<long>(Factory, transaction, true);
            if (id < 1) return false;

            keyIdentityProperty.SetValue(entity, id, null);
            return true;
        }

        return await this.GetSqlForAdd(entity, false, fieldExpression).ExecuteAsync(Factory, transaction, true) > 0;
    }
    public virtual async Task<bool> AddAsync(IEnumerable<TEntity> entities, bool returnAutoIncrementId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
    {
        if (entities == null || !entities.Any()) return false;

        if (transaction?.Connection == null)
        {
            return await ExecuteAutoTransactionAsync(async trans =>
            {
                foreach (var entity in entities)
                {
                    if (!await AddAsync(entity, returnAutoIncrementId, fieldExpression, trans))
                    {
                        return false;
                    }
                }

                return true;
            });
        }

        foreach (var entity in entities)
        {
            if (!await AddAsync(entity, returnAutoIncrementId, fieldExpression, transaction))
            {
                return false;
            }
        }

        return true;
    }

    public virtual async Task<bool> AddOrUpdateAsync(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
    {
        if (entity == null) return false;

        switch (DbType)
        {
            case DatabaseType.MySql:
            case DatabaseType.SQLite:
                {
                    return await this.GetSqlForAddOrUpdate(entity, fieldExpression).ExecuteAsync(Factory, transaction, true) > 0;
                }
            default:
                {
                    if (await this.GetSqlForEntityExists(entity).ExecuteScalarAsync<int>(Factory, null, true) < 1)
                    {
                        // INSERT
                        return await AddAsync(entity, false, fieldExpression, transaction);
                    }

                    if (transaction?.Connection != null)
                    {
                        // DELETE
                        if (!await DeleteAsync(entity, transaction))
                        {
                            return false;
                        }

                        // INSERT
                        return await AddAsync(entity, false, fieldExpression, transaction);
                    }

                    return await ExecuteAutoTransactionAsync(async trans =>
                    {
                        // DELETE
                        if (!await DeleteAsync(entity, trans))
                        {
                            return false;
                        }

                        // INSERT
                        if (!await AddAsync(entity, false, fieldExpression, trans))
                        {
                            return false;
                        }

                        return true;
                    });
                }
        }
    }
    public virtual async Task<bool> AddOrUpdateAsync(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
    {
        if (entities == null || !entities.Any()) return false;

        if (transaction?.Connection == null)
        {
            return await ExecuteAutoTransactionAsync(async trans =>
            {
                foreach (var entity in entities)
                {
                    if (!await AddOrUpdateAsync(entity, fieldExpression, trans))
                    {
                        return false;
                    }
                }

                return true;
            });
        }

        foreach (var entity in entities)
        {
            if (!await AddOrUpdateAsync(entity, fieldExpression, transaction))
            {
                return false;
            }
        }

        return true;
    }

    public virtual async Task<bool> DeleteAsync(TEntity entity, IDbTransaction transaction = null)
    {
        return await this.GetSqlForDelete(entity).ExecuteAsync(Factory, transaction, true) > 0;
    }
    public virtual async Task<int> DeleteAsync(Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null)
    {
        return await this.GetSqlForDelete(whereExpression).ExecuteAsync(Factory, transaction, true);
    }

    public virtual async Task<int> UpdateAsync(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, Expression<Func<TEntity, bool>> whereExpression = null, IDbTransaction transaction = null)
    {
        return await this.GetSqlForUpdate(entity, fieldExpression, whereExpression).ExecuteAsync(Factory, transaction, true);
    }
    public virtual async Task<bool> UpdateAsync(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
    {
        if (entities == null || !entities.Any()) return false;

        if (transaction?.Connection == null)
        {
            return await ExecuteAutoTransactionAsync(async trans =>
            {
                foreach (var entity in entities)
                {
                    if (!(await UpdateAsync(entity, fieldExpression, null, trans) > 0))
                    {
                        return false;
                    }
                }

                return true;
            });
        }

        foreach (var entity in entities)
        {
            if (!(await UpdateAsync(entity, fieldExpression, null, transaction) > 0))
            {
                return false;
            }
        }

        return true;
    }

    public virtual async Task<bool> IncrAsync<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null) where TValue : struct
    {
        return await this.GetSqlForIncr(value, fieldExpression, whereExpression).ExecuteAsync(Factory, transaction, true) > 0;
    }
    public virtual async Task<bool> DecrAsync<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null) where TValue : struct
    {
        return await this.GetSqlForDecr(value, fieldExpression, whereExpression).ExecuteAsync(Factory, transaction, true) > 0;
    }

    public virtual async Task<IEnumerable<TEntity>> QueryAsync(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? pageIndex = null, int? pageSize = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true)
    {
        return await this.GetSqlForQuery(whereExpression, orderBy, pageIndex, pageSize, fieldExpression).QueryAsync<TEntity>(Factory, null, master);
    }
    public virtual async Task<IEnumerable<TEntity>> QueryOffsetAsync(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? offset = null, int? rows = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true)
    {
        return await this.GetSqlForQueryOffset(whereExpression, orderBy, offset, rows, fieldExpression).QueryAsync<TEntity>(Factory, null, master);
    }

    public virtual async Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true)
    {
        return await this.GetSqlForGet(whereExpression, fieldExpression).QueryFirstOrDefaultAsync<TEntity>(Factory, null, master);
    }

    public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>> whereExpression, bool master = true)
    {
        return await this.GetSqlForCount(whereExpression).ExecuteScalarAsync<int>(Factory, null, master);
    }

    public virtual async Task<bool> IsTableExistsAsync(string tableName, bool master = true, bool useCache = true)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            return false;
        }

        if (useCache && TableInfoCache.Exists(tableName))
        {
            return true;
        }

        var exists = await ExecuteAsync(async connection =>
        {
            var sql = SqlUtil.GetSqlForCountTable(DbType, connection.Database, tableName);
            return await Factory.GetAsync<int>(connection, sql) > 0;
        }, master);

        if (useCache && exists)
        {
            TableInfoCache.Add(tableName);
        }
        return exists;
    }

    public virtual async Task<bool> IsTableFieldExistsAsync(string tableName, string fieldName, bool master = true, bool useCache = true)
    {
        if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(fieldName))
        {
            return false;
        }

        if (useCache && TableInfoCache.Exists(tableName, fieldName))
        {
            return true;
        }

        var exists = await ExecuteAsync(async connection =>
        {
            var sql = SqlUtil.GetSqlForCountTableField(DbType, connection.Database, tableName, fieldName);
            return await Factory.GetAsync<int>(connection, sql) > 0;
        }, master);

        if (useCache && exists)
        {
            TableInfoCache.Add(tableName, fieldName);
        }
        return exists;
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
#endif
    #endregion
}