using System;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sean.Core.DbRepository.Util;

namespace Sean.Core.DbRepository.Test;

/// <summary>
/// 连接选择和共享并发缓存的离线回归测试。
/// </summary>
[TestClass]
public class ConcurrencyCorrectnessTest
{
    [TestMethod]
    public void MultiConnectionSettings_RoundRobinSurvivesIntegerOverflow()
    {
        var master1 = CreateConnection("master1");
        var master2 = CreateConnection("master2");
        var master3 = CreateConnection("master3");
        var settings = new MultiConnectionSettings(new[] { master1, master2, master3 });

        // 保持既有轮询顺序：多连接场景第一次调用选择索引 1。
        Assert.AreSame(master2, settings.Get());
        Assert.AreSame(master3, settings.Get());
        Assert.AreSame(master1, settings.Get());

        var timesField = typeof(MultiConnectionSettings).GetField("_times", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(timesField);
        timesField.SetValue(settings, int.MaxValue);

        // int.MaxValue 加一会溢出为 int.MinValue；无符号取模仍必须得到合法索引。
        Assert.AreSame(master3, settings.Get());
    }

    [TestMethod]
    public void MultiConnectionSettings_PreservesRoleFallbackAndLiveListBehavior()
    {
        Assert.IsNull(new MultiConnectionSettings(Array.Empty<ConnectionStringOptions>()).Get());
        var singleSlave = CreateConnection("single-slave", false);
        Assert.AreSame(singleSlave, new MultiConnectionSettings(singleSlave).Get(),
            "A single connection must remain selectable regardless of its role.");

        var master1 = CreateConnection("master1");
        var master2 = CreateConnection("master2");
        var slave = CreateConnection("slave", false);
        var settings = new MultiConnectionSettings(new[] { master1, master2, slave });

        Assert.AreSame(slave, settings.Get(false));

        settings.ConnectionStrings.Remove(slave);
        Assert.AreSame(master2, settings.Get(false), "Without a slave, selection must fall back to the master list.");

        var replacement = CreateConnection("replacement");
        settings.ConnectionStrings.Clear();
        settings.ConnectionStrings.Add(replacement);
        Assert.AreSame(replacement, settings.Get(), "The public mutable list must remain live; no stale snapshot is allowed.");
    }

    [TestMethod]
    public void TableInfoCache_PreservesTableAndFieldSemantics()
    {
        var dbKey = $"cache-{Guid.NewGuid():N}";
        const string tableName = "TestTable";
        try
        {
            TableInfoCache.AddTable(dbKey, true, tableName);
            Assert.IsTrue(TableInfoCache.IsTableExists(dbKey, true, tableName));
            Assert.IsFalse(TableInfoCache.IsTableFieldExists(dbKey, true, tableName, "Id"));

            TableInfoCache.AddTableField(dbKey, true, tableName, "Id");
            TableInfoCache.AddTableField(dbKey, true, tableName, "Id");
            Assert.IsTrue(TableInfoCache.IsTableFieldExists(dbKey, true, tableName, "Id"));

            TableInfoCache.RemoveTableField(dbKey, true, tableName, "Id");
            Assert.IsTrue(TableInfoCache.IsTableExists(dbKey, true, tableName));
            Assert.IsFalse(TableInfoCache.IsTableFieldExists(dbKey, true, tableName, "Id"));

            TableInfoCache.AddTableField(dbKey, true, tableName, "Name");
            TableInfoCache.RemoveTable(dbKey, true, tableName);
            Assert.IsFalse(TableInfoCache.IsTableExists(dbKey, true, tableName));
            Assert.IsFalse(TableInfoCache.IsTableFieldExists(dbKey, true, tableName, "Name"));
        }
        finally
        {
            TableInfoCache.RemoveTable(dbKey, true, tableName);
        }
    }

    [TestMethod]
    public void TableInfoCache_ConcurrentFieldUpdatesDoNotLoseData()
    {
        var dbKey = $"cache-{Guid.NewGuid():N}";
        const string tableName = "ConcurrentTable";
        const int fieldCount = 2000;
        try
        {
            Parallel.For(0, fieldCount, index =>
            {
                var fieldName = $"Field{index}";
                TableInfoCache.AddTableField(dbKey, true, tableName, fieldName);
                TableInfoCache.AddTableField(dbKey, true, tableName, fieldName);
            });

            Assert.IsTrue(TableInfoCache.IsTableExists(dbKey, true, tableName));
            for (var index = 0; index < fieldCount; index++)
            {
                Assert.IsTrue(TableInfoCache.IsTableFieldExists(dbKey, true, tableName, $"Field{index}"));
            }

            Parallel.For(0, fieldCount / 2, index =>
                TableInfoCache.RemoveTableField(dbKey, true, tableName, $"Field{index * 2}"));

            for (var index = 0; index < fieldCount; index++)
            {
                Assert.AreEqual(index % 2 != 0,
                    TableInfoCache.IsTableFieldExists(dbKey, true, tableName, $"Field{index}"));
            }
        }
        finally
        {
            TableInfoCache.RemoveTable(dbKey, true, tableName);
        }
    }

    [TestMethod]
    public void DbOptions_TypeHandlersSupportConcurrentReplacementAndReads()
    {
        var options = new DbOptions();
        var type = typeof(TypeHandlerMarker);
        var handler1 = new StubTypeHandler();
        var handler2 = new StubTypeHandler();
        options.RemoveTypeHandler(type);
        try
        {
            options.AddTypeHandler(type, handler1);
            Assert.AreSame(handler1, options.GetTypeHandler(type));
            Assert.AreSame(handler1, new DbOptions().GetTypeHandler(type), "Type handlers must remain global across DbOptions instances.");
            options.AddTypeHandler(type, handler2);
            Assert.AreSame(handler2, options.GetTypeHandler(type));

            Parallel.For(0, 10000, index =>
            {
                options.AddTypeHandler(type, index % 2 == 0 ? handler1 : handler2);
                var current = options.GetTypeHandler(type);
                if (!ReferenceEquals(current, handler1) && !ReferenceEquals(current, handler2))
                {
                    throw new InvalidOperationException("A concurrent read observed an unknown type handler.");
                }
            });

            Assert.IsTrue(options.ContainsTypeHandler(type));
            Assert.IsTrue(options.RemoveTypeHandler(type));
            Assert.IsFalse(options.ContainsTypeHandler(type));
            Assert.IsNull(options.GetTypeHandler(type));
            Assert.IsFalse(options.RemoveTypeHandler(type));
        }
        finally
        {
            options.RemoveTypeHandler(type);
        }
    }

    [TestMethod]
    public void SynchronousWriteLock_PreservesReentrancySerializationAndTimeoutCallbacks()
    {
        var connectionString = $"lock-{Guid.NewGuid():N}";
        var ownerConnection = new StubDbConnection(connectionString);
        var waitingConnection = new StubDbConnection(connectionString);
        using var ownerEntered = new ManualResetEventSlim();
        using var releaseOwner = new ManualResetEventSlim();
        using var waiterStarted = new ManualResetEventSlim();
        using var waiterEntered = new ManualResetEventSlim();

        var ownerTask = Task.Run(() => SynchronousWriteUtil.UseDatabaseLock(5000, ownerConnection, () =>
        {
            ownerEntered.Set();
            Assert.IsTrue(releaseOwner.Wait(5000), "Timed out while waiting to release the test lock owner.");
            return 1;
        }));
        Assert.IsTrue(ownerEntered.Wait(5000), "The test lock owner did not start.");

        var waiterTask = Task.Run(() =>
        {
            waiterStarted.Set();
            return SynchronousWriteUtil.UseDatabaseLock(5000, waitingConnection, () =>
            {
                waiterEntered.Set();
                return 2;
            });
        });

        try
        {
            Assert.IsTrue(waiterStarted.Wait(5000), "The waiting task did not start.");
            Assert.IsFalse(waiterEntered.Wait(100), "A different connection entered before the owner released the lock.");

            var noCallbackExecuted = false;
            var noCallbackResult = SynchronousWriteUtil.UseDatabaseLock(0, waitingConnection, () =>
            {
                noCallbackExecuted = true;
                return 3;
            });
            Assert.IsTrue(noCallbackExecuted);
            Assert.AreEqual(3, noCallbackResult);

            var rejectedCallbackExecuted = false;
            var rejectedResult = SynchronousWriteUtil.UseDatabaseLock(0, waitingConnection, () =>
            {
                rejectedCallbackExecuted = true;
                return 4;
            }, onLockTakenFailed: _ => false);
            Assert.IsFalse(rejectedCallbackExecuted);
            Assert.AreEqual(default, rejectedResult);

            var acceptedCallbackExecuted = false;
            var acceptedResult = SynchronousWriteUtil.UseDatabaseLock(0, waitingConnection, () =>
            {
                acceptedCallbackExecuted = true;
                return 5;
            }, onLockTakenFailed: _ => true);
            Assert.IsTrue(acceptedCallbackExecuted);
            Assert.AreEqual(5, acceptedResult);
        }
        finally
        {
            releaseOwner.Set();
        }

        Assert.AreEqual(1, ownerTask.GetAwaiter().GetResult());
        Assert.AreEqual(2, waiterTask.GetAwaiter().GetResult());

        var nestedResult = SynchronousWriteUtil.UseDatabaseLock(1000, ownerConnection, () =>
            SynchronousWriteUtil.UseDatabaseLock(0, ownerConnection, () => 42));
        Assert.AreEqual(42, nestedResult);

        var transaction = new StubDbTransaction(ownerConnection);
        var transactionNestedResult = SynchronousWriteUtil.UseDatabaseLock(1000, ownerConnection, () =>
            SynchronousWriteUtil.UseDatabaseLock(0, waitingConnection, () => 43, transaction), transaction);
        Assert.AreEqual(43, transactionNestedResult);
    }

    [TestMethod]
    public async Task SynchronousWriteLockAsync_PreservesReentrancyAndRejectedCallbackBehavior()
    {
        var connectionString = $"lock-{Guid.NewGuid():N}";
        var ownerConnection = new StubDbConnection(connectionString);
        var waitingConnection = new StubDbConnection(connectionString);
        var ownerEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseOwner = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var ownerTask = SynchronousWriteUtil.UseDatabaseLockAsync(5000, ownerConnection, async () =>
        {
            ownerEntered.SetResult(true);
            await releaseOwner.Task;
            return 1;
        });
        await ownerEntered.Task;

        var callbackExecuted = false;
        var rejectedResult = await SynchronousWriteUtil.UseDatabaseLockAsync(0, waitingConnection, async () =>
        {
            callbackExecuted = true;
            await Task.Yield();
            return 2;
        }, onLockTakenFailed: _ => false);
        Assert.IsFalse(callbackExecuted);
        Assert.AreEqual(default, rejectedResult);

        releaseOwner.SetResult(true);
        Assert.AreEqual(1, await ownerTask);

        var nestedResult = await SynchronousWriteUtil.UseDatabaseLockAsync(1000, ownerConnection, () =>
            SynchronousWriteUtil.UseDatabaseLockAsync(0, ownerConnection, () => Task.FromResult(42)));
        Assert.AreEqual(42, nestedResult);
    }

    private static ConnectionStringOptions CreateConnection(string name, bool master = true)
    {
        return new ConnectionStringOptions($"Data Source={name}", DatabaseType.SQLite, master);
    }

    private sealed class TypeHandlerMarker
    {
    }

    private sealed class StubTypeHandler : ITypeHandler
    {
        public void Set(DbParameter dbParameter, object value, DatabaseType databaseType)
        {
        }
    }

    private sealed class StubDbConnection : IDbConnection
    {
        public StubDbConnection(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public string ConnectionString { get; set; }
        public int ConnectionTimeout => 0;
        public string Database => string.Empty;
        public ConnectionState State => ConnectionState.Open;
        public IDbTransaction BeginTransaction() => throw new NotSupportedException();
        public IDbTransaction BeginTransaction(IsolationLevel il) => throw new NotSupportedException();
        public void ChangeDatabase(string databaseName) => throw new NotSupportedException();
        public void Close()
        {
        }
        public IDbCommand CreateCommand() => throw new NotSupportedException();
        public void Open()
        {
        }
        public void Dispose()
        {
        }
    }

    private sealed class StubDbTransaction : IDbTransaction
    {
        public StubDbTransaction(IDbConnection connection)
        {
            Connection = connection;
        }

        public IDbConnection Connection { get; }
        public IsolationLevel IsolationLevel => IsolationLevel.Unspecified;
        public void Commit()
        {
        }
        public void Rollback()
        {
        }
        public void Dispose()
        {
        }
    }
}
