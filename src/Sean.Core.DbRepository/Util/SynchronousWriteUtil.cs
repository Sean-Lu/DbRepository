using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Sean.Core.DbRepository.Util;

public static class SynchronousWriteUtil
{
    private static readonly ConcurrentDictionary<string, object> _databaseLocks = new();

    public static T CheckWriteLock<T>(SynchronousWriteOptions options, string connectionString, string sql, Func<T> func)
    {
        if (!options.Enable || !SqlMatchUtil.IsWriteOperation(sql))
        {
            return func();
        }

        var lockObject = _databaseLocks.GetOrAdd(connectionString, _ => new object());

        var lockAcquired = false;
        try
        {
            Monitor.TryEnter(lockObject, options.LockTimeout, ref lockAcquired);
            if (!lockAcquired)
            {
                if (options.OnLockTimeout != null && !options.OnLockTimeout.Invoke(sql))
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
                Monitor.Exit(lockObject);
            }
        }
    }

    public static async Task<T> CheckWriteLockAsync<T>(SynchronousWriteOptions options, string connectionString, string sql, Func<Task<T>> func)
    {
        if (!options.Enable || !SqlMatchUtil.IsWriteOperation(sql))
        {
            return await func();
        }

        var lockObject = _databaseLocks.GetOrAdd(connectionString, _ => new object());

        var lockAcquired = false;
        try
        {
            Monitor.TryEnter(lockObject, options.LockTimeout, ref lockAcquired);
            if (!lockAcquired)
            {
                if (options.OnLockTimeout != null && !options.OnLockTimeout.Invoke(sql))
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
                Monitor.Exit(lockObject);
            }
        }
    }
}