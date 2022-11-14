using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Sean.Core.DbRepository.Dapper.Extensions;
using Sean.Core.DbRepository.Extensions;
using Sean.Core.DbRepository.Util;
using System.Linq;
#if NETSTANDARD
using Microsoft.Extensions.Configuration;
#endif

namespace Sean.Core.DbRepository.Dapper
{
    /// <summary>
    /// Database table entity base repository.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public abstract class BaseRepository<TEntity> : EntityBaseRepository<TEntity> where TEntity : class
    {
        #region Constructors
        /// <summary>
        /// Single or clustered database.
        /// </summary>
        protected BaseRepository() : base()
        {
        }
#if NETSTANDARD
        /// <summary>
        /// Single or clustered database.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="configName">Configuration ConnectionStrings name</param>
        protected BaseRepository(IConfiguration configuration, string configName = Constants.Master) : base(configuration, configName)
        {
        }
#else
        /// <summary>
        /// Single or clustered database.
        /// </summary>
        /// <param name="configName">Configuration ConnectionStrings name</param>
        protected BaseRepository(string configName) : base(configName)
        {
        }
#endif
        /// <summary>
        /// Single or clustered database.
        /// </summary>
        /// <param name="connectionSettings"></param>
        protected BaseRepository(MultiConnectionSettings connectionSettings) : base(connectionSettings)
        {
        }
        /// <summary>
        /// Single database.
        /// </summary>
        /// <param name="connString"></param>
        /// <param name="type"></param>
        protected BaseRepository(string connString, DatabaseType type) : base(connString, type)
        {

        }
        /// <summary>
        /// Single database.
        /// </summary>
        /// <param name="connString"></param>
        /// <param name="factory"></param>
        protected BaseRepository(string connString, DbProviderFactory factory) : base(connString, factory)
        {

        }
        /// <summary>
        /// Single database.
        /// </summary>
        /// <param name="connString"></param>
        /// <param name="providerName"></param>
        protected BaseRepository(string connString, string providerName) : base(connString, providerName)
        {

        }
        #endregion

        #region Synchronous method
        public override int Execute(string sql, object param = null, IDbTransaction transaction = null, bool master = true)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

            return new DefaultSqlWithParameter
            {
                Sql = sql,
                Parameter = param
            }.Execute(this, transaction, master);
        }
        public override IEnumerable<T> Query<T>(string sql, object param = null, IDbTransaction transaction = null, bool master = true)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

            return new DefaultSqlWithParameter
            {
                Sql = sql,
                Parameter = param
            }.Query<T>(this, transaction, master);
        }
        public override T QueryFirstOrDefault<T>(string sql, object param = null, IDbTransaction transaction = null, bool master = true)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

            return new DefaultSqlWithParameter
            {
                Sql = sql,
                Parameter = param
            }.QueryFirstOrDefault<T>(this, transaction, master);
        }
        public override T ExecuteScalar<T>(string sql, object param = null, IDbTransaction transaction = null, bool master = true)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

            return new DefaultSqlWithParameter
            {
                Sql = sql,
                Parameter = param
            }.ExecuteScalar<T>(this, transaction, master);
        }

        public override bool Add(TEntity entity, bool returnAutoIncrementId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
        {
            if (entity == null) return false;

            PropertyInfo keyIdentityProperty;
            if (returnAutoIncrementId && (keyIdentityProperty = typeof(TEntity).GetKeyIdentityProperty()) != null)
            {
                var id = this.GetSqlForAdd(entity, true, fieldExpression).ExecuteScalar<long>(this, transaction, true);
                if (id < 1) return false;

                keyIdentityProperty.SetValue(entity, id, null);
                return true;
            }

            return this.GetSqlForAdd(entity, false, fieldExpression).Execute(this, transaction, true) > 0;
        }
        public override bool Add(IEnumerable<TEntity> entities, bool returnAutoIncrementId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
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

            return this.CreateInsertableBuilder(fieldExpression == null)
                .IncludeFields(fieldExpression)
                //.ReturnAutoIncrementId(returnAutoIncrementId)
                .SetParameter(entities)// BulkInsert
                .Build()
                .Execute(this, transaction, true) > 0;
        }

        public override bool AddOrUpdate(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
        {
            if (entity == null) return false;

            switch (DbType)
            {
                case DatabaseType.MySql:
                case DatabaseType.SQLite:
                    {
                        return this.GetSqlForAddOrUpdate(entity, fieldExpression).Execute(this, transaction, true) > 0;
                    }
                default:
                    {
                        if (this.GetSqlForEntityExists(entity).ExecuteScalar<int>(this, null, true) < 1)
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
        public override bool AddOrUpdate(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
        {
            if (entities == null || !entities.Any()) return false;

            switch (DbType)
            {
                case DatabaseType.MySql:
                case DatabaseType.SQLite:
                    return this.CreateReplaceableBuilder(fieldExpression == null)
                        .IncludeFields(fieldExpression)
                        .SetParameter(entities)
                        .Build()
                        .Execute(this, transaction, true) > 0;
                default:
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

        public override bool Delete(TEntity entity, IDbTransaction transaction = null)
        {
            return this.GetSqlForDelete(entity).Execute(this, transaction, true) > 0;
        }
        public override int Delete(Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null)
        {
            return this.GetSqlForDelete(whereExpression).Execute(this, transaction, true);
        }

        public override int Update(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, Expression<Func<TEntity, bool>> whereExpression = null, IDbTransaction transaction = null)
        {
            return this.GetSqlForUpdate(entity, fieldExpression, whereExpression).Execute(this, transaction, true);
        }
        public override bool Update(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
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

        public override bool Incr<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null) where TValue : struct
        {
            return this.GetSqlForIncr(value, fieldExpression, whereExpression).Execute(this, transaction, true) > 0;
        }
        public override bool Decr<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null) where TValue : struct
        {
            return this.GetSqlForDecr(value, fieldExpression, whereExpression).Execute(this, transaction, true) > 0;
        }

        public override IEnumerable<TEntity> Query(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? pageIndex = null, int? pageSize = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true)
        {
            return this.GetSqlForQuery(whereExpression, orderBy, pageIndex, pageSize, fieldExpression).Query<TEntity>(this, null, master);
        }
        public override IEnumerable<TEntity> QueryOffset(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? offset = null, int? rows = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true)
        {
            return this.GetSqlForQueryOffset(whereExpression, orderBy, offset, rows, fieldExpression).Query<TEntity>(this, null, master);
        }

        public override TEntity Get(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true)
        {
            return this.GetSqlForGet(whereExpression, fieldExpression).QueryFirstOrDefault<TEntity>(this, null, master);
        }

        public override int Count(Expression<Func<TEntity, bool>> whereExpression, bool master = true)
        {
            return this.GetSqlForCount(whereExpression).ExecuteScalar<int>(this, null, master);
        }

        public override bool IsTableExists(string tableName, bool master = true, bool useCache = true)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return false;
            }

            if (useCache && TableInfoCache.Exists(tableName))
            {
                return true;
            }

            var exist = Execute(connection =>
            {
                ISqlWithParameter sql = new DefaultSqlWithParameter
                {
                    Sql = SqlUtil.GetSqlForCountTable(this.DbType, connection.Database, tableName)
                };
                return sql.ExecuteScalar<int>(connection, null, this, this.CommandTimeout) > 0;
            }, master);

            if (useCache && exist)
            {
                TableInfoCache.Add(tableName);
            }
            return exist;

        }

        public override bool IsTableFieldExists(string tableName, string fieldName, bool master = true, bool useCache = true)
        {
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            if (useCache && TableInfoCache.Exists(tableName, fieldName))
            {
                return true;
            }

            var exist = Execute(connection =>
            {
                ISqlWithParameter sql = new DefaultSqlWithParameter
                {
                    Sql = SqlUtil.GetSqlForCountTableField(this.DbType, connection.Database, tableName, fieldName)
                };
                return sql.ExecuteScalar<int>(connection, null, this, this.CommandTimeout) > 0;
            }, master);

            if (useCache && exist)
            {
                TableInfoCache.Add(tableName, fieldName);
            }
            return exist;
        }
        #endregion

        #region Asynchronous method
#if NETSTANDARD || NET45_OR_GREATER
        public override async Task<int> ExecuteAsync(string sql, object param = null, IDbTransaction transaction = null, bool master = true)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

            return await new DefaultSqlWithParameter
            {
                Sql = sql,
                Parameter = param
            }.ExecuteAsync(this, transaction, master);
        }
        public override async Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null, IDbTransaction transaction = null, bool master = true)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

            return await new DefaultSqlWithParameter
            {
                Sql = sql,
                Parameter = param
            }.QueryAsync<T>(this, transaction, master);
        }
        public override async Task<T> QueryFirstOrDefaultAsync<T>(string sql, object param = null, IDbTransaction transaction = null, bool master = true)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

            return await new DefaultSqlWithParameter
            {
                Sql = sql,
                Parameter = param
            }.QueryFirstOrDefaultAsync<T>(this, transaction, master);
        }
        public override async Task<T> ExecuteScalarAsync<T>(string sql, object param = null, IDbTransaction transaction = null, bool master = true)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sql));

            return await new DefaultSqlWithParameter
            {
                Sql = sql,
                Parameter = param
            }.ExecuteScalarAsync<T>(this, transaction, master);
        }

        public override async Task<bool> AddAsync(TEntity entity, bool returnAutoIncrementId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
        {
            if (entity == null) return false;

            PropertyInfo keyIdentityProperty;
            if (returnAutoIncrementId && (keyIdentityProperty = typeof(TEntity).GetKeyIdentityProperty()) != null)
            {
                var id = await this.GetSqlForAdd(entity, true, fieldExpression).ExecuteScalarAsync<long>(this, transaction, true);
                if (id < 1) return false;

                keyIdentityProperty.SetValue(entity, id, null);
                return true;
            }

            return await this.GetSqlForAdd(entity, false, fieldExpression).ExecuteAsync(this, transaction, true) > 0;
        }
        public override async Task<bool> AddAsync(IEnumerable<TEntity> entities, bool returnAutoIncrementId = false, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
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

            return await this.CreateInsertableBuilder(fieldExpression == null)
                .IncludeFields(fieldExpression)
                //.ReturnAutoIncrementId(returnAutoIncrementId)
                .SetParameter(entities)// BulkInsert
                .Build()
                .ExecuteAsync(this, transaction, true) > 0;
        }

        public override async Task<bool> AddOrUpdateAsync(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
        {
            if (entity == null) return false;

            switch (DbType)
            {
                case DatabaseType.MySql:
                case DatabaseType.SQLite:
                    {
                        return await this.GetSqlForAddOrUpdate(entity, fieldExpression).ExecuteAsync(this, transaction, true) > 0;
                    }
                default:
                    {
                        if (await this.GetSqlForEntityExists(entity).ExecuteScalarAsync<int>(this, null, true) < 1)
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
        public override async Task<bool> AddOrUpdateAsync(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
        {
            if (entities == null || !entities.Any()) return false;

            switch (DbType)
            {
                case DatabaseType.MySql:
                case DatabaseType.SQLite:
                    return await this.CreateReplaceableBuilder(fieldExpression == null)
                        .IncludeFields(fieldExpression)
                        .SetParameter(entities)
                        .Build()
                        .ExecuteAsync(this, transaction, true) > 0;
                default:
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

        public override async Task<bool> DeleteAsync(TEntity entity, IDbTransaction transaction = null)
        {
            return await this.GetSqlForDelete(entity).ExecuteAsync(this, transaction, true) > 0;
        }
        public override async Task<int> DeleteAsync(Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null)
        {
            return await this.GetSqlForDelete(whereExpression).ExecuteAsync(this, transaction, true);
        }

        public override async Task<int> UpdateAsync(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null, Expression<Func<TEntity, bool>> whereExpression = null, IDbTransaction transaction = null)
        {
            return await this.GetSqlForUpdate(entity, fieldExpression, whereExpression).ExecuteAsync(this, transaction, true);
        }
        public override async Task<bool> UpdateAsync(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> fieldExpression = null, IDbTransaction transaction = null)
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

        public override async Task<bool> IncrAsync<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null) where TValue : struct
        {
            return await this.GetSqlForIncr(value, fieldExpression, whereExpression).ExecuteAsync(this, transaction, true) > 0;
        }
        public override async Task<bool> DecrAsync<TValue>(TValue value, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, bool>> whereExpression, IDbTransaction transaction = null) where TValue : struct
        {
            return await this.GetSqlForDecr(value, fieldExpression, whereExpression).ExecuteAsync(this, transaction, true) > 0;
        }

        public override async Task<IEnumerable<TEntity>> QueryAsync(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? pageIndex = null, int? pageSize = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true)
        {
            return await this.GetSqlForQuery(whereExpression, orderBy, pageIndex, pageSize, fieldExpression).QueryAsync<TEntity>(this, null, master);
        }
        public override async Task<IEnumerable<TEntity>> QueryOffsetAsync(Expression<Func<TEntity, bool>> whereExpression, OrderByCondition orderBy = null, int? offset = null, int? rows = null, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true)
        {
            return await this.GetSqlForQueryOffset(whereExpression, orderBy, offset, rows, fieldExpression).QueryAsync<TEntity>(this, null, master);
        }

        public override async Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, object>> fieldExpression = null, bool master = true)
        {
            return await this.GetSqlForGet(whereExpression, fieldExpression).QueryFirstOrDefaultAsync<TEntity>(this, null, master);
        }

        public override async Task<int> CountAsync(Expression<Func<TEntity, bool>> whereExpression, bool master = true)
        {
            return await this.GetSqlForCount(whereExpression).ExecuteScalarAsync<int>(this, null, master);
        }

        public override async Task<bool> IsTableExistsAsync(string tableName, bool master = true, bool useCache = true)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return false;
            }

            if (useCache && TableInfoCache.Exists(tableName))
            {
                return true;
            }

            var exist = await ExecuteAsync(async connection =>
            {
                ISqlWithParameter sql = new DefaultSqlWithParameter
                {
                    Sql = SqlUtil.GetSqlForCountTable(this.DbType, connection.Database, tableName)
                };
                return await sql.ExecuteScalarAsync<int>(connection, null, this, this.CommandTimeout) > 0;
            }, master);

            if (useCache && exist)
            {
                TableInfoCache.Add(tableName);
            }
            return exist;
        }

        public override async Task<bool> IsTableFieldExistsAsync(string tableName, string fieldName, bool master = true, bool useCache = true)
        {
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            if (useCache && TableInfoCache.Exists(tableName, fieldName))
            {
                return true;
            }

            var exist = await ExecuteAsync(async connection =>
            {
                ISqlWithParameter sql = new DefaultSqlWithParameter
                {
                    Sql = SqlUtil.GetSqlForCountTableField(this.DbType, connection.Database, tableName, fieldName)
                };
                return await sql.ExecuteScalarAsync<int>(connection, null, this, this.CommandTimeout) > 0;
            }, master);

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
