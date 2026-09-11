using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sean.Core.DbRepository.Dapper;

namespace Sean.Core.DbRepository.Test;

/// <summary>
/// 使用真实 SQLite 验证非 Reader 执行入口；两个仓储实现分别运行，防止重复实现发生行为偏差。
/// </summary>
[TestClass]
public class DapperExecutionCoverageTest
{
    private enum Operation { Execute, Query, Get, Scalar, UntypedScalar, DataTable, DataSet }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task SharedExecution_UsesRepositoryVirtualBoundaryIncludingReader(bool generic, bool asynchronous)
    {
        using var fixture = new Fixture(generic);
        foreach (var operation in Enum.GetValues<Operation>())
        {
            fixture.ExecutionOwnership.Clear();
            var result = await Run(fixture.Repository, operation, CreateCommand(operation), asynchronous);
            AssertResult(operation, result);
            CollectionAssert.AreEqual(new[] { true }, fixture.ExecutionOwnership);
        }

        fixture.ExecutionOwnership.Clear();
        var command = new DefaultSqlCommand("SELECT 41 AS Value");
        using (var reader = asynchronous
            ? await fixture.Repository.ExecuteReaderAsync(command)
            : fixture.Repository.ExecuteReader(command))
        {
            // 共享组件不能绕过子类的通用执行重写，也不能在返回 Reader 前释放内部连接。
            CollectionAssert.AreEqual(new[] { false }, fixture.ExecutionOwnership);
            Assert.IsTrue(reader.Read());
            Assert.AreEqual(41L, reader.GetInt64(0));
        }
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task InternalConnection_ResultsRemainUsableAfterDisposal(bool generic, bool asynchronous)
    {
        using var fixture = new Fixture(generic);
        foreach (var master in new[] { true, false })
        foreach (var operation in Enum.GetValues<Operation>())
        {
            var command = CreateCommand(operation);
            command.Master = master;
            var result = await Run(fixture.Repository, operation, command, asynchronous);

            Assert.AreEqual(master ? 17 : 29,
                new SQLiteConnectionStringBuilder(fixture.ConnectionStrings.Last()).DefaultTimeout);
            Assert.IsTrue(fixture.Disposed.Contains(fixture.Started.Last().Connection), operation.ToString());
            // 在内部连接释放后才枚举结果，保护 Dapper 默认缓冲以及 DataTable/DataSet 的独立生命周期。
            AssertResult(operation, result);
            Assert.IsNull(fixture.Completed.Last().Exception);
            Assert.AreSame(command.Parameter, fixture.Started.Last().SqlParameter);
        }
        Assert.AreEqual(14, fixture.Started.Count);
        Assert.AreEqual(14, fixture.Completed.Count);
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task CallerConnection_PreservesOriginalStateAndOwnership(bool generic, bool asynchronous)
    {
        using var fixture = new Fixture(generic);
        foreach (var initiallyOpen in new[] { false, true })
        {
            using var connection = new SQLiteConnection("Data Source=:memory:;Pooling=False;");
            if (initiallyOpen) connection.Open();
            foreach (var operation in Enum.GetValues<Operation>())
            {
                var command = CreateCommand(operation);
                command.Connection = connection;
                AssertResult(operation, await Run(fixture.Repository, operation, command, asynchronous));
                Assert.AreSame(connection, fixture.Started.Last().Connection);
                Assert.IsFalse(fixture.Disposed.Contains(connection), operation.ToString());
                Assert.AreEqual(initiallyOpen ? ConnectionState.Open : ConnectionState.Closed, connection.State);
            }
        }
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task CallerTransaction_ReadsOwnWriteAndCanStillRollback(bool generic, bool asynchronous)
    {
        using var fixture = new Fixture(generic);
        using var connection = new SQLiteConnection("Data Source=:memory:;Pooling=False;");
        connection.Open();
        using var setup = connection.CreateCommand();
        setup.CommandText = "CREATE TABLE sample (Value INTEGER)";
        setup.ExecuteNonQuery();
        using var transaction = connection.BeginTransaction();
        var insert = new DefaultSqlCommand("INSERT INTO sample (Value) VALUES (@Value)", new { Value = 41 })
        {
            Transaction = transaction
        };
        Assert.AreEqual(1, await Run(fixture.Repository, Operation.Execute, insert, asynchronous));
        foreach (var operation in Enum.GetValues<Operation>().Where(value => value != Operation.Execute))
        {
            var command = new DefaultSqlCommand("SELECT Value FROM sample") { Transaction = transaction };
            AssertResult(operation, await Run(fixture.Repository, operation, command, asynchronous), rows: 1);
            Assert.AreSame(connection, fixture.Started.Last().Connection);
            Assert.AreSame(transaction, fixture.Completed.Last().Transaction);
            Assert.AreEqual(ConnectionState.Open, connection.State);
            Assert.IsFalse(fixture.Disposed.Contains(connection));
        }
        transaction.Rollback();
        setup.CommandText = "SELECT COUNT(*) FROM sample";
        Assert.AreEqual(0L, setup.ExecuteScalar());
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task SqlFailure_ReportsOriginalExceptionAndRespectsOwnership(bool generic, bool asynchronous)
    {
        using var fixture = new Fixture(generic);
        using var callerConnection = new SQLiteConnection("Data Source=:memory:;Pooling=False;");
        callerConnection.Open();
        foreach (var external in new[] { false, true })
        foreach (var operation in Enum.GetValues<Operation>())
        {
            var command = new DefaultSqlCommand("SELECT Value FROM missing_table")
            {
                Connection = external ? callerConnection : null
            };
            var error = await Assert.ThrowsAsync<SQLiteException>(async () =>
                await Run(fixture.Repository, operation, command, asynchronous));
            Assert.AreSame(error, fixture.Completed.Last().Exception);
            var actualConnection = fixture.Started.Last().Connection;
            Assert.AreEqual(!external, fixture.Disposed.Contains(actualConnection), operation.ToString());
            if (external)
            {
                Assert.AreSame(callerConnection, actualConnection);
                Assert.AreEqual(ConnectionState.Open, callerConnection.State);
            }
        }
        Assert.AreEqual(14, fixture.Started.Count);
        Assert.AreEqual(14, fixture.Completed.Count);
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task AllEntries_RejectNullCommand(bool generic, bool asynchronous)
    {
        using var fixture = new Fixture(generic);
        foreach (var operation in Enum.GetValues<Operation>())
        {
            var error = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await Run(fixture.Repository, operation, null, asynchronous));
            Assert.AreEqual("sqlCommand", error.ParamName);
        }
        var readerError = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            using var reader = asynchronous
                ? await fixture.Repository.ExecuteReaderAsync(null)
                : fixture.Repository.ExecuteReader(null);
        });
        Assert.AreEqual("sqlCommand", readerError.ParamName);
        Assert.AreEqual(0, fixture.Started.Count);
        Assert.AreEqual(0, fixture.Completed.Count);
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task EmptyResult_PreservesDefaultsAndTableSchema(bool generic, bool asynchronous)
    {
        using var fixture = new Fixture(generic);
        var command = new DefaultSqlCommand("SELECT 41 AS Value WHERE 1 = 0");
        foreach (var operation in Enum.GetValues<Operation>().Where(value => value != Operation.Execute))
        {
            var result = await Run(fixture.Repository, operation, command, asynchronous);
            switch (operation)
            {
                case Operation.Query:
                    Assert.AreEqual(0, ((IEnumerable<long>)result).Count());
                    break;
                case Operation.Get:
                case Operation.Scalar:
                    Assert.AreEqual(0L, result);
                    break;
                case Operation.UntypedScalar:
                    Assert.IsNull(result);
                    break;
                case Operation.DataTable:
                    using (var table = (DataTable)result) AssertEmptyTable(table);
                    break;
                case Operation.DataSet:
                    using (var set = (DataSet)result)
                    {
                        Assert.AreEqual(1, set.Tables.Count);
                        AssertEmptyTable(set.Tables[0]);
                    }
                    break;
            }
            Assert.IsTrue(fixture.Disposed.Contains(fixture.Started.Last().Connection));
        }
        // 无行时，非空值类型返回默认值，Nullable 和引用类型仍应返回 null。
        Assert.IsNull(asynchronous ? await fixture.Repository.GetAsync<long?>(command) : fixture.Repository.Get<long?>(command));
        Assert.IsNull(asynchronous ? await fixture.Repository.GetAsync<Row>(command) : fixture.Repository.Get<Row>(command));
        Assert.IsNull(asynchronous ? await fixture.Repository.ExecuteScalarAsync<long?>(command) : fixture.Repository.ExecuteScalar<long?>(command));
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task MonitorFailure_PreservesExceptionAndConnectionOwnership(bool generic, bool asynchronous)
    {
        foreach (var beforeExecution in new[] { true, false })
        {
            using var fixture = new Fixture(generic);
            using var callerConnection = new SQLiteConnection("Data Source=:memory:;Pooling=False;");
            callerConnection.Open();
            var expected = new InvalidOperationException("监控回调失败");
            if (beforeExecution) fixture.Repository.Factory.SqlMonitor.SqlExecuting += _ => throw expected;
            else fixture.Repository.Factory.SqlMonitor.SqlExecuted += _ => throw expected;
            foreach (var external in new[] { false, true })
            foreach (var operation in Enum.GetValues<Operation>())
            {
                var command = CreateCommand(operation);
                // 执行前监控失败应阻止 SQL 执行，不能被这条无效 SQL 的异常取代。
                if (beforeExecution) command.Sql = "SELECT Value FROM missing_table";
                command.Connection = external ? callerConnection : null;
                var actual = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    await Run(fixture.Repository, operation, command, asynchronous));
                Assert.AreSame(expected, actual);
                Assert.AreEqual(!external, fixture.Disposed.Contains(fixture.Started.Last().Connection));
                if (external) Assert.AreEqual(ConnectionState.Open, callerConnection.State);
                // 完成事件描述的是 SQL 执行结果；回调自身抛错并不表示 SQL 执行失败。
                Assert.AreSame(beforeExecution ? expected : null, fixture.Completed.Last().Exception);
            }
            Assert.AreEqual(14, fixture.Completed.Count);
            using var verify = callerConnection.CreateCommand();
            verify.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'sample'";
            Assert.AreEqual(beforeExecution ? 0L : 1L, verify.ExecuteScalar());
        }
    }

    private static void AssertEmptyTable(DataTable table)
    {
        Assert.AreEqual(0, table.Rows.Count);
        Assert.AreEqual(1, table.Columns.Count);
        Assert.AreEqual("Value", table.Columns[0].ColumnName);
    }

    private static DefaultSqlCommand CreateCommand(Operation operation) => new(
        operation == Operation.Execute ? "CREATE TABLE sample (Value INTEGER)" : "SELECT @Value AS Value UNION ALL SELECT @Value + 1 AS Value",
        new { Value = 41 });

    private static async Task<object> Run(BaseRepository repository, Operation operation, ISqlCommand command, bool asynchronous)
    {
        if (asynchronous)
        {
            return operation switch
            {
                Operation.Execute => await repository.ExecuteAsync(command),
                Operation.Query => await repository.QueryAsync<long>(command),
                Operation.Get => await repository.GetAsync<long>(command),
                Operation.Scalar => await repository.ExecuteScalarAsync<long>(command),
                Operation.UntypedScalar => await repository.ExecuteScalarAsync(command),
                Operation.DataTable => await repository.ExecuteDataTableAsync(command),
                Operation.DataSet => await repository.ExecuteDataSetAsync(command),
                _ => throw new ArgumentOutOfRangeException(nameof(operation))
            };
        }
        return operation switch
        {
            Operation.Execute => repository.Execute(command),
            Operation.Query => repository.Query<long>(command),
            Operation.Get => repository.Get<long>(command),
            Operation.Scalar => repository.ExecuteScalar<long>(command),
            Operation.UntypedScalar => repository.ExecuteScalar(command),
            Operation.DataTable => repository.ExecuteDataTable(command),
            Operation.DataSet => repository.ExecuteDataSet(command),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
    }

    private static void AssertResult(Operation operation, object result, int rows = 2)
    {
        switch (operation)
        {
            case Operation.Execute:
                Assert.AreEqual(0, result);
                break;
            case Operation.Query:
                CollectionAssert.AreEqual(rows == 1 ? new[] { 41L } : new[] { 41L, 42L }, ((IEnumerable<long>)result).ToArray());
                break;
            case Operation.DataTable:
                using (var table = (DataTable)result) AssertTable(table, rows);
                break;
            case Operation.DataSet:
                using (var set = (DataSet)result)
                {
                    Assert.AreEqual(1, set.Tables.Count);
                    AssertTable(set.Tables[0], rows);
                }
                break;
            default:
                Assert.AreEqual(41L, result);
                break;
        }
    }

    private static void AssertTable(DataTable table, int rows)
    {
        Assert.AreEqual(rows, table.Rows.Count);
        Assert.AreEqual(1, table.Columns.Count);
        Assert.AreEqual("Value", table.Columns[0].ColumnName);
        Assert.AreEqual(41L, table.Rows[0][0]);
        if (rows == 2) Assert.AreEqual(42L, table.Rows[1][0]);
    }

    private sealed class Fixture : IDisposable
    {
        public BaseRepository Repository { get; }
        public List<SqlExecutingContext> Started { get; } = new();
        public List<SqlExecutedContext> Completed { get; } = new();
        public List<string> ConnectionStrings { get; } = new();
        public HashSet<IDbConnection> Disposed { get; } = new();
        public List<bool> ExecutionOwnership { get; } = new();

        public Fixture(bool generic)
        {
            var settings = new MultiConnectionSettings(new[]
            {
                new ConnectionStringOptions("Data Source=:memory:;Pooling=False;Default Timeout=17;", SQLiteFactory.Instance),
                new ConnectionStringOptions("Data Source=:memory:;Pooling=False;Default Timeout=29;", SQLiteFactory.Instance, master: false)
            });
            Repository = generic ? new GenericRepository(settings, ExecutionOwnership.Add) : new Repository(settings, ExecutionOwnership.Add);
            Repository.Factory.SqlMonitor.SqlExecuting += context =>
            {
                Started.Add(context);
                // 连接释放后 SQLite 不允许访问 ConnectionString，必须在执行时保存快照。
                ConnectionStrings.Add(context.Connection.ConnectionString);
                ((DbConnection)context.Connection).Disposed += (_, _) => Disposed.Add(context.Connection);
            };
            Repository.Factory.SqlMonitor.SqlExecuted += Completed.Add;
        }

        public void Dispose()
        {
            // 断言失败时也回收测试使用的连接；所有权断言均在此兜底清理之前完成。
            foreach (var connection in Started.Select(context => context.Connection).Distinct()) connection.Dispose();
        }
    }

    private sealed class Repository(MultiConnectionSettings settings, Action<bool> onExecute) : DapperBaseRepository(settings)
    {
        public override T Execute<T>(Func<IDbConnection, T> func, bool master = true, IDbTransaction transaction = null,
            IDbConnection connection = null, bool autoDisposeInternalConnection = true)
        {
            onExecute(autoDisposeInternalConnection);
            return base.Execute(func, master, transaction, connection, autoDisposeInternalConnection);
        }
        public override Task<T> ExecuteAsync<T>(Func<IDbConnection, Task<T>> func, bool master = true, IDbTransaction transaction = null,
            IDbConnection connection = null, bool autoDisposeInternalConnection = true)
        {
            onExecute(autoDisposeInternalConnection);
            return base.ExecuteAsync(func, master, transaction, connection, autoDisposeInternalConnection);
        }
    }
    private sealed class GenericRepository(MultiConnectionSettings settings, Action<bool> onExecute) : DapperBaseRepository<Row>(settings)
    {
        public override T Execute<T>(Func<IDbConnection, T> func, bool master = true, IDbTransaction transaction = null,
            IDbConnection connection = null, bool autoDisposeInternalConnection = true)
        {
            onExecute(autoDisposeInternalConnection);
            return base.Execute(func, master, transaction, connection, autoDisposeInternalConnection);
        }
        public override Task<T> ExecuteAsync<T>(Func<IDbConnection, Task<T>> func, bool master = true, IDbTransaction transaction = null,
            IDbConnection connection = null, bool autoDisposeInternalConnection = true)
        {
            onExecute(autoDisposeInternalConnection);
            return base.ExecuteAsync(func, master, transaction, connection, autoDisposeInternalConnection);
        }
    }
    private sealed class Row { public long Value { get; set; } }
}
