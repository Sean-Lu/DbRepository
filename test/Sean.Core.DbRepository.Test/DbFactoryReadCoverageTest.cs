using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sean.Core.DbRepository.Test;

/// <summary>
/// 验证 DbFactory 各读取重载的结果、参数与连接所有权，不依赖共享数据库。
/// </summary>
[TestClass]
public class DbFactoryReadCoverageTest
{
    public enum Route { Settings, ConnectionString, Connection, Transaction }
    private enum Operation { Query, Get, Scalar, UntypedScalar, DataTable, DataSet }
    private const string Direct = "Data Source=:memory:;Pooling=False;Default Timeout=43;";

    [TestMethod]
    [DataRow(Route.Settings, false)]
    [DataRow(Route.Settings, true)]
    [DataRow(Route.ConnectionString, false)]
    [DataRow(Route.ConnectionString, true)]
    [DataRow(Route.Connection, false)]
    [DataRow(Route.Connection, true)]
    [DataRow(Route.Transaction, false)]
    [DataRow(Route.Transaction, true)]
    public async Task ReadOverloads_PreserveResultsAndOwnership(Route route, bool asynchronous)
    {
        using var fixture = new Fixture();
        using var connection = fixture.Factory.CreateConnection();
        if (route == Route.Transaction) connection.Open();
        using var transaction = route == Route.Transaction ? connection.BeginTransaction() : null;
        var internalConnection = route == Route.Settings || route == Route.ConnectionString;
        foreach (var operation in Enum.GetValues<Operation>())
        {
            var result = await Run(fixture.Factory, operation, route, asynchronous,
                "SELECT @Value AS Value UNION ALL SELECT @Value + 1 AS Value", connection, transaction);
            AssertResult(operation, result);
            Assert.AreEqual(internalConnection, fixture.Disposed.Contains(fixture.Last.Connection));
            Assert.AreEqual(route == Route.Settings ? 29 : route == Route.ConnectionString ? 43 : 17, fixture.Timeout);
            Assert.AreSame(transaction, fixture.Last.Transaction);
            if (!internalConnection)
            {
                Assert.AreSame(connection, fixture.Last.Connection);
                // 原生执行层主动打开外部连接，但不负责关闭或释放它。
                Assert.AreEqual(ConnectionState.Open, connection.State);
            }
            var error = await Assert.ThrowsAsync<SQLiteException>(async () =>
                await Run(fixture.Factory, operation, route, asynchronous,
                    "SELECT Value FROM missing_table", connection, transaction));
            Assert.AreSame(error, fixture.Last.Exception);
            Assert.AreEqual(internalConnection, fixture.Disposed.Contains(fixture.Last.Connection));
            if (!internalConnection) Assert.AreEqual(ConnectionState.Open, connection.State);
        }
        if (transaction != null) transaction.Rollback();
    }

    [TestMethod]
    public void ConnectionManagement_PreservesSelectionAndExplicitOwnership()
    {
        using var fixture = new Fixture();
        var factory = fixture.Factory;
        using var empty = factory.CreateEmptyConnection();
        Assert.AreEqual(ConnectionState.Closed, empty.State);
        Assert.IsTrue(string.IsNullOrEmpty(empty.ConnectionString));
        foreach (var master in new[] { true, false })
        {
            using var connection = factory.CreateConnection(master);
            Assert.AreEqual(ConnectionState.Closed, connection.State);
            Assert.AreEqual(master ? 17 : 29, new SQLiteConnectionStringBuilder(connection.ConnectionString).DefaultTimeout);
            var disposed = false;
            connection.Disposed += (_, _) => disposed = true;
            factory.OpenConnection(connection);
            factory.OpenConnection(connection);
            Assert.AreEqual(ConnectionState.Open, connection.State);
            factory.CloseConnection(connection, disposeConnection: false);
            Assert.AreEqual(ConnectionState.Closed, connection.State);
            Assert.IsFalse(disposed);
            factory.OpenConnection(connection);
            factory.CloseConnection(connection);
            Assert.IsTrue(disposed);
        }
        using var direct = factory.OpenNewConnection(Direct);
        Assert.AreEqual(ConnectionState.Open, direct.State);
        Assert.AreEqual(43, new SQLiteConnectionStringBuilder(direct.ConnectionString).DefaultTimeout);
        factory.OpenConnection(null);
        factory.CloseConnection(null);
    }

    [TestMethod]
    public void CreateConnection_RejectsMissingConnectionString()
    {
        using var fixture = new Fixture();
        foreach (var value in new[] { null, string.Empty, "  " })
        {
            Assert.AreEqual("connectionString", Assert.Throws<ArgumentException>(() =>
                fixture.Factory.CreateConnection(value)).ParamName);
            Assert.AreEqual("connectionString", Assert.Throws<ArgumentException>(() =>
                fixture.Factory.OpenNewConnection(value)).ParamName);
        }
    }

    private static async Task<object> Run(DbFactory factory, Operation operation, Route route, bool asynchronous,
        string sql, IDbConnection connection, IDbTransaction transaction)
    {
        // DbParameter 不能跨命令共享，每次调用创建独立参数，防止参数所有权干扰测试。
        DbParameter[] parameters = { new SQLiteParameter("@Value", 41L) };
        return (route, asynchronous) switch
        {
            (Route.Settings, false) => operation switch
            {
                Operation.Query => factory.Query<long>(sql, parameters, master: false),
                Operation.Get => factory.Get<long>(sql, parameters, master: false),
                Operation.Scalar => factory.ExecuteScalar<long>(sql, parameters, master: false),
                Operation.UntypedScalar => factory.ExecuteScalar(sql, parameters, master: false),
                Operation.DataTable => factory.ExecuteDataTable(sql, parameters, master: false),
                Operation.DataSet => factory.ExecuteDataSet(sql, parameters, master: false),
                _ => throw new ArgumentOutOfRangeException(nameof(operation))
            },
            (Route.Settings, true) => operation switch
            {
                Operation.Query => await factory.QueryAsync<long>(sql, parameters, master: false),
                Operation.Get => await factory.GetAsync<long>(sql, parameters, master: false),
                Operation.Scalar => await factory.ExecuteScalarAsync<long>(sql, parameters, master: false),
                Operation.UntypedScalar => await factory.ExecuteScalarAsync(sql, parameters, master: false),
                Operation.DataTable => await factory.ExecuteDataTableAsync(sql, parameters, master: false),
                Operation.DataSet => await factory.ExecuteDataSetAsync(sql, parameters, master: false),
                _ => throw new ArgumentOutOfRangeException(nameof(operation))
            },
            (Route.ConnectionString, false) => operation switch
            {
                Operation.Query => factory.Query<long>(Direct, sql, parameters),
                Operation.Get => factory.Get<long>(Direct, sql, parameters),
                Operation.Scalar => factory.ExecuteScalar<long>(Direct, sql, parameters),
                Operation.UntypedScalar => factory.ExecuteScalar(Direct, sql, parameters),
                Operation.DataTable => factory.ExecuteDataTable(Direct, sql, parameters),
                Operation.DataSet => factory.ExecuteDataSet(Direct, sql, parameters),
                _ => throw new ArgumentOutOfRangeException(nameof(operation))
            },
            (Route.ConnectionString, true) => operation switch
            {
                Operation.Query => await factory.QueryAsync<long>(Direct, sql, parameters),
                Operation.Get => await factory.GetAsync<long>(Direct, sql, parameters),
                Operation.Scalar => await factory.ExecuteScalarAsync<long>(Direct, sql, parameters),
                Operation.UntypedScalar => await factory.ExecuteScalarAsync(Direct, sql, parameters),
                Operation.DataTable => await factory.ExecuteDataTableAsync(Direct, sql, parameters),
                Operation.DataSet => await factory.ExecuteDataSetAsync(Direct, sql, parameters),
                _ => throw new ArgumentOutOfRangeException(nameof(operation))
            },
            (Route.Connection, false) => operation switch
            {
                Operation.Query => factory.Query<long>(connection, sql, parameters),
                Operation.Get => factory.Get<long>(connection, sql, parameters),
                Operation.Scalar => factory.ExecuteScalar<long>(connection, sql, parameters),
                Operation.UntypedScalar => factory.ExecuteScalar(connection, sql, parameters),
                Operation.DataTable => factory.ExecuteDataTable(connection, sql, parameters),
                Operation.DataSet => factory.ExecuteDataSet(connection, sql, parameters),
                _ => throw new ArgumentOutOfRangeException(nameof(operation))
            },
            (Route.Connection, true) => operation switch
            {
                Operation.Query => await factory.QueryAsync<long>(connection, sql, parameters),
                Operation.Get => await factory.GetAsync<long>(connection, sql, parameters),
                Operation.Scalar => await factory.ExecuteScalarAsync<long>(connection, sql, parameters),
                Operation.UntypedScalar => await factory.ExecuteScalarAsync(connection, sql, parameters),
                Operation.DataTable => await factory.ExecuteDataTableAsync(connection, sql, parameters),
                Operation.DataSet => await factory.ExecuteDataSetAsync(connection, sql, parameters),
                _ => throw new ArgumentOutOfRangeException(nameof(operation))
            },
            (Route.Transaction, false) => operation switch
            {
                Operation.Query => factory.Query<long>(transaction, sql, parameters),
                Operation.Get => factory.Get<long>(transaction, sql, parameters),
                Operation.Scalar => factory.ExecuteScalar<long>(transaction, sql, parameters),
                Operation.UntypedScalar => factory.ExecuteScalar(transaction, sql, parameters),
                Operation.DataTable => factory.ExecuteDataTable(transaction, sql, parameters),
                Operation.DataSet => factory.ExecuteDataSet(transaction, sql, parameters),
                _ => throw new ArgumentOutOfRangeException(nameof(operation))
            },
            (Route.Transaction, true) => operation switch
            {
                Operation.Query => await factory.QueryAsync<long>(transaction, sql, parameters),
                Operation.Get => await factory.GetAsync<long>(transaction, sql, parameters),
                Operation.Scalar => await factory.ExecuteScalarAsync<long>(transaction, sql, parameters),
                Operation.UntypedScalar => await factory.ExecuteScalarAsync(transaction, sql, parameters),
                Operation.DataTable => await factory.ExecuteDataTableAsync(transaction, sql, parameters),
                Operation.DataSet => await factory.ExecuteDataSetAsync(transaction, sql, parameters),
                _ => throw new ArgumentOutOfRangeException(nameof(operation))
            },
            _ => throw new ArgumentOutOfRangeException(nameof(route))
        };
    }

    private static void AssertResult(Operation operation, object result)
    {
        switch (operation)
        {
            case Operation.Query:
                CollectionAssert.AreEqual(new[] { 41L, 42L }, ((IEnumerable<long>)result).ToArray());
                break;
            case Operation.DataTable:
                using (var table = (DataTable)result) AssertTable(table);
                break;
            case Operation.DataSet:
                using (var set = (DataSet)result)
                {
                    Assert.AreEqual(1, set.Tables.Count);
                    AssertTable(set.Tables[0]);
                }
                break;
            default:
                Assert.AreEqual(41L, result);
                break;
        }
    }

    private static void AssertTable(DataTable table)
    {
        Assert.AreEqual(1, table.Columns.Count);
        Assert.AreEqual("Value", table.Columns[0].ColumnName);
        Assert.AreEqual(2, table.Rows.Count);
        Assert.AreEqual(41L, table.Rows[0][0]);
        Assert.AreEqual(42L, table.Rows[1][0]);
    }

    private sealed class Fixture : IDisposable
    {
        private readonly List<IDbConnection> _connections = new();
        public DbFactory Factory { get; } = new(new MultiConnectionSettings(new[]
        {
            new ConnectionStringOptions("Data Source=:memory:;Pooling=False;Default Timeout=17;", SQLiteFactory.Instance),
            new ConnectionStringOptions("Data Source=:memory:;Pooling=False;Default Timeout=29;", SQLiteFactory.Instance, master: false)
        }));
        public HashSet<IDbConnection> Disposed { get; } = new();
        public SqlExecutedContext Last { get; private set; }
        public int Timeout { get; private set; }
        public Fixture()
        {
            Factory.SqlMonitor.SqlExecuting += context =>
            {
                _connections.Add(context.Connection);
                Timeout = new SQLiteConnectionStringBuilder(context.Connection.ConnectionString).DefaultTimeout;
                ((DbConnection)context.Connection).Disposed += (_, _) => Disposed.Add(context.Connection);
            };
            Factory.SqlMonitor.SqlExecuted += context => Last = context;
        }
        public void Dispose()
        {
            foreach (var connection in _connections.Distinct()) connection.Dispose();
        }
    }
}
