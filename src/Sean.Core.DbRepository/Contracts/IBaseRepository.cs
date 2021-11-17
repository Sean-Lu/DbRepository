using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using System.Transactions;
using Sean.Core.DbRepository.Factory;

namespace Sean.Core.DbRepository.Contracts
{
    public interface IBaseRepository
    {
        /// <summary>
        /// Database factory
        /// </summary>
        DbFactory Factory { get; }

        /// <summary>
        /// 执行
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <returns></returns>
        T Execute<T>(Func<IDbConnection, T> func, bool master = true);

        /// <summary>
        /// 执行事务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <returns></returns>
        T ExecuteTransaction<T>(Func<IDbTransaction, T> func);
        /// <summary>
        /// 执行事务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        T ExecuteTransaction<T>(IDbConnection connection, Func<IDbTransaction, T> func);

        /// <summary>
        /// <para><see cref="DbTransaction"/>、<see cref="TransactionScope"/>的区别：</para>
        /// <para><see cref="DbTransaction"/>：每个<see cref="DbTransaction"/>是基于每个<see cref="DbConnection"/>的。这种设计对于跨越多个程序集或者多个方法的事务行为来说，不是非常好，需要把事务和数据库连接作为参数传入。</para>
        /// <para><see cref="TransactionScope"/>：<see cref="TransactionScope"/>是基于当前线程的，在当前线程中，调用<see cref="Transaction.Current"/>方法可以看到当前事务的信息，调用<see cref="TransactionScope.Complete"/>方法提交事务。</para>
        /// </summary>
        /// <param name="toDoInTransactionScope"></param>
        /// <returns></returns>
        T ExecuteTransactionScope<T>(Func<TransactionScope, T> toDoInTransactionScope);

#if NETSTANDARD || NET45_OR_GREATER
        /// <summary>
        /// 异步执行
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <returns></returns>
        Task<T> ExecuteAsync<T>(Func<IDbConnection, Task<T>> func, bool master = true);

        /// <summary>
        /// 异步执行事务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <returns></returns>
        Task<T> ExecuteTransactionAsync<T>(Func<IDbTransaction, Task<T>> func);
        /// <summary>
        /// 异步执行事务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        Task<T> ExecuteTransactionAsync<T>(IDbConnection connection, Func<IDbTransaction, Task<T>> func);
#endif

#if NETSTANDARD || NET451_OR_GREATER
        /// <summary>
        /// <para><see cref="DbTransaction"/>、<see cref="TransactionScope"/>的区别：</para>
        /// <para><see cref="DbTransaction"/>：每个<see cref="DbTransaction"/>是基于每个<see cref="DbConnection"/>的。这种设计对于跨越多个程序集或者多个方法的事务行为来说，不是非常好，需要把事务和数据库连接作为参数传入。</para>
        /// <para><see cref="TransactionScope"/>：<see cref="TransactionScope"/>是基于当前线程的，在当前线程中，调用<see cref="Transaction.Current"/>方法可以看到当前事务的信息，调用<see cref="TransactionScope.Complete"/>方法提交事务。</para>
        /// </summary>
        /// <param name="toDoInTransactionScope"></param>
        /// <returns></returns>
        Task<T> ExecuteTransactionScopeAsync<T>(Func<TransactionScope, Task<T>> toDoInTransactionScope);
#endif
    }

    public interface IBaseRepository<TEntity> : IBaseRepository
    {
        /// <summary>
        /// 主表表名
        /// </summary>
        string MainTableName { get; }

        /// <summary>
        /// 表名：默认返回主表表名 <see cref="MainTableName"/>
        /// </summary>
        /// <returns></returns>
        string TableName();

        /// <summary>
        /// 返回创建表的SQL语句（在方法 <see cref="CreateTableIfNotExist"/> 中使用）
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        string CreateTableSql(string tableName);

        /// <summary>
        /// 如果表不存在，则执行SQL语句（<see cref="CreateTableSql"/>）来创建新表
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="master">true: 主库, false: 从库</param>
        void CreateTableIfNotExist(string tableName, bool master = true);

        /// <summary>
        /// 输出执行的SQL语句
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        void OutputExecutedSql(string sql, object param);

        /// <summary>
        /// <see cref="SqlFactory{TEntity}.Build(IBaseRepository, bool)"/>
        /// </summary>
        /// <param name="autoIncludeFields"></param>
        /// <returns></returns>
        SqlFactory<TEntity> NewSqlFactory(bool autoIncludeFields = true);

        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="returnId">是否返回自增主键Id</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        bool Add(TEntity entity, bool returnId = false, IDbTransaction transaction = null, int? commandTimeout = null);

        /// <summary>
        /// 批量新增
        /// </summary>
        /// <param name="entitys"></param>
        /// <param name="returnId">是否返回自增主键Id</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        bool Add(IList<TEntity> entitys, bool returnId = false, IDbTransaction transaction = null, int? commandTimeout = null);

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        bool Delete(TEntity entity, IDbTransaction transaction = null, int? commandTimeout = null);
        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        bool Delete(SqlFactory sqlFactory, IDbTransaction transaction = null, int? commandTimeout = null);

        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        bool Update(TEntity entity, IDbTransaction transaction = null, int? commandTimeout = null);
        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        bool Update(SqlFactory sqlFactory, IDbTransaction transaction = null, int? commandTimeout = null);

        /// <summary>
        /// 查询
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        IEnumerable<TEntity> Query(SqlFactory sqlFactory, bool master = true, int? commandTimeout = null);

        /// <summary>
        /// 统计数量
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        int Count(SqlFactory sqlFactory, bool master = true, int? commandTimeout = null);

        /// <summary>
        /// 查询指定的表是否存在
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <returns></returns>
        bool IsTableExists(string tableName, bool master = true);

#if NETSTANDARD || NET45_OR_GREATER
        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="returnId">是否返回自增主键Id</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        Task<bool> AddAsync(TEntity entity, bool returnId = false, IDbTransaction transaction = null, int? commandTimeout = null);

        /// <summary>
        /// 批量新增
        /// </summary>
        /// <param name="entitys"></param>
        /// <param name="returnId">是否返回自增主键Id</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        Task<bool> AddAsync(IList<TEntity> entitys, bool returnId = false, IDbTransaction transaction = null, int? commandTimeout = null);

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        Task<bool> DeleteAsync(TEntity entity, IDbTransaction transaction = null, int? commandTimeout = null);
        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        Task<bool> DeleteAsync(SqlFactory sqlFactory, IDbTransaction transaction = null, int? commandTimeout = null);

        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        Task<bool> UpdateAsync(TEntity entity, IDbTransaction transaction = null, int? commandTimeout = null);
        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        Task<bool> UpdateAsync(SqlFactory sqlFactory, IDbTransaction transaction = null, int? commandTimeout = null);

        /// <summary>
        /// 查询
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        Task<IEnumerable<TEntity>> QueryAsync(SqlFactory sqlFactory, bool master = true, int? commandTimeout = null);

        /// <summary>
        /// 统计数量
        /// </summary>
        /// <param name="sqlFactory"></param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <param name="commandTimeout">命令执行超时时间（单位：秒）</param>
        /// <returns></returns>
        Task<int> CountAsync(SqlFactory sqlFactory, bool master = true, int? commandTimeout = null);

        /// <summary>
        /// 查询指定的表是否存在
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="master">true: 主库, false: 从库</param>
        /// <returns></returns>
        Task<bool> IsTableExistsAsync(string tableName, bool master = true);
#endif
    }
}