using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sean.Core.DbRepository.Dapper;

namespace Sean.Core.DbRepository.Test;

/// <summary>
/// 使用独立的 SQLite 内存连接验证通用执行方法的资源所有权，不依赖共享测试数据库。
/// </summary>
[TestClass]
public class GeneralExecutionLifetimeTest
{
    [TestMethod]
    public void Execute_DefaultThroughInterface_DisposesInternalConnectionAfterSuccess()
    {
        using var repository = new ObservableRepository();
        IBaseRepository api = repository;

        var result = api.Execute(connection =>
        {
            Assert.AreEqual(ConnectionState.Open, connection.State);
            return global::Dapper.SqlMapper.ExecuteScalar<long>(connection, "SELECT 17");
        });

        Assert.AreEqual(17L, result);
        Assert.IsTrue(repository.Trace.Disposed.Contains(repository.Trace.Connections[0]));
        CollectionAssert.AreEqual(new[] { true }, repository.Trace.SyncModes);
        CollectionAssert.AreEqual(new[] { true }, repository.Trace.MasterRequests);
    }

    [TestMethod]
    public async Task ExecuteAsync_DefaultThroughConcreteSubclass_KeepsConnectionAcrossAwaitAndDisposesAfterSuccess()
    {
        using var repository = new ObservableDapperRepository();

        var result = await repository.ExecuteAsync(async connection =>
        {
            Assert.AreEqual(ConnectionState.Open, connection.State);
            await Task.Yield();
            Assert.AreEqual(ConnectionState.Open, connection.State);
            Assert.IsFalse(repository.Trace.Disposed.Contains((DbConnection)connection));
            return await global::Dapper.SqlMapper.ExecuteScalarAsync<long>(connection, "SELECT 23");
        });

        Assert.AreEqual(23L, result);
        Assert.IsTrue(repository.Trace.Disposed.Contains(repository.Trace.Connections[0]));
        CollectionAssert.AreEqual(new[] { true }, repository.Trace.AsyncModes);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Execute_WhenCallbackFails_DisposesInternalConnectionRegardlessOfHandoff(bool autoDispose)
    {
        using var repository = new ObservableRepository();
        IBaseRepository api = repository;
        var expected = new InvalidOperationException("同步执行失败");

        var actual = Assert.Throws<InvalidOperationException>(() => api.Execute<int>(connection =>
        {
            Assert.AreEqual(ConnectionState.Open, connection.State);
            throw expected;
        }, autoDisposeInternalConnection: autoDispose));

        Assert.AreSame(expected, actual);
        Assert.IsTrue(repository.Trace.Disposed.Contains(repository.Trace.Connections[0]));
        CollectionAssert.AreEqual(new[] { autoDispose }, repository.Trace.SyncModes);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task ExecuteAsync_WhenCallbackFails_DisposesInternalConnectionRegardlessOfHandoff(bool autoDispose)
    {
        using var repository = new ObservableDapperRepository();
        IBaseRepository api = repository;
        var expected = new InvalidOperationException("异步执行失败");

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => api.ExecuteAsync<int>(async connection =>
        {
            await Task.Yield();
            Assert.AreEqual(ConnectionState.Open, connection.State);
            throw expected;
        }, autoDisposeInternalConnection: autoDispose));

        Assert.AreSame(expected, actual);
        Assert.IsTrue(repository.Trace.Disposed.Contains(repository.Trace.Connections[0]));
        CollectionAssert.AreEqual(new[] { autoDispose }, repository.Trace.AsyncModes);
    }

    [TestMethod]
    public void Execute_HandoffThroughInterface_DapperReaderClosesInternalConnectionAfterConsumption()
    {
        using var repository = new ObservableRepository();
        IBaseRepository api = repository;

        // 通用方法只交接连接；创建 Reader 时仍须显式指定 CloseConnection。
        using (var reader = api.Execute(connection => global::Dapper.SqlMapper.ExecuteReader(
                   connection, new global::Dapper.CommandDefinition("SELECT 31 UNION ALL SELECT 32"),
                   CommandBehavior.CloseConnection), autoDisposeInternalConnection: false))
        {
            Assert.IsInstanceOfType<global::Dapper.IWrappedDataReader>(reader);
            Assert.AreEqual(ConnectionState.Open, repository.Trace.Connections[0].State);
            Assert.IsFalse(repository.Trace.Disposed.Contains(repository.Trace.Connections[0]));
            Assert.IsTrue(reader.Read());
            Assert.AreEqual(31L, reader.GetInt64(0));
            Assert.IsTrue(reader.Read());
            Assert.AreEqual(32L, reader.GetInt64(0));
            Assert.IsFalse(reader.Read());
        }

        Assert.AreEqual(ConnectionState.Closed, repository.Trace.Connections[0].State);
        CollectionAssert.AreEqual(new[] { false }, repository.Trace.SyncModes);
    }

    [TestMethod]
    public async Task ExecuteAsync_HandoffThroughConcreteSubclass_DapperReaderSurvivesAwaitAndClosesConnection()
    {
        using var repository = new ObservableDapperRepository();

        using (var reader = await repository.ExecuteAsync(async connection =>
               {
                   await Task.Yield();
                   return await global::Dapper.SqlMapper.ExecuteReaderAsync(connection,
                       new global::Dapper.CommandDefinition("SELECT 41"), CommandBehavior.CloseConnection);
               }, autoDisposeInternalConnection: false))
        {
            await Task.Yield();
            Assert.AreEqual(ConnectionState.Open, repository.Trace.Connections[0].State);
            Assert.IsFalse(repository.Trace.Disposed.Contains(repository.Trace.Connections[0]));
            Assert.IsInstanceOfType<global::Dapper.IWrappedDataReader>(reader);
            Assert.IsTrue(await ((DbDataReader)reader).ReadAsync());
            Assert.AreEqual(41L, reader.GetInt64(0));
        }

        Assert.AreEqual(ConnectionState.Closed, repository.Trace.Connections[0].State);
        CollectionAssert.AreEqual(new[] { false }, repository.Trace.AsyncModes);
    }

    [TestMethod]
    public void Execute_HandoffWithNativeReader_CallerKeepsCommandUntilReaderIsDisposed()
    {
        using var repository = new ObservableRepository();
        DbCommand command = null;
        var commandDisposed = false;

        // 原生 ADO.NET 的 Command 由调用方保留到 Reader 用完，不能在回调内 using 后返回 Reader。
        try
        {
            using (var reader = repository.Execute(connection =>
                   {
                       command = (DbCommand)connection.CreateCommand();
                       command.Disposed += (_, _) => commandDisposed = true;
                       command.CommandText = "SELECT 51";
                       return command.ExecuteReader(CommandBehavior.CloseConnection);
                   }, autoDisposeInternalConnection: false))
            {
                Assert.IsInstanceOfType<SQLiteDataReader>(reader);
                Assert.IsFalse(commandDisposed);
                Assert.AreEqual(ConnectionState.Open, repository.Trace.Connections[0].State);
                Assert.IsTrue(reader.Read());
                Assert.AreEqual(51L, reader.GetInt64(0));
            }

            Assert.AreEqual(ConnectionState.Closed, repository.Trace.Connections[0].State);
            Assert.IsFalse(commandDisposed);
        }
        finally
        {
            command?.Dispose();
        }

        Assert.IsTrue(commandDisposed);
    }

    [TestMethod]
    public async Task ExecuteAsync_HandoffWithNativeReader_CallerKeepsCommandUntilReaderIsDisposed()
    {
        using var repository = new ObservableRepository();
        IBaseRepository api = repository;
        DbCommand command = null;
        var commandDisposed = false;

        try
        {
            using (var reader = await api.ExecuteAsync(async connection =>
                   {
                       command = (DbCommand)connection.CreateCommand();
                       command.Disposed += (_, _) => commandDisposed = true;
                       command.CommandText = "SELECT 61";
                       await Task.Yield();
                       return await command.ExecuteReaderAsync(CommandBehavior.CloseConnection);
                   }, autoDisposeInternalConnection: false))
            {
                await Task.Yield();
                Assert.IsInstanceOfType<SQLiteDataReader>(reader);
                Assert.IsFalse(commandDisposed);
                Assert.AreEqual(ConnectionState.Open, repository.Trace.Connections[0].State);
                Assert.IsTrue(await reader.ReadAsync());
                Assert.AreEqual(61L, reader.GetInt64(0));
            }

            Assert.AreEqual(ConnectionState.Closed, repository.Trace.Connections[0].State);
            Assert.IsFalse(commandDisposed);
        }
        finally
        {
            command?.Dispose();
        }

        Assert.IsTrue(commandDisposed);
    }

    [TestMethod]
    [DataRow(true, true)]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(false, false)]
    public async Task Execute_HandoffWithEmptyReader_StillClosesInternalConnection(bool useAsync, bool readBeforeDispose)
    {
        using var repository = new ObservableDapperRepository();
        IBaseRepository api = repository;
        var definition = new global::Dapper.CommandDefinition("SELECT 1 WHERE 1 = 0");
        using (var reader = useAsync
                   ? await api.ExecuteAsync(connection => global::Dapper.SqlMapper.ExecuteReaderAsync(
                       connection, definition, CommandBehavior.CloseConnection), autoDisposeInternalConnection: false)
                   : api.Execute(connection => global::Dapper.SqlMapper.ExecuteReader(
                       connection, definition, CommandBehavior.CloseConnection), autoDisposeInternalConnection: false))
        {
            Assert.AreEqual(ConnectionState.Open, repository.Trace.Connections[0].State);
            if (readBeforeDispose)
            {
                Assert.IsFalse(reader.Read());
            }
        }

        Assert.AreEqual(ConnectionState.Closed, repository.Trace.Connections[0].State);
    }

    [TestMethod]
    [DataRow(true, true)]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(false, false)]
    public void Execute_CallerResourcesRemainOwnedAndTransactionTakesPriority(bool autoDispose, bool useTransaction)
    {
        using var repository = new ObservableRepository();
        IBaseRepository api = repository;
        using var selected = OpenCallerConnection();
        using var other = OpenCallerConnection();
        using var transaction = useTransaction ? selected.BeginTransaction() : null;
        var suppliedConnection = useTransaction ? other : selected;
        var selectedDisposed = false;
        selected.Disposed += (_, _) => selectedDisposed = true;

        var result = api.Execute(connection =>
        {
            Assert.AreSame(selected, connection, "事务连接应优先于另外传入的连接。");
            return global::Dapper.SqlMapper.ExecuteScalar<long>(connection, "SELECT 71", transaction: transaction);
        }, transaction: transaction, connection: suppliedConnection, autoDisposeInternalConnection: autoDispose);
        Assert.AreEqual(71L, result);

        var expected = new InvalidOperationException("调用方连接上的执行失败");
        var actual = Assert.Throws<InvalidOperationException>(() => api.Execute<int>(connection =>
        {
            Assert.AreSame(selected, connection);
            throw expected;
        }, transaction: transaction, connection: suppliedConnection, autoDisposeInternalConnection: autoDispose));

        Assert.AreSame(expected, actual);
        Assert.IsFalse(selectedDisposed);
        Assert.AreEqual(ConnectionState.Open, selected.State);
        Assert.AreEqual(ConnectionState.Open, other.State);
        Assert.AreEqual(0, repository.Trace.Connections.Count);
        transaction?.Rollback();
    }

    [TestMethod]
    [DataRow(true, true)]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(false, false)]
    public async Task ExecuteAsync_CallerResourcesRemainOwnedAndTransactionTakesPriority(bool autoDispose, bool useTransaction)
    {
        using var repository = new ObservableDapperRepository();
        IBaseRepository api = repository;
        using var selected = OpenCallerConnection();
        using var other = OpenCallerConnection();
        using var transaction = useTransaction ? selected.BeginTransaction() : null;
        var suppliedConnection = useTransaction ? other : selected;
        var selectedDisposed = false;
        selected.Disposed += (_, _) => selectedDisposed = true;

        var result = await api.ExecuteAsync(async connection =>
        {
            await Task.Yield();
            Assert.AreSame(selected, connection, "异步执行同样应优先使用事务连接。");
            return await global::Dapper.SqlMapper.ExecuteScalarAsync<long>(connection, "SELECT 81", transaction: transaction);
        }, transaction: transaction, connection: suppliedConnection, autoDisposeInternalConnection: autoDispose);
        Assert.AreEqual(81L, result);

        var expected = new InvalidOperationException("调用方连接上的异步执行失败");
        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => api.ExecuteAsync<int>(async connection =>
        {
            await Task.Yield();
            Assert.AreSame(selected, connection);
            throw expected;
        }, transaction: transaction, connection: suppliedConnection, autoDisposeInternalConnection: autoDispose));

        Assert.AreSame(expected, actual);
        Assert.IsFalse(selectedDisposed);
        Assert.AreEqual(ConnectionState.Open, selected.State);
        Assert.AreEqual(ConnectionState.Open, other.State);
        Assert.AreEqual(0, repository.Trace.Connections.Count);
        transaction?.Rollback();
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Execute_NullCallbackFailsBeforeCreatingConnection(bool autoDispose)
    {
        using var repository = new ObservableRepository();
        IBaseRepository api = repository;

        Assert.Throws<ArgumentNullException>(() => api.Execute<int>(null,
            autoDisposeInternalConnection: autoDispose));
        await Assert.ThrowsAsync<ArgumentNullException>(() => api.ExecuteAsync<int>(null,
            autoDisposeInternalConnection: autoDispose));

        Assert.AreEqual(0, repository.Trace.Connections.Count);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task ExecuteAsync_CanceledCallback_PreservesCancellationAndDisposesInternalConnection(bool autoDispose)
    {
        using var repository = new ObservableRepository();
        IBaseRepository api = repository;
        var cancellationToken = new CancellationToken(canceled: true);

        var execution = api.ExecuteAsync(connection => Task.FromCanceled<int>(cancellationToken),
            autoDisposeInternalConnection: autoDispose);
        var exception = await Assert.ThrowsAsync<TaskCanceledException>(() => execution);

        Assert.AreEqual(cancellationToken, exception.CancellationToken);
        Assert.IsTrue(execution.IsCanceled);
        Assert.IsTrue(repository.Trace.Disposed.Contains(repository.Trace.Connections[0]));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Execute_ClosedCallerConnection_IsNeitherOpenedNorDisposed(bool autoDispose)
    {
        using var repository = new ObservableRepository();
        IBaseRepository api = repository;
        using var connection = new SQLiteConnection("Data Source=:memory:;Version=3;Pooling=False;");
        var disposed = false;
        connection.Disposed += (_, _) => disposed = true;

        Assert.AreEqual(ConnectionState.Closed, api.Execute(selected => selected.State,
            connection: connection, autoDisposeInternalConnection: autoDispose));
        Assert.AreEqual(ConnectionState.Closed, await api.ExecuteAsync(selected => Task.FromResult(selected.State),
            connection: connection, autoDisposeInternalConnection: autoDispose));
        Assert.Throws<InvalidOperationException>(() => api.Execute<int>(_ =>
            throw new InvalidOperationException("未打开的外部连接上的同步失败"),
            connection: connection, autoDisposeInternalConnection: autoDispose));
        await Assert.ThrowsAsync<InvalidOperationException>(() => api.ExecuteAsync<int>(_ =>
            Task.FromException<int>(new InvalidOperationException("未打开的外部连接上的异步失败")),
            connection: connection, autoDisposeInternalConnection: autoDispose));

        Assert.IsFalse(disposed);
        Assert.AreEqual(ConnectionState.Closed, connection.State);
        Assert.AreEqual(0, repository.Trace.Connections.Count);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Execute_DefaultAndHandoffModesPreserveMasterSelection(bool master)
    {
        using var repository = new ObservableRepository();
        var expectedTimeout = master ? 17 : 29;

        var actualTimeout = repository.Execute(connection =>
            new SQLiteConnectionStringBuilder(connection.ConnectionString).DefaultTimeout, master: master);
        Assert.AreEqual(expectedTimeout, actualTimeout);
        Assert.IsTrue(repository.Trace.Disposed.Contains(repository.Trace.Connections[0]));

        using (var reader = await repository.ExecuteAsync(connection => global::Dapper.SqlMapper.ExecuteReaderAsync(
                   connection, new global::Dapper.CommandDefinition("SELECT 91"), CommandBehavior.CloseConnection),
                   master: master, autoDisposeInternalConnection: false))
        {
            Assert.AreEqual(expectedTimeout,
                new SQLiteConnectionStringBuilder(repository.Trace.Connections[1].ConnectionString).DefaultTimeout);
            Assert.IsTrue(reader.Read());
            Assert.AreEqual(91L, reader.GetInt64(0));
        }

        CollectionAssert.AreEqual(new[] { master, master }, repository.Trace.MasterRequests);
        Assert.AreEqual(ConnectionState.Closed, repository.Trace.Connections[1].State);
    }

    [TestMethod]
    public async Task DapperReaderEntries_KeepGenericExecutionAndConnectionOverridesInTheCallChain()
    {
        using var repository = new ObservableDapperRepository();

        using (var reader = repository.ExecuteReader(new DefaultSqlCommand("SELECT 101")))
        {
            Assert.IsTrue(reader.Read());
            Assert.AreEqual(101L, reader.GetInt64(0));
            CollectionAssert.AreEqual(new[] { false }, repository.Trace.SyncModes);
            Assert.AreEqual(1, repository.Trace.Connections.Count);
            Assert.AreEqual(ConnectionState.Open, repository.Trace.Connections[0].State);
        }

        using (var reader = await repository.ExecuteReaderAsync(
                   new DefaultSqlCommand("SELECT 102") { Master = false }))
        {
            Assert.IsTrue(await ((DbDataReader)reader).ReadAsync());
            Assert.AreEqual(102L, reader.GetInt64(0));
            CollectionAssert.AreEqual(new[] { false }, repository.Trace.AsyncModes);
            Assert.AreEqual(2, repository.Trace.Connections.Count);
            Assert.AreEqual(ConnectionState.Open, repository.Trace.Connections[1].State);
        }

        CollectionAssert.AreEqual(new[] { true, false }, repository.Trace.MasterRequests);
        Assert.AreEqual(ConnectionState.Closed, repository.Trace.Connections[0].State);
        Assert.AreEqual(ConnectionState.Closed, repository.Trace.Connections[1].State);
    }

    private static MultiConnectionSettings CreateSettings()
    {
        return new MultiConnectionSettings(new[]
        {
            new ConnectionStringOptions("Data Source=:memory:;Version=3;Pooling=False;Default Timeout=17;",
                SQLiteFactory.Instance),
            new ConnectionStringOptions("Data Source=:memory:;Version=3;Pooling=False;Default Timeout=29;",
                SQLiteFactory.Instance, master: false)
        });
    }

    private static SQLiteConnection OpenCallerConnection()
    {
        var connection = new SQLiteConnection("Data Source=:memory:;Version=3;Pooling=False;");
        connection.Open();
        return connection;
    }

    private sealed class ConnectionTrace : IDisposable
    {
        public List<DbConnection> Connections { get; } = new();
        public HashSet<DbConnection> Disposed { get; } = new();
        public List<bool> MasterRequests { get; } = new();
        public List<bool> SyncModes { get; } = new();
        public List<bool> AsyncModes { get; } = new();

        public DbConnection Record(DbConnection connection, bool master)
        {
            Connections.Add(connection);
            MasterRequests.Add(master);
            connection.Disposed += (_, _) => Disposed.Add(connection);
            return connection;
        }

        public void Dispose()
        {
            // 断言失败时也释放本测试创建的所有连接，避免失败用例影响后续执行。
            foreach (var connection in Connections)
            {
                connection.Dispose();
            }
        }
    }

    private sealed class ObservableRepository : BaseRepository, IDisposable
    {
        public ConnectionTrace Trace { get; } = new();

        public ObservableRepository() : base(CreateSettings())
        {
        }

        protected override DbConnection OpenNewConnection(bool master = true)
        {
            return Trace.Record(base.OpenNewConnection(master), master);
        }

        public override T Execute<T>(Func<IDbConnection, T> func, bool master = true,
            IDbTransaction transaction = null, IDbConnection connection = null, bool autoDisposeInternalConnection = true)
        {
            Trace.SyncModes.Add(autoDisposeInternalConnection);
            return base.Execute(func, master, transaction, connection, autoDisposeInternalConnection);
        }

        public override Task<T> ExecuteAsync<T>(Func<IDbConnection, Task<T>> func, bool master = true,
            IDbTransaction transaction = null, IDbConnection connection = null, bool autoDisposeInternalConnection = true)
        {
            Trace.AsyncModes.Add(autoDisposeInternalConnection);
            return base.ExecuteAsync(func, master, transaction, connection, autoDisposeInternalConnection);
        }

        public void Dispose() => Trace.Dispose();
    }

    private sealed class ObservableDapperRepository : DapperBaseRepository, IDisposable
    {
        public ConnectionTrace Trace { get; } = new();

        public ObservableDapperRepository() : base(CreateSettings())
        {
        }

        protected override DbConnection OpenNewConnection(bool master = true)
        {
            return Trace.Record(base.OpenNewConnection(master), master);
        }

        public override T Execute<T>(Func<IDbConnection, T> func, bool master = true,
            IDbTransaction transaction = null, IDbConnection connection = null, bool autoDisposeInternalConnection = true)
        {
            Trace.SyncModes.Add(autoDisposeInternalConnection);
            return base.Execute(func, master, transaction, connection, autoDisposeInternalConnection);
        }

        public override Task<T> ExecuteAsync<T>(Func<IDbConnection, Task<T>> func, bool master = true,
            IDbTransaction transaction = null, IDbConnection connection = null, bool autoDisposeInternalConnection = true)
        {
            Trace.AsyncModes.Add(autoDisposeInternalConnection);
            return base.ExecuteAsync(func, master, transaction, connection, autoDisposeInternalConnection);
        }

        public void Dispose() => Trace.Dispose();
    }
}
