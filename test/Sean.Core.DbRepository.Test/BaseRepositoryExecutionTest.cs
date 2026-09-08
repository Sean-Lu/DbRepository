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
/// 保护原生仓储的字符串重载到命令入口的转发，不替代 Dapper 专项测试。
/// </summary>
[TestClass]
public class BaseRepositoryExecutionTest
{
    private enum Operation { Execute, Query, Get, Scalar, UntypedScalar, DataTable, DataSet, Reader }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task Entries_ForwardParametersConnectionAndTransaction(bool asynchronous, bool rawSql)
    {
        using var repository = new ProbeRepository();
        foreach (var route in new[] { "internal", "connection", "transaction" })
        {
            using var connection = new SQLiteConnection("Data Source=:memory:;Pooling=False;Default Timeout=17;");
            if (route == "transaction") connection.Open();
            using var transaction = route == "transaction" ? connection.BeginTransaction() : null;
            foreach (var operation in Enum.GetValues<Operation>())
            {
                var sql = operation == Operation.Execute ? "CREATE TABLE sample (Value INTEGER)"
                    : "SELECT @Value AS Value UNION ALL SELECT @Value + 1 AS Value";
                var result = await Run(repository, operation, asynchronous, rawSql, sql,
                    route == "connection" ? connection : null, transaction);
                AssertResult(operation, result);
                Assert.AreEqual(route == "internal" ? 29 : 17, repository.ConnectionTimeout);
                Assert.AreSame(transaction, repository.Last.Transaction);
                Assert.IsNull(repository.Last.Exception);
                if (route == "internal")
                {
                    // Query/Get 也通过 Reader 的 CloseConnection 关闭连接，不能强制要求 Disposed 事件。
                    if (operation is Operation.Reader or Operation.Query or Operation.Get)
                        Assert.AreEqual(ConnectionState.Closed, repository.Last.Connection.State);
                    else Assert.IsTrue(repository.Disposed.Contains(repository.Last.Connection), operation.ToString());
                }
                else
                {
                    Assert.AreSame(connection, repository.Last.Connection);
                    Assert.AreEqual(ConnectionState.Open, connection.State);
                    Assert.IsFalse(repository.Disposed.Contains(connection));
                }
            }
            transaction?.Rollback();
        }
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task Entries_RejectMissingSqlOrCommand(bool asynchronous, bool rawSql)
    {
        using var repository = new ProbeRepository();
        foreach (var sql in rawSql ? new[] { null, string.Empty, "  " } : new string[] { null })
        foreach (var operation in Enum.GetValues<Operation>())
        {
            if (rawSql)
            {
                var error = await Assert.ThrowsAsync<ArgumentException>(async () =>
                    await Run(repository, operation, asynchronous, rawSql, sql));
                Assert.AreEqual("sql", error.ParamName);
            }
            else
            {
                var error = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                    await Run(repository, operation, asynchronous, rawSql, sql));
                Assert.AreEqual("sqlCommand", error.ParamName);
            }
        }
        Assert.IsNull(repository.Last);
    }

    private static async Task<object> Run(BaseRepository repository, Operation operation, bool asynchronous,
        bool rawSql, string sql, IDbConnection connection = null, IDbTransaction transaction = null)
    {
        var parameters = new { Value = 41L };
        ISqlCommand command = sql == null ? null : new DefaultSqlCommand(sql, parameters)
        {
            Master = false, Connection = connection, Transaction = transaction
        };
        return (rawSql, asynchronous) switch
        {
            (false, false) => operation switch
            {
                Operation.Execute => repository.Execute(command),
                Operation.Query => repository.Query<long>(command),
                Operation.Get => repository.Get<long>(command),
                Operation.Scalar => repository.ExecuteScalar<long>(command),
                Operation.UntypedScalar => repository.ExecuteScalar(command),
                Operation.DataTable => repository.ExecuteDataTable(command),
                Operation.DataSet => repository.ExecuteDataSet(command),
                Operation.Reader => repository.ExecuteReader(command),
                _ => throw new ArgumentOutOfRangeException(nameof(operation))
            },
            (false, true) => operation switch
            {
                Operation.Execute => await repository.ExecuteAsync(command),
                Operation.Query => await repository.QueryAsync<long>(command),
                Operation.Get => await repository.GetAsync<long>(command),
                Operation.Scalar => await repository.ExecuteScalarAsync<long>(command),
                Operation.UntypedScalar => await repository.ExecuteScalarAsync(command),
                Operation.DataTable => await repository.ExecuteDataTableAsync(command),
                Operation.DataSet => await repository.ExecuteDataSetAsync(command),
                Operation.Reader => await repository.ExecuteReaderAsync(command),
                _ => throw new ArgumentOutOfRangeException(nameof(operation))
            },
            (true, false) => operation switch
            {
                Operation.Execute => repository.Execute(sql, parameters, master: false, transaction: transaction, connection: connection),
                Operation.Query => repository.Query<long>(sql, parameters, master: false, transaction: transaction, connection: connection),
                Operation.Get => repository.Get<long>(sql, parameters, master: false, transaction: transaction, connection: connection),
                Operation.Scalar => repository.ExecuteScalar<long>(sql, parameters, master: false, transaction: transaction, connection: connection),
                Operation.UntypedScalar => repository.ExecuteScalar(sql, parameters, master: false, transaction: transaction, connection: connection),
                Operation.DataTable => repository.ExecuteDataTable(sql, parameters, master: false, transaction: transaction, connection: connection),
                Operation.DataSet => repository.ExecuteDataSet(sql, parameters, master: false, transaction: transaction, connection: connection),
                Operation.Reader => repository.ExecuteReader(sql, parameters, master: false, transaction: transaction, connection: connection),
                _ => throw new ArgumentOutOfRangeException(nameof(operation))
            },
            (true, true) => operation switch
            {
                Operation.Execute => await repository.ExecuteAsync(sql, parameters, master: false, transaction: transaction, connection: connection),
                Operation.Query => await repository.QueryAsync<long>(sql, parameters, master: false, transaction: transaction, connection: connection),
                Operation.Get => await repository.GetAsync<long>(sql, parameters, master: false, transaction: transaction, connection: connection),
                Operation.Scalar => await repository.ExecuteScalarAsync<long>(sql, parameters, master: false, transaction: transaction, connection: connection),
                Operation.UntypedScalar => await repository.ExecuteScalarAsync(sql, parameters, master: false, transaction: transaction, connection: connection),
                Operation.DataTable => await repository.ExecuteDataTableAsync(sql, parameters, master: false, transaction: transaction, connection: connection),
                Operation.DataSet => await repository.ExecuteDataSetAsync(sql, parameters, master: false, transaction: transaction, connection: connection),
                Operation.Reader => await repository.ExecuteReaderAsync(sql, parameters, master: false, transaction: transaction, connection: connection),
                _ => throw new ArgumentOutOfRangeException(nameof(operation))
            }
        };
    }

    private static void AssertResult(Operation operation, object result)
    {
        switch (operation)
        {
            case Operation.Execute:
                Assert.AreEqual(0, result);
                break;
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
            case Operation.Reader:
                using (var reader = (IDataReader)result)
                {
                    Assert.IsTrue(reader.Read());
                    Assert.AreEqual(41L, reader.GetInt64(0));
                    Assert.IsTrue(reader.Read());
                    Assert.AreEqual(42L, reader.GetInt64(0));
                    Assert.IsFalse(reader.Read());
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

    private sealed class ProbeRepository : BaseRepository, IDisposable
    {
        private readonly List<IDbConnection> _connections = new();
        public HashSet<IDbConnection> Disposed { get; } = new();
        public SqlExecutedContext Last { get; private set; }
        public int ConnectionTimeout { get; private set; }
        public ProbeRepository() : base(new MultiConnectionSettings(new[]
        {
            new ConnectionStringOptions("Data Source=:memory:;Pooling=False;Default Timeout=17;", SQLiteFactory.Instance),
            new ConnectionStringOptions("Data Source=:memory:;Pooling=False;Default Timeout=29;", SQLiteFactory.Instance, master: false)
        }))
        {
            Factory.SqlMonitor.SqlExecuting += context =>
            {
                _connections.Add(context.Connection);
                ConnectionTimeout = new SQLiteConnectionStringBuilder(context.Connection.ConnectionString).DefaultTimeout;
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
