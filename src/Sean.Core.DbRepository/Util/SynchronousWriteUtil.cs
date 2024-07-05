using System;
using System.Collections.Concurrent;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Sean.Core.DbRepository.Util;

public static class SynchronousWriteUtil
{
    private static readonly ConcurrentDictionary<string, SynchronousWriteLock> _databaseLocks = new();

    public static T CheckWriteLock<T>(IDbConnection connection, string sql, Func<T> func, IDbTransaction transaction = null)
    {
        var options = DbContextConfiguration.Options.SynchronousWriteOptions;
        if (!options.Enable || !SqlMatchUtil.IsWriteOperation(sql))
        {
            return func();// 未启用写入同步锁 或者 不是写入操作的SQL，可以直接执行
        }

        return UseWriteLock(options.LockTimeout, connection, func, transaction, options.OnLockTakenFailed);
    }
    public static T CheckWriteLock<T>(IDbConnection connection, Func<T> func, IDbTransaction transaction = null)
    {
        var options = DbContextConfiguration.Options.SynchronousWriteOptions;
        if (!options.Enable)
        {
            return func();// 未启用写入同步锁，可以直接执行
        }

        return UseWriteLock(options.LockTimeout, connection, func, transaction, options.OnLockTakenFailed);
    }

    public static async Task<T> CheckWriteLockAsync<T>(IDbConnection connection, string sql, Func<Task<T>> func, IDbTransaction transaction = null)
    {
        var options = DbContextConfiguration.Options.SynchronousWriteOptions;
        if (!options.Enable || !SqlMatchUtil.IsWriteOperation(sql))
        {
            return await func();// 未启用写入同步锁 或者 不是写入操作的SQL，可以直接执行
        }

        return await UseWriteLockAsync(options.LockTimeout, connection, func, transaction, options.OnLockTakenFailed);
    }
    public static async Task<T> CheckWriteLockAsync<T>(IDbConnection connection, Func<Task<T>> func, IDbTransaction transaction = null)
    {
        var options = DbContextConfiguration.Options.SynchronousWriteOptions;
        if (!options.Enable)
        {
            return await func();// 未启用写入同步锁，可以直接执行
        }

        return await UseWriteLockAsync(options.LockTimeout, connection, func, transaction, options.OnLockTakenFailed);
    }

    public static T UseWriteLock<T>(int lockTimeout, IDbConnection connection, Func<T> func, IDbTransaction transaction = null, Func<int, bool> onLockTakenFailed = null)
    {
        var locker = _databaseLocks.GetOrAdd(connection.ConnectionString, _ => new SynchronousWriteLock { Locker = new object() });
        if (locker.Connection != null)
        {
            if (locker.Connection == connection)
            {
                return func();// 使用同一个连接，不需要加锁，可以直接执行
            }
        }
        if (locker.Transaction != null)
        {
            if (locker.Transaction == transaction)
            {
                return func();// 使用同一个事务，不需要加锁，可以直接执行
            }
        }

        var lockAcquired = false;
        try
        {
            Monitor.TryEnter(locker.Locker, lockTimeout, ref lockAcquired);
            if (lockAcquired)
            {
                locker.Connection = connection;
                locker.Transaction = transaction;
            }
            else
            {
                if (onLockTakenFailed != null && !onLockTakenFailed(lockTimeout))
                {
                    return default;
                }
            }

            return func();
        }
        finally
        {
            if (lockAcquired)
            {
                locker.Connection = null;
                locker.Transaction = null;
                Monitor.Exit(locker.Locker);
            }
        }
    }
    public static async Task<T> UseWriteLockAsync<T>(int lockTimeout, IDbConnection connection, Func<Task<T>> func, IDbTransaction transaction = null, Func<int, bool> onLockTakenFailed = null)
    {
        var locker = _databaseLocks.GetOrAdd(connection.ConnectionString, _ => new SynchronousWriteLock { Locker = new object() });
        if (locker.Connection != null)
        {
            if (locker.Connection == connection)
            {
                return await func();// 使用同一个连接，不需要加锁，可以直接执行
            }
        }
        if (locker.Transaction != null)
        {
            if (locker.Transaction == transaction)
            {
                return await func();// 使用同一个事务，不需要加锁，可以直接执行
            }
        }

        var lockAcquired = false;
        try
        {
            Monitor.TryEnter(locker.Locker, lockTimeout, ref lockAcquired);
            if (lockAcquired)
            {
                locker.Connection = connection;
                locker.Transaction = transaction;
            }
            else
            {
                if (onLockTakenFailed != null && !onLockTakenFailed(lockTimeout))
                {
                    return default;
                }
            }

            return await func();
        }
        finally
        {
            if (lockAcquired)
            {
                locker.Connection = null;
                locker.Transaction = null;
                Monitor.Exit(locker.Locker);
            }
        }
    }
}

internal class SynchronousWriteLock
{
    public object Locker { get; set; }
    public IDbConnection Connection { get; set; }
    public IDbTransaction Transaction { get; set; }
}