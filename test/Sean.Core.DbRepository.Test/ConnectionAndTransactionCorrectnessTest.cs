using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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

    private sealed class TrackingDbProviderFactory : DbProviderFactory
    {
        public List<TrackingDbConnection> Connections { get; } = new();
        public bool ThrowOnOpen { get; set; }
        public bool ThrowOnCommandDispose { get; set; }

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
            return new TrackingDbCommand(this);
        }

        public override DbParameter CreateParameter()
        {
            return new TrackingDbParameter();
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
            return new TrackingDbCommand(_provider) { Connection = this };
        }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            _state = ConnectionState.Closed;
            base.Dispose(disposing);
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
            throw new InvalidOperationException("模拟 Reader 执行失败。");
        }

        protected override Task<DbDataReader> ExecuteDbDataReaderAsync(
            CommandBehavior behavior, CancellationToken cancellationToken)
        {
            return Task.FromException<DbDataReader>(new InvalidOperationException("模拟异步 Reader 执行失败。"));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _provider.ThrowOnCommandDispose)
            {
                throw new InvalidOperationException("模拟命令释放失败。");
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
