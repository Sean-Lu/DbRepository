using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sean.Core.DbRepository.Dapper;

namespace Sean.Core.DbRepository.Test;

/// <summary>
/// 连接释放和 AddOrUpdate 事务语义的回归测试。
/// </summary>
[TestClass]
public class ConnectionAndTransactionCorrectnessTest
{
    [TestMethod]
    public async Task ExecuteReader_WhenInternalExecutionFails_DisposesEveryInternalConnection()
    {
        var provider = new TrackingDbProviderFactory();
        var factory = CreateFactory(provider);

        AssertThrowsAndDisposesConnection(() => factory.ExecuteReader("SELECT 1"), provider);
        AssertThrowsAndDisposesConnection(() => factory.ExecuteReader("Data Source=direct", "SELECT 1"), provider);
        AssertThrowsAndDisposesConnection(() => factory.ExecuteReader(new DefaultSqlCommand("SELECT 1")), provider);

        await AssertThrowsAndDisposesConnectionAsync(() => factory.ExecuteReaderAsync("SELECT 1"), provider);
        await AssertThrowsAndDisposesConnectionAsync(() => factory.ExecuteReaderAsync("Data Source=direct", "SELECT 1"), provider);
        await AssertThrowsAndDisposesConnectionAsync(() => factory.ExecuteReaderAsync(new DefaultSqlCommand("SELECT 1")), provider);

        provider.ThrowOnCommandDispose = true;
        AssertThrowsAndDisposesConnection(() => factory.ExecuteReader(new DefaultSqlCommand("SELECT 1")), provider);
    }

    [TestMethod]
    public void OpenNewConnection_WhenOpenFails_DisposesCreatedConnection()
    {
        var provider = new TrackingDbProviderFactory { ThrowOnOpen = true };
        var factory = CreateFactory(provider);

        AssertThrowsAndDisposesConnection(() => factory.OpenNewConnection(), provider);
        AssertThrowsAndDisposesConnection(() => factory.OpenNewConnection("Data Source=direct"), provider);
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task CommandCreation_WhenOpenFails_DisposesCommandAndPreservesConnectionOwnership(bool asynchronous, bool cleanupFails)
    {
        var provider = new TrackingDbProviderFactory { ThrowOnOpen = true, ThrowOnCommandDispose = cleanupFails };
        var factory = CreateFactory(provider);
        using var external = provider.CreateTrackingConnection("Data Source=external");
        foreach (var route in new[] { "external", "command", "internal" })
        {
            async Task Execute()
            {
                if (asynchronous)
                {
                    if (route == "external") await factory.ExecuteScalarAsync(external, "SELECT 1");
                    else if (route == "command") await factory.ExecuteScalarAsync(new DefaultSqlCommand("SELECT 1") { Connection = external });
                    else await factory.ExecuteScalarAsync("SELECT 1");
                }
                else
                {
                    if (route == "external") factory.ExecuteScalar(external, "SELECT 1");
                    else if (route == "command") factory.ExecuteScalar(new DefaultSqlCommand("SELECT 1") { Connection = external });
                    else factory.ExecuteScalar("SELECT 1");
                }
            }
            var error = await Assert.ThrowsAsync<InvalidOperationException>(Execute);
            Assert.AreEqual("模拟连接打开失败。", error.Message);
            Assert.IsTrue(provider.Commands.Last().IsDisposed, route);
            Assert.IsFalse(external.IsDisposed);
            if (route == "internal") Assert.IsTrue(provider.Connections.Last().IsDisposed);
        }
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task CommandCreation_WhenParameterEnumerationFails_DisposesPartialCommand(bool asynchronous, bool cleanupFails)
    {
        var provider = new TrackingDbProviderFactory { ThrowOnCommandDispose = cleanupFails };
        var factory = CreateFactory(provider);
        using var external = provider.CreateTrackingConnection("Data Source=external");
        var expected = new InvalidOperationException("模拟参数枚举失败");
        IEnumerable<DbParameter> Parameters()
        {
            yield return new TrackingDbParameter { ParameterName = "Value", Value = 1 };
            throw expected;
        }
        var actual = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            if (asynchronous) await factory.ExecuteScalarAsync(external, "SELECT 1", Parameters());
            else factory.ExecuteScalar(external, "SELECT 1", Parameters());
        });
        Assert.AreSame(expected, actual);
        Assert.AreEqual(1, provider.Commands.Single().Parameters.Count);
        Assert.IsTrue(provider.Commands.Single().IsDisposed);
        Assert.IsFalse(external.IsDisposed);
        Assert.AreEqual(ConnectionState.Closed, external.State);
    }

    [TestMethod]
    public async Task ExecuteReader_WhenSuccessful_KeepsInternalConnectionUntilReaderIsDisposed()
    {
        var factory = new DbFactory("Data Source=:memory:;Version=3;New=True;", SQLiteFactory.Instance);

        using (var reader = factory.ExecuteReader("SELECT 1"))
        {
            Assert.IsTrue(reader.Read());
            Assert.AreEqual(1, Convert.ToInt32(reader.GetValue(0)));
        }

        using (var reader = factory.ExecuteReader(new DefaultSqlCommand("SELECT 2")))
        {
            Assert.IsTrue(reader.Read());
            Assert.AreEqual(2, Convert.ToInt32(reader.GetValue(0)));
        }

        using (var reader = await factory.ExecuteReaderAsync("SELECT 3"))
        {
            Assert.IsTrue(await reader.ReadAsync());
            Assert.AreEqual(3, Convert.ToInt32(reader.GetValue(0)));
        }

        using (var reader = await factory.ExecuteReaderAsync(new DefaultSqlCommand("SELECT 4")))
        {
            Assert.IsTrue(await reader.ReadAsync());
            Assert.AreEqual(4, Convert.ToInt32(reader.GetValue(0)));
        }
    }

    [TestMethod]
    public async Task ExecuteReader_WhenUsingCallerConnection_DoesNotDisposeItOnFailure()
    {
        var provider = new TrackingDbProviderFactory();
        var factory = CreateFactory(provider);
        var syncConnection = provider.CreateTrackingConnection("Data Source=external-sync");

        Assert.Throws<InvalidOperationException>(() => factory.ExecuteReader(syncConnection, "SELECT 1"));
        Assert.IsFalse(syncConnection.IsDisposed, "调用方传入的同步连接不能由 DbFactory 释放。");
        Assert.AreEqual(ConnectionState.Open, syncConnection.State);
        syncConnection.Dispose();

        var asyncConnection = provider.CreateTrackingConnection("Data Source=external-async");
        await AssertThrowsInvalidOperationAsync(() => factory.ExecuteReaderAsync(
            new DefaultSqlCommand("SELECT 1") { Connection = asyncConnection }));
        Assert.IsFalse(asyncConnection.IsDisposed, "调用方传入的异步连接不能由 DbFactory 释放。");
        Assert.AreEqual(ConnectionState.Open, asyncConnection.State);
        asyncConnection.Dispose();
    }

    [TestMethod]
    public void DapperExecuteReader_InternalConnectionRemainsOpenUntilReaderIsDisposed()
    {
        AssertDapperInternalReaderOwnership(false);
        AssertDapperInternalReaderOwnership(true);
    }

    [TestMethod]
    public async Task DapperExecuteReaderAsync_InternalConnectionRemainsOpenUntilReaderIsDisposed()
    {
        await AssertDapperInternalReaderOwnershipAsync(false);
        await AssertDapperInternalReaderOwnershipAsync(true);
    }

    [TestMethod]
    public async Task DapperExecuteReader_PreservesNativeReaderAndReadsSQLite()
    {
        using var repository = TestInfrastructure.CreateRepository();

        using (var reader = repository.ExecuteReader(
                   new DefaultSqlCommand(DatabaseType.SQLite) { Sql = "SELECT 1" }))
        {
            Assert.IsTrue(reader is DbDataReader);
            Assert.IsTrue(reader is global::Dapper.IWrappedDataReader);
            Assert.IsTrue(reader.Read());
            Assert.AreEqual(1, Convert.ToInt32(reader.GetValue(0)));
        }

        using (var reader = await repository.ExecuteReaderAsync(
                   new DefaultSqlCommand(DatabaseType.SQLite) { Sql = "SELECT 2" }))
        {
            Assert.IsTrue(reader is DbDataReader);
            Assert.IsTrue(reader is global::Dapper.IWrappedDataReader);
            Assert.IsTrue(await ((DbDataReader)reader).ReadAsync());
            Assert.AreEqual(2, Convert.ToInt32(reader.GetValue(0)));
        }
    }

    [TestMethod]
    public async Task DapperExecuteReader_CallerConnectionAndTransactionRemainCallerOwned()
    {
        var provider = new TrackingDbProviderFactory { ThrowOnReaderExecution = false };
        var repository = new TrackingDapperRepository(CreateDapperOptions(provider));
        var connection = provider.CreateTrackingConnection("Data Source=external");
        connection.Open();

        using (var reader = repository.ExecuteReader(
                   new DefaultSqlCommand(DatabaseType.SQLite)
                   {
                       Sql = "SELECT 1",
                       Connection = connection
                   }))
        {
            Assert.IsTrue(reader.Read());
        }
        Assert.IsFalse(connection.IsDisposed, "调用方传入的连接不能由仓储释放。");
        Assert.AreEqual(ConnectionState.Open, connection.State);

        using (var transaction = connection.BeginTransaction())
        using (var reader = await repository.ExecuteReaderAsync(
                   new DefaultSqlCommand(DatabaseType.SQLite)
                   {
                       Sql = "SELECT 1",
                       Transaction = transaction
                   }))
        {
            Assert.IsTrue(await ((DbDataReader)reader).ReadAsync());
        }
        Assert.IsFalse(connection.IsDisposed, "事务关联的连接仍然归调用方所有。");
        Assert.AreEqual(ConnectionState.Open, connection.State);
        connection.Dispose();
    }

    [TestMethod]
    public async Task DapperExecuteReader_WhenCreationFails_DisposesOnlyInternalConnection()
    {
        var internalProvider = new TrackingDbProviderFactory();
        var repository = new TrackingDapperRepository(CreateDapperOptions(internalProvider));
        Assert.Throws<InvalidOperationException>(() => repository.ExecuteReader(
            new DefaultSqlCommand(DatabaseType.SQLite) { Sql = "SELECT 1" }));
        Assert.IsTrue(internalProvider.Connections.Single().IsDisposed);

        var externalProvider = new TrackingDbProviderFactory();
        repository = new TrackingDapperRepository(CreateDapperOptions(externalProvider));
        var externalConnection = externalProvider.CreateTrackingConnection("Data Source=external");
        externalConnection.Open();
        await AssertThrowsInvalidOperationAsync(() => repository.ExecuteReaderAsync(
            new DefaultSqlCommand(DatabaseType.SQLite)
            {
                Sql = "SELECT 1",
                Connection = externalConnection
            }));
        Assert.IsFalse(externalConnection.IsDisposed, "Reader 创建失败也不能释放调用方连接。");
        externalConnection.Dispose();
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task DapperExecuteReader_MonitorSeesOpenConnectionAndReaderClosesIt(bool generic, bool asynchronous)
    {
        var provider = new TrackingDbProviderFactory { ThrowOnReaderExecution = false };
        IBaseRepository repository = generic
            ? new TrackingGenericDapperRepository(CreateDapperOptions(provider))
            : new TrackingDapperRepository(CreateDapperOptions(provider));
        var states = new List<ConnectionState>();
        repository.Factory.SqlMonitor.SqlExecuting += context =>
        {
            states.Add(context.Connection.State);
            // 模拟监控代码初始化会话：不能依赖 Dapper 观察到关闭状态才安排连接关闭。
            if (context.Connection.State == ConnectionState.Closed)
            {
                context.Connection.Open();
            }
        };
        repository.Factory.SqlMonitor.SqlExecuted += context => states.Add(context.Connection.State);
        var sqlCommand = new DefaultSqlCommand(DatabaseType.SQLite) { Sql = "SELECT 1" };
        using (var reader = asynchronous
                   ? await repository.ExecuteReaderAsync(sqlCommand)
                   : repository.ExecuteReader(sqlCommand))
        {
            Assert.IsTrue(reader.Read());
        }
        Assert.AreEqual(ConnectionState.Closed, provider.Connections.Single().State,
            "监控代码打开过连接也不能导致 Reader 释放后连接仍然打开。");
        CollectionAssert.AreEqual(new[] { ConnectionState.Open, ConnectionState.Open }, states);
        Assert.IsTrue(provider.Commands.Single().IsDisposed);
    }

    [TestMethod]
    [DataRow(false, false, false)]
    [DataRow(false, true, false)]
    [DataRow(true, false, false)]
    [DataRow(true, true, false)]
    [DataRow(false, false, true)]
    [DataRow(false, true, true)]
    [DataRow(true, false, true)]
    [DataRow(true, true, true)]
    public async Task DapperExecuteReader_WhenCompletedMonitorThrows_CleansUpUnreturnedReader(bool external, bool asynchronous, bool cleanupFails)
    {
        var provider = new TrackingDbProviderFactory
        {
            ThrowOnReaderExecution = false,
            ThrowOnCommandDispose = cleanupFails
        };
        var repository = new TrackingDapperRepository(CreateDapperOptions(provider));
        using var externalConnection = external ? provider.CreateTrackingConnection("Data Source=external") : null;
        externalConnection?.Open();
        var expected = new InvalidOperationException("模拟完成监控异常。");
        repository.Factory.SqlMonitor.SqlExecuted += _ => throw expected;
        var sqlCommand = new DefaultSqlCommand(DatabaseType.SQLite)
        {
            Sql = "SELECT 1",
            Connection = externalConnection
        };
        var actual = asynchronous
            ? await Assert.ThrowsAsync<InvalidOperationException>(() => repository.ExecuteReaderAsync(sqlCommand))
            : Assert.Throws<InvalidOperationException>(() => repository.ExecuteReader(sqlCommand));

        Assert.AreSame(expected, actual);
        Assert.IsTrue(provider.Commands.Single().IsDisposed,
            "Reader 已创建但未能返回时，其持有的命令也必须释放。");
        var connection = provider.Connections.Single();
        Assert.AreEqual(!external, connection.IsDisposed);
        Assert.AreEqual(external ? ConnectionState.Open : ConnectionState.Closed, connection.State);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task DapperExecuteReader_PreservesCommandOptionsAndTransactionPriority(bool asynchronous)
    {
        var provider = new TrackingDbProviderFactory { ThrowOnReaderExecution = false };
        var repository = new TrackingDapperRepository(CreateDapperOptions(provider));
        using var transactionConnection = provider.CreateTrackingConnection("Data Source=transaction");
        using var ignoredConnection = provider.CreateTrackingConnection("Data Source=ignored");
        transactionConnection.Open();
        using var transaction = transactionConnection.BeginTransaction();
        var sqlCommand = new DefaultSqlCommand(DatabaseType.SQLite)
        {
            Sql = "ReadValue",
            Parameter = new { Value = 7 },
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 37,
            Connection = ignoredConnection,
            Transaction = transaction,
            Master = false
        };
        using (var reader = asynchronous
                   ? await repository.ExecuteReaderAsync(sqlCommand)
                   : repository.ExecuteReader(sqlCommand))
        {
            var command = provider.Commands.Single();
            Assert.AreEqual("ReadValue", command.CommandText);
            Assert.AreEqual(CommandType.StoredProcedure, command.CommandType);
            Assert.AreEqual(37, command.CommandTimeout);
            Assert.AreSame(transaction, command.Transaction);
            Assert.AreSame(transactionConnection, command.Connection);
            Assert.AreEqual(7, command.Parameters["Value"].Value);
            Assert.IsTrue(reader.Read());
        }
        Assert.AreEqual(ConnectionState.Open, transactionConnection.State);
        Assert.AreEqual(ConnectionState.Closed, ignoredConnection.State);
        Assert.IsFalse(transactionConnection.IsDisposed);
        Assert.IsFalse(ignoredConnection.IsDisposed);
        Assert.AreEqual(2, provider.Connections.Count, "传入事务时不能另建内部连接。");
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task GeneralExecute_WhenCleanupAlsoFails_PreservesExecutionException(bool autoDispose, bool asynchronous)
    {
        var provider = new TrackingDbProviderFactory { ThrowOnConnectionDispose = true };
        IBaseRepository repository = new TrackingDapperRepository(CreateDapperOptions(provider));
        var expected = new InvalidOperationException("模拟委托执行异常。");
        var actual = asynchronous
            ? await Assert.ThrowsAsync<InvalidOperationException>(() => repository.ExecuteAsync<int>(
                _ => Task.FromException<int>(expected), autoDisposeInternalConnection: autoDispose))
            : Assert.Throws<InvalidOperationException>(() => repository.Execute<int>(
                _ => throw expected, autoDisposeInternalConnection: autoDispose));
        Assert.AreSame(expected, actual, "清理异常不能覆盖委托的原始异常。");
        Assert.IsTrue(provider.Connections.Single().IsDisposed);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task GeneralExecute_WhenSuccessfulDisposeFails_ReportsCleanupException(bool asynchronous)
    {
        var provider = new TrackingDbProviderFactory { ThrowOnConnectionDispose = true };
        IBaseRepository repository = new TrackingDapperRepository(CreateDapperOptions(provider));
        var error = asynchronous
            ? await Assert.ThrowsAsync<InvalidOperationException>(() => repository.ExecuteAsync(_ => Task.FromResult(1)))
            : Assert.Throws<InvalidOperationException>(() => repository.Execute(_ => 1));
        Assert.AreEqual("模拟连接释放失败。", error.Message);
        Assert.IsTrue(provider.Connections.Single().IsDisposed);
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task GeneralExecute_WhenOpenFails_DisposesConnectionWithoutCallingDelegate(bool autoDispose, bool asynchronous)
    {
        var provider = new TrackingDbProviderFactory { ThrowOnOpen = true };
        IBaseRepository repository = new TrackingDapperRepository(CreateDapperOptions(provider));
        var called = false;
        Func<IDbConnection, int> execute = _ => { called = true; return 1; };
        if (asynchronous)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => repository.ExecuteAsync(
                connection => Task.FromResult(execute(connection)), autoDisposeInternalConnection: autoDispose));
        }
        else
        {
            Assert.Throws<InvalidOperationException>(() => repository.Execute(execute, autoDisposeInternalConnection: autoDispose));
        }
        Assert.IsFalse(called);
        Assert.IsTrue(provider.Connections.Single().IsDisposed);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task AddOrUpdate_BatchFailurePreservesEarlierSuccessfulItem(bool asynchronous)
    {
        var databasePath = CreateAtomicDatabase();
        try
        {
            var repository = CreateSqlServerStyleRepository(databasePath);
            var entities = new[]
            {
                new AtomicEntity { Id = 1, Value = "已成功" },
                new AtomicEntity { Id = 2, Value = null }
            };
            // 无外部事务的批量入口保持逐项提交，不擅自改为整批原子操作。
            if (asynchronous)
                await Assert.ThrowsAsync<SQLiteException>(() => repository.AddOrUpdateAsync(entities));
            else
                Assert.Throws<SQLiteException>(() => repository.AddOrUpdate(entities));
            Assert.AreEqual("已成功", ReadAtomicValue(databasePath));
        }
        finally
        {
            DeleteAtomicDatabase(databasePath);
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task AddOrUpdate_DuplicateUniqueValueRollsBackDelete(bool asynchronous)
    {
        var databasePath = CreateAtomicDatabase();
        try
        {
            using (var connection = new SQLiteConnection(GetAtomicConnectionString(databasePath)))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "CREATE UNIQUE INDEX IX_AtomicValue ON Batch14Atomic(Value); INSERT INTO Batch14Atomic(Id, Value) VALUES (2, 'occupied');";
                command.ExecuteNonQuery();
            }
            var repository = CreateSqlServerStyleRepository(databasePath);
            var entity = new AtomicEntity { Id = 1, Value = "occupied" };
            if (asynchronous)
                await Assert.ThrowsAsync<SQLiteException>(() => repository.AddOrUpdateAsync(entity));
            else
                Assert.Throws<SQLiteException>(() => repository.AddOrUpdate(entity));
            Assert.AreEqual("原始值", ReadAtomicValue(databasePath));
        }
        finally
        {
            DeleteAtomicDatabase(databasePath);
        }
    }

    [TestMethod]
    public void AddOrUpdate_WithExternalTransaction_UsesItForCountDeleteAndInsert()
    {
        var repository = new TrackingAtomicRepository(DatabaseType.SqlServer);
        var connection = new TrackingDbConnection(new TrackingDbProviderFactory()) { ConnectionString = "Data Source=external" };
        var transaction = new TrackingDbTransaction(connection);

        Assert.IsTrue(repository.AddOrUpdate(new AtomicEntity { Id = 1, Value = "更新值" }, transaction: transaction));
        Assert.AreSame(transaction, repository.CountTransaction);
        Assert.AreSame(transaction, repository.DeleteTransaction);
        Assert.AreSame(transaction, repository.AddTransaction);
        Assert.IsFalse(repository.AutoTransactionCalled);
    }

    [TestMethod]
    public async Task AddOrUpdateAsync_WithExternalTransaction_UsesItForCountDeleteAndInsert()
    {
        var repository = new TrackingAtomicRepository(DatabaseType.SqlServer);
        var connection = new TrackingDbConnection(new TrackingDbProviderFactory()) { ConnectionString = "Data Source=external" };
        var transaction = new TrackingDbTransaction(connection);

        Assert.IsTrue(await repository.AddOrUpdateAsync(new AtomicEntity { Id = 1, Value = "更新值" }, transaction: transaction));
        Assert.AreSame(transaction, repository.CountTransaction);
        Assert.AreSame(transaction, repository.DeleteTransaction);
        Assert.AreSame(transaction, repository.AddTransaction);
        Assert.IsFalse(repository.AutoTransactionCalled);
    }

    [TestMethod]
    public async Task AddOrUpdate_UsesAutoTransactionOnlyForVerifiedProviders()
    {
        var transactionalRepository = new TrackingAtomicRepository(DatabaseType.SqlServer);
        Assert.IsTrue(transactionalRepository.AddOrUpdate(new AtomicEntity { Id = 1, Value = "同步值" }));
        Assert.IsTrue(transactionalRepository.AutoTransactionCalled);
        Assert.IsNotNull(transactionalRepository.DeleteTransaction);
        Assert.AreSame(transactionalRepository.DeleteTransaction, transactionalRepository.AddTransaction);

        var asyncTransactionalRepository = new TrackingAtomicRepository(DatabaseType.SqlServer);
        Assert.IsTrue(await asyncTransactionalRepository.AddOrUpdateAsync(new AtomicEntity { Id = 1, Value = "异步值" }));
        Assert.IsTrue(asyncTransactionalRepository.AutoTransactionCalled);
        Assert.IsNotNull(asyncTransactionalRepository.DeleteTransaction);
        Assert.AreSame(asyncTransactionalRepository.DeleteTransaction, asyncTransactionalRepository.AddTransaction);

        foreach (var databaseType in new[] { DatabaseType.QuestDB, DatabaseType.ClickHouse, DatabaseType.Unknown })
        {
            var repository = new TrackingAtomicRepository(databaseType);
            Assert.IsTrue(repository.AddOrUpdate(new AtomicEntity { Id = 1, Value = "同步兼容值" }));
            Assert.IsFalse(repository.AutoTransactionCalled, $"{databaseType} 未经本地事务验证，必须保留原有逐语句行为。");
            Assert.IsNull(repository.DeleteTransaction);
            Assert.IsNull(repository.AddTransaction);

            var asyncRepository = new TrackingAtomicRepository(databaseType);
            Assert.IsTrue(await asyncRepository.AddOrUpdateAsync(new AtomicEntity { Id = 1, Value = "异步兼容值" }));
            Assert.IsFalse(asyncRepository.AutoTransactionCalled, $"{databaseType} 未经本地事务验证，必须保留原有逐语句行为。");
            Assert.IsNull(asyncRepository.DeleteTransaction);
            Assert.IsNull(asyncRepository.AddTransaction);
        }
    }

    [TestMethod]
    public void AddOrUpdate_WhenInsertFails_RollsBackDeletedRow()
    {
        var databasePath = CreateAtomicDatabase();
        try
        {
            var repository = CreateSqlServerStyleRepository(databasePath);
            Assert.Throws<SQLiteException>(() => repository.AddOrUpdate(
                new AtomicEntity { Id = 1, Value = null }));

            Assert.AreEqual("原始值", ReadAtomicValue(databasePath));
        }
        finally
        {
            DeleteAtomicDatabase(databasePath);
        }
    }

    [TestMethod]
    public async Task AddOrUpdateAsync_WhenInsertFails_RollsBackDeletedRow()
    {
        var databasePath = CreateAtomicDatabase();
        try
        {
            var repository = CreateSqlServerStyleRepository(databasePath);
            await AssertThrowsSqliteAsync(() => repository.AddOrUpdateAsync(
                new AtomicEntity { Id = 1, Value = null }));

            Assert.AreEqual("原始值", ReadAtomicValue(databasePath));
        }
        finally
        {
            DeleteAtomicDatabase(databasePath);
        }
    }

    [TestMethod]
    public async Task AddOrUpdate_ExternalTransactionObservesItsOwnChanges()
    {
        var databasePath = CreateAtomicDatabase();
        try
        {
            var connectionString = GetAtomicConnectionString(databasePath);
            var repository = CreateSqlServerStyleRepository(databasePath);
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            using (var transaction = connection.BeginTransaction())
            {
                DeleteAtomicRow(connection, transaction);
                Assert.IsTrue(repository.AddOrUpdate(
                    new AtomicEntity { Id = 1, Value = "同步事务值" }, transaction: transaction));
                Assert.AreEqual("同步事务值", ReadAtomicValue(connection, transaction));
                transaction.Rollback();
            }
            Assert.AreEqual("原始值", ReadAtomicValue(databasePath));

            using (var transaction = connection.BeginTransaction())
            {
                DeleteAtomicRow(connection, transaction);
                Assert.IsTrue(await repository.AddOrUpdateAsync(
                    new AtomicEntity { Id = 1, Value = "异步事务值" }, transaction: transaction));
                Assert.AreEqual("异步事务值", ReadAtomicValue(connection, transaction));
                transaction.Rollback();
            }
            Assert.AreEqual("原始值", ReadAtomicValue(databasePath));
        }
        finally
        {
            DeleteAtomicDatabase(databasePath);
        }
    }

    private static DbFactory CreateFactory(TrackingDbProviderFactory provider)
    {
        return new DbFactory("Data Source=tracking", provider);
    }

    private static void AssertThrowsAndDisposesConnection(Action action, TrackingDbProviderFactory provider)
    {
        var connectionIndex = provider.Connections.Count;
        Assert.Throws<InvalidOperationException>(action);
        Assert.AreEqual(connectionIndex + 1, provider.Connections.Count);
        Assert.IsTrue(provider.Connections[connectionIndex].IsDisposed, "内部连接在异常路径上必须被释放。");
    }

    private static async Task AssertThrowsAndDisposesConnectionAsync(Func<Task> action, TrackingDbProviderFactory provider)
    {
        var connectionIndex = provider.Connections.Count;
        await AssertThrowsInvalidOperationAsync(action);
        Assert.AreEqual(connectionIndex + 1, provider.Connections.Count);
        Assert.IsTrue(provider.Connections[connectionIndex].IsDisposed, "内部连接在异步异常路径上必须被释放。");
    }

    private static async Task AssertThrowsInvalidOperationAsync(Func<Task> action)
    {
        try
        {
            await action();
            Assert.Fail("预期抛出 InvalidOperationException。");
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static async Task AssertThrowsSqliteAsync(Func<Task> action)
    {
        try
        {
            await action();
            Assert.Fail("预期 SQLite 拒绝写入空值。");
        }
        catch (SQLiteException)
        {
        }
    }

    private static AtomicRepository CreateSqlServerStyleRepository(string databasePath)
    {
        return new AtomicRepository(GetAtomicConnectionString(databasePath));
    }

    private static string CreateAtomicDatabase()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"Sean.Core.DbRepository.Batch14.{Guid.NewGuid():N}.db");
        try
        {
            using var connection = new SQLiteConnection(GetAtomicConnectionString(databasePath));
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE [Batch14Atomic] ([Id] INTEGER NOT NULL PRIMARY KEY, [Value] TEXT NOT NULL);";
            command.ExecuteNonQuery();
            command.CommandText = "INSERT INTO [Batch14Atomic] ([Id], [Value]) VALUES (1, '原始值');";
            command.ExecuteNonQuery();
            return databasePath;
        }
        catch
        {
            DeleteAtomicDatabase(databasePath);
            throw;
        }
    }

    private static string GetAtomicConnectionString(string databasePath)
    {
        return $"Data Source={databasePath};Version=3;Pooling=False;Default Timeout=1;";
    }

    private static void DeleteAtomicRow(SQLiteConnection connection, SQLiteTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "DELETE FROM [Batch14Atomic] WHERE [Id] = 1;";
        Assert.AreEqual(1, command.ExecuteNonQuery());
    }

    private static string ReadAtomicValue(string databasePath)
    {
        using var connection = new SQLiteConnection(GetAtomicConnectionString(databasePath));
        connection.Open();
        return ReadAtomicValue(connection, null);
    }

    private static string ReadAtomicValue(SQLiteConnection connection, SQLiteTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT [Value] FROM [Batch14Atomic] WHERE [Id] = 1;";
        return Convert.ToString(command.ExecuteScalar());
    }

    private static void DeleteAtomicDatabase(string databasePath)
    {
        foreach (var suffix in new[] { string.Empty, "-wal", "-shm" })
        {
            var filePath = databasePath + suffix;
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Table("Batch14Atomic")]
    private sealed class AtomicEntity
    {
        [Key]
        public long Id { get; set; }
        public string Value { get; set; }
    }

    private sealed class AtomicRepository : BaseRepository<AtomicEntity>
    {
        public AtomicRepository(string connectionString)
            : base(CreateConnectionOptions(connectionString, DatabaseType.SqlServer, SQLiteFactory.Instance))
        {
        }
    }

    private sealed class TrackingAtomicRepository : BaseRepository<AtomicEntity>
    {
        public TrackingAtomicRepository(DatabaseType databaseType)
            : base(CreateConnectionOptions("Data Source=tracking", databaseType, new TrackingDbProviderFactory()))
        {
        }

        public IDbTransaction CountTransaction { get; private set; }
        public IDbTransaction DeleteTransaction { get; private set; }
        public IDbTransaction AddTransaction { get; private set; }
        public bool AutoTransactionCalled { get; private set; }

        public override T ExecuteScalar<T>(ISqlCommand sqlCommand)
        {
            CountTransaction = sqlCommand.Transaction;
            return (T)(object)1;
        }

        public override Task<T> ExecuteScalarAsync<T>(ISqlCommand sqlCommand)
        {
            CountTransaction = sqlCommand.Transaction;
            return Task.FromResult((T)(object)1);
        }

        public override bool Delete(AtomicEntity entity, IDbTransaction transaction = null)
        {
            DeleteTransaction = transaction;
            return true;
        }

        public override Task<bool> DeleteAsync(AtomicEntity entity, IDbTransaction transaction = null)
        {
            DeleteTransaction = transaction;
            return Task.FromResult(true);
        }

        public override bool Add(AtomicEntity entity, bool returnAutoIncrementId = false,
            System.Linq.Expressions.Expression<Func<AtomicEntity, object>> fieldExpression = null,
            IDbTransaction transaction = null)
        {
            AddTransaction = transaction;
            return true;
        }

        public override Task<bool> AddAsync(AtomicEntity entity, bool returnAutoIncrementId = false,
            System.Linq.Expressions.Expression<Func<AtomicEntity, object>> fieldExpression = null,
            IDbTransaction transaction = null)
        {
            AddTransaction = transaction;
            return Task.FromResult(true);
        }

        public override bool ExecuteAutoTransaction(Func<IDbTransaction, bool> func,
            IDbTransaction transaction = null, IDbConnection connection = null)
        {
            AutoTransactionCalled = true;
            return func(new TrackingDbTransaction(new TrackingDbConnection(new TrackingDbProviderFactory())));
        }

        public override Task<bool> ExecuteAutoTransactionAsync(Func<IDbTransaction, Task<bool>> func,
            IDbTransaction transaction = null, IDbConnection connection = null)
        {
            AutoTransactionCalled = true;
            return func(new TrackingDbTransaction(new TrackingDbConnection(new TrackingDbProviderFactory())));
        }
    }

    private static ConnectionStringOptions CreateConnectionOptions(
        string connectionString, DatabaseType databaseType, DbProviderFactory providerFactory)
    {
        return new ConnectionStringOptions(connectionString, providerFactory) { DbType = databaseType };
    }

    private static ConnectionStringOptions CreateDapperOptions(DbProviderFactory providerFactory)
    {
        return CreateConnectionOptions("Data Source=dapper-reader", DatabaseType.SQLite, providerFactory);
    }

    private static void AssertDapperInternalReaderOwnership(bool genericRepository)
    {
        var provider = new TrackingDbProviderFactory { ThrowOnReaderExecution = false };
        IBaseRepository repository = genericRepository
            ? new TrackingGenericDapperRepository(CreateDapperOptions(provider))
            : new TrackingDapperRepository(CreateDapperOptions(provider));

        var reader = repository.ExecuteReader(new DefaultSqlCommand(DatabaseType.SQLite) { Sql = "SELECT 1" });
        var connection = provider.Connections.Single();
        var command = provider.Commands.Single();
        Assert.IsFalse(connection.IsDisposed,
            genericRepository ? "泛型仓储提前释放了内部连接。" : "非泛型仓储提前释放了内部连接。");
        Assert.AreEqual(ConnectionState.Open, connection.State);
        Assert.IsFalse(command.IsDisposed);
        Assert.IsTrue(reader.Read());
        reader.Dispose();
        Assert.AreEqual(ConnectionState.Closed, connection.State,
            "仓储内部连接应在 Reader 释放时通过 CloseConnection 关闭。");
        Assert.IsFalse(connection.IsDisposed,
            "CloseConnection 只保证关闭连接，不等同于调用连接对象的 Dispose。");
        Assert.IsTrue(command.IsDisposed);
    }

    private static async Task AssertDapperInternalReaderOwnershipAsync(bool genericRepository)
    {
        var provider = new TrackingDbProviderFactory { ThrowOnReaderExecution = false };
        IBaseRepository repository = genericRepository
            ? new TrackingGenericDapperRepository(CreateDapperOptions(provider))
            : new TrackingDapperRepository(CreateDapperOptions(provider));

        var reader = await repository.ExecuteReaderAsync(
            new DefaultSqlCommand(DatabaseType.SQLite) { Sql = "SELECT 1" });
        var connection = provider.Connections.Single();
        var command = provider.Commands.Single();
        Assert.IsFalse(connection.IsDisposed,
            genericRepository ? "泛型仓储提前释放了异步内部连接。" : "非泛型仓储提前释放了异步内部连接。");
        Assert.AreEqual(ConnectionState.Open, connection.State);
        Assert.IsFalse(command.IsDisposed);
        Assert.IsTrue(await ((DbDataReader)reader).ReadAsync());
        reader.Dispose();
        Assert.AreEqual(ConnectionState.Closed, connection.State,
            "仓储内部连接应在 Reader 释放时通过 CloseConnection 关闭。");
        Assert.IsFalse(connection.IsDisposed,
            "CloseConnection 只保证关闭连接，不等同于调用连接对象的 Dispose。");
        Assert.IsTrue(command.IsDisposed);
    }

    private sealed class TrackingDapperRepository : DapperBaseRepository
    {
        public TrackingDapperRepository(ConnectionStringOptions options) : base(options)
        {
        }
    }

    private sealed class TrackingGenericDapperRepository : DapperBaseRepository<AtomicEntity>
    {
        public TrackingGenericDapperRepository(ConnectionStringOptions options) : base(options)
        {
        }
    }

    private sealed class TrackingDbProviderFactory : DbProviderFactory
    {
        public List<TrackingDbConnection> Connections { get; } = new();
        public List<TrackingDbCommand> Commands { get; } = new();
        public bool ThrowOnOpen { get; set; }
        public bool ThrowOnCommandDispose { get; set; }
        public bool ThrowOnConnectionDispose { get; set; }
        public bool ThrowOnReaderExecution { get; set; } = true;

        public TrackingDbConnection CreateTrackingConnection(string connectionString)
        {
            var connection = new TrackingDbConnection(this) { ConnectionString = connectionString };
            Connections.Add(connection);
            return connection;
        }

        public override DbConnection CreateConnection()
        {
            return CreateTrackingConnection(null);
        }

        public override DbCommand CreateCommand()
        {
            return CreateTrackingCommand();
        }

        public override DbParameter CreateParameter()
        {
            return new TrackingDbParameter();
        }

        public TrackingDbCommand CreateTrackingCommand()
        {
            var command = new TrackingDbCommand(this);
            Commands.Add(command);
            return command;
        }
    }

    private sealed class TrackingDbConnection : DbConnection
    {
        private readonly TrackingDbProviderFactory _provider;
        private ConnectionState _state = ConnectionState.Closed;

        public TrackingDbConnection(TrackingDbProviderFactory provider)
        {
            _provider = provider;
        }

        public bool IsDisposed { get; private set; }
        public override string ConnectionString { get; set; }
        public override string Database => "Tracking";
        public override string DataSource => "Tracking";
        public override string ServerVersion => "1.0";
        public override ConnectionState State => _state;

        public override void ChangeDatabase(string databaseName)
        {
        }

        public override void Close()
        {
            _state = ConnectionState.Closed;
        }

        public override void Open()
        {
            if (_provider.ThrowOnOpen)
            {
                throw new InvalidOperationException("模拟连接打开失败。");
            }
            _state = ConnectionState.Open;
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            return new TrackingDbTransaction(this);
        }

        protected override DbCommand CreateDbCommand()
        {
            var command = _provider.CreateTrackingCommand();
            command.Connection = this;
            return command;
        }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            _state = ConnectionState.Closed;
            base.Dispose(disposing);
            if (disposing && _provider.ThrowOnConnectionDispose)
            {
                throw new InvalidOperationException("模拟连接释放失败。");
            }
        }
    }

    private sealed class TrackingDbTransaction : DbTransaction
    {
        private readonly DbConnection _connection;

        public TrackingDbTransaction(DbConnection connection)
        {
            _connection = connection;
        }

        public override IsolationLevel IsolationLevel => IsolationLevel.Unspecified;
        protected override DbConnection DbConnection => _connection;
        public override void Commit()
        {
        }
        public override void Rollback()
        {
        }
    }

    private sealed class TrackingDbCommand : DbCommand
    {
        private readonly TrackingDbProviderFactory _provider;
        private readonly TrackingDbParameterCollection _parameters = new();

        public TrackingDbCommand(TrackingDbProviderFactory provider)
        {
            _provider = provider;
        }

        public bool IsDisposed { get; private set; }
        public override string CommandText { get; set; }
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection DbConnection { get; set; }
        protected override DbParameterCollection DbParameterCollection => _parameters;
        protected override DbTransaction DbTransaction { get; set; }

        public override void Cancel()
        {
        }

        public override int ExecuteNonQuery()
        {
            throw new InvalidOperationException("模拟命令执行失败。");
        }

        public override object ExecuteScalar()
        {
            throw new InvalidOperationException("模拟命令执行失败。");
        }

        public override void Prepare()
        {
        }

        protected override DbParameter CreateDbParameter()
        {
            return new TrackingDbParameter();
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            if (_provider.ThrowOnReaderExecution)
            {
                throw new InvalidOperationException("模拟 Reader 执行失败。");
            }
            return new TrackingDbDataReader(this, behavior);
        }

        protected override Task<DbDataReader> ExecuteDbDataReaderAsync(
            CommandBehavior behavior, CancellationToken cancellationToken)
        {
            return _provider.ThrowOnReaderExecution
                ? Task.FromException<DbDataReader>(new InvalidOperationException("模拟异步 Reader 执行失败。"))
                : Task.FromResult<DbDataReader>(new TrackingDbDataReader(this, behavior));
        }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            if (disposing && _provider.ThrowOnCommandDispose)
            {
                throw new InvalidOperationException("模拟命令释放失败。");
            }
            base.Dispose(disposing);
        }
    }

    private sealed class TrackingDbDataReader : DbDataReader
    {
        private readonly TrackingDbCommand _command;
        private readonly CommandBehavior _behavior;
        private bool _read;
        private bool _closed;

        public TrackingDbDataReader(TrackingDbCommand command, CommandBehavior behavior)
        {
            _command = command;
            _behavior = behavior;
        }

        public override object this[int ordinal] => GetValue(ordinal);
        public override object this[string name] => GetValue(0);
        public override int Depth => 0;
        public override int FieldCount => 1;
        public override bool HasRows => true;
        public override bool IsClosed => _closed;
        public override int RecordsAffected => -1;

        public override bool Read()
        {
            if (_command.Connection is TrackingDbConnection { IsDisposed: true })
            {
                throw new ObjectDisposedException(nameof(TrackingDbConnection));
            }
            if (_read)
            {
                return false;
            }
            _read = true;
            return true;
        }

        public override Task<bool> ReadAsync(CancellationToken cancellationToken) => Task.FromResult(Read());
        public override bool NextResult() => false;
        public override string GetName(int ordinal) => "Value";
        public override string GetDataTypeName(int ordinal) => typeof(int).Name;
        public override Type GetFieldType(int ordinal) => typeof(int);
        public override object GetValue(int ordinal) => 1;
        public override int GetValues(object[] values)
        {
            values[0] = 1;
            return 1;
        }
        public override int GetOrdinal(string name) => 0;
        public override bool GetBoolean(int ordinal) => true;
        public override byte GetByte(int ordinal) => 1;
        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length) => 0;
        public override char GetChar(int ordinal) => '1';
        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length) => 0;
        public override Guid GetGuid(int ordinal) => Guid.Empty;
        public override short GetInt16(int ordinal) => 1;
        public override int GetInt32(int ordinal) => 1;
        public override long GetInt64(int ordinal) => 1;
        public override float GetFloat(int ordinal) => 1;
        public override double GetDouble(int ordinal) => 1;
        public override string GetString(int ordinal) => "1";
        public override decimal GetDecimal(int ordinal) => 1;
        public override DateTime GetDateTime(int ordinal) => DateTime.UnixEpoch;
        public override bool IsDBNull(int ordinal) => false;
        public override IEnumerator GetEnumerator() => Array.Empty<object>().GetEnumerator();

        public override void Close()
        {
            Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing || _closed)
            {
                return;
            }
            _closed = true;
            if ((_behavior & CommandBehavior.CloseConnection) != 0)
            {
                _command.Connection?.Close();
            }
            base.Dispose(disposing);
        }
    }

    private sealed class TrackingDbParameter : DbParameter
    {
        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; }
        public override bool IsNullable { get; set; }
        public override string ParameterName { get; set; }
        public override int Size { get; set; }
        public override string SourceColumn { get; set; }
        public override bool SourceColumnNullMapping { get; set; }
        public override object Value { get; set; }
        public override void ResetDbType()
        {
        }
    }

    private sealed class TrackingDbParameterCollection : DbParameterCollection
    {
        private readonly List<DbParameter> _items = new();

        public override int Count => _items.Count;
        public override object SyncRoot => ((ICollection)_items).SyncRoot;
        public override int Add(object value)
        {
            _items.Add((DbParameter)value);
            return _items.Count - 1;
        }
        public override void AddRange(Array values)
        {
            foreach (var value in values)
            {
                Add(value);
            }
        }
        public override void Clear() => _items.Clear();
        public override bool Contains(object value) => _items.Contains((DbParameter)value);
        public override bool Contains(string value) => IndexOf(value) >= 0;
        public override void CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
        public override IEnumerator GetEnumerator() => _items.GetEnumerator();
        public override int IndexOf(object value) => _items.IndexOf((DbParameter)value);
        public override int IndexOf(string parameterName) => _items.FindIndex(parameter => parameter.ParameterName == parameterName);
        public override void Insert(int index, object value) => _items.Insert(index, (DbParameter)value);
        public override void Remove(object value) => _items.Remove((DbParameter)value);
        public override void RemoveAt(int index) => _items.RemoveAt(index);
        public override void RemoveAt(string parameterName)
        {
            var index = IndexOf(parameterName);
            if (index >= 0)
            {
                RemoveAt(index);
            }
        }
        protected override DbParameter GetParameter(int index) => _items[index];
        protected override DbParameter GetParameter(string parameterName) => _items[IndexOf(parameterName)];
        protected override void SetParameter(int index, DbParameter value) => _items[index] = value;
        protected override void SetParameter(string parameterName, DbParameter value)
        {
            var index = IndexOf(parameterName);
            if (index >= 0)
            {
                _items[index] = value;
            }
            else
            {
                _items.Add(value);
            }
        }
    }
}
