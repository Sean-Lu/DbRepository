using System;
using System.Collections.Concurrent;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Sean.Core.DbRepository.Util;

public static class SynchronousWriteUtil
{
    private static readonly ConcurrentDictionary<string, SynchronousWriteLock> _databaseLocks = new();

    public static T UseDatabaseWriteLock<T>(IDbConnection connection, string sql, Func<T> func, IDbTransaction transaction = null)
    {
        var options = DbContextConfiguration.Options.SynchronousWriteOptions;
        if (!options.Enable || !SqlMatchUtil.IsWriteOperation(sql))
        {
            return func();// 未启用写入同步锁 或者 不是写入操作的SQL，可以直接执行
        }

        return UseDatabaseLock(options.LockTimeout, connection, func, transaction, options.OnLockTakenFailed);
    }
    public static T UseDatabaseLock<T>(IDbConnection connection, Func<T> func, IDbTransaction transaction = null)
    {
        var options = DbContextConfiguration.Options.SynchronousWriteOptions;
        if (!options.Enable)
        {
            return func();// 未启用写入同步锁，可以直接执行
        }

        return UseDatabaseLock(options.LockTimeout, connection, func, transaction, options.OnLockTakenFailed);
    }

    public static async Task<T> UseDatabaseWriteLockAsync<T>(IDbConnection connection, string sql, Func<Task<T>> func, IDbTransaction transaction = null)
    {
        var options = DbContextConfiguration.Options.SynchronousWriteOptions;
        if (!options.Enable || !SqlMatchUtil.IsWriteOperation(sql))
        {
            return await func();// 未启用写入同步锁 或者 不是写入操作的SQL，可以直接执行
        }

        return await UseDatabaseLockAsync(options.LockTimeout, connection, func, transaction, options.OnLockTakenFailed);
    }
    public static async Task<T> UseDatabaseLockAsync<T>(IDbConnection connection, Func<Task<T>> func, IDbTransaction transaction = null)
    {
        var options = DbContextConfiguration.Options.SynchronousWriteOptions;
        if (!options.Enable)
        {
            return await func();// 未启用写入同步锁，可以直接执行
        }

        return await UseDatabaseLockAsync(options.LockTimeout, connection, func, transaction, options.OnLockTakenFailed);
    }

    public static T UseDatabaseLock<T>(int lockTimeout, IDbConnection connection, Func<T> func, IDbTransaction transaction = null, Func<int, bool> onLockTakenFailed = null)
    {
        var locker = _databaseLocks.GetOrAdd(connection.ConnectionString, _ => new SynchronousWriteLock { Semaphore = new SemaphoreSlim(1, 1) });
        if (IsReentrant(locker, connection, transaction))
        {
            return func();// 使用同一个连接或事务，不需要加锁，可以直接执行
        }

        var lockAcquired = false;
        try
        {
            lockAcquired = locker.Semaphore.Wait(lockTimeout);
            if (lockAcquired)
            {
                Interlocked.Exchange(ref locker.Owner, new SynchronousWriteLockOwner(connection, transaction));
            }
            else if (onLockTakenFailed != null && !onLockTakenFailed(lockTimeout))
            {
                return default;
            }

            return func();
        }
        finally
        {
            if (lockAcquired)
            {
                Interlocked.Exchange(ref locker.Owner, null);
                locker.Semaphore.Release();
            }
        }
    }
    public static async Task<T> UseDatabaseLockAsync<T>(int lockTimeout, IDbConnection connection, Func<Task<T>> func, IDbTransaction transaction = null, Func<int, bool> onLockTakenFailed = null)
    {
        var locker = _databaseLocks.GetOrAdd(connection.ConnectionString, _ => new SynchronousWriteLock { Semaphore = new SemaphoreSlim(1, 1) });
        if (IsReentrant(locker, connection, transaction))
        {
            return await func();// 使用同一个连接或事务，不需要加锁，可以直接执行
        }

        var lockAcquired = false;
        try
        {
            lockAcquired = await locker.Semaphore.WaitAsync(lockTimeout);
            if (lockAcquired)
            {
                Interlocked.Exchange(ref locker.Owner, new SynchronousWriteLockOwner(connection, transaction));
            }
            else if (onLockTakenFailed != null && !onLockTakenFailed(lockTimeout))
            {
                return default;
            }

            //var curThreadId = Thread.CurrentThread.ManagedThreadId;
            return await func();
        }
        finally
        {
            if (lockAcquired)
            {
                //var curThreadId = Thread.CurrentThread.ManagedThreadId;
                Interlocked.Exchange(ref locker.Owner, null);
                locker.Semaphore.Release();
            }
        }
    }

    private static bool IsReentrant(SynchronousWriteLock locker, IDbConnection connection, IDbTransaction transaction)
    {
        var owner = Interlocked.CompareExchange(ref locker.Owner, null, null);
        return owner != null
               && (ReferenceEquals(owner.Connection, connection)
                   || owner.Transaction != null && ReferenceEquals(owner.Transaction, transaction));
    }
}

internal class SynchronousWriteLock
{
    public SemaphoreSlim Semaphore { get; set; }
    public SynchronousWriteLockOwner Owner;
}

internal sealed class SynchronousWriteLockOwner
{
    public SynchronousWriteLockOwner(IDbConnection connection, IDbTransaction transaction)
    {
        Connection = connection;
        Transaction = transaction;
    }

    public IDbConnection Connection { get; }
    public IDbTransaction Transaction { get; }
}
