using System;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository.Test;

/// <summary>
/// 直接验证 Adapter 入口；仓储当前使用 Reader，不能由仓储测试替代。
/// </summary>
[TestClass]
public class DataAdapterExecutionTest
{
    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task Fill_ReturnsResultsAndPreservesCallerResources(bool asynchronous, bool dataSet)
    {
        foreach (var initiallyOpen in new[] { false, true })
        {
            using var connection = new SQLiteConnection("Data Source=:memory:;Pooling=False;");
            if (initiallyOpen) connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT @Value AS Value UNION ALL SELECT NULL; SELECT 42 AS Other";
            command.Parameters.AddWithValue("@Value", 41L);
            using var adapter = new SQLiteDataAdapter();
            var disposed = false;
            command.Disposed += (_, _) => disposed = true;
            var monitor = new DefaultSqlMonitor();
            var started = 0;
            var completed = 0;
            monitor.SqlExecuting += context => { started++; context.Handled = true; };
            monitor.SqlExecuted += context =>
            {
                completed++;
                context.Handled = true;
                Assert.IsNull(context.Exception);
                Assert.AreSame(connection, context.Connection);
                Assert.AreSame(command.Parameters, context.SqlParameter);
                Assert.AreEqual(command.CommandText, context.Sql);
            };
            using var result = await Fill(adapter, command, monitor, asynchronous, dataSet);
            var table = dataSet ? ((DataSet)result).Tables[0] : (DataTable)result;
            Assert.AreEqual("Value", table.Columns[0].ColumnName);
            Assert.AreEqual(2, table.Rows.Count);
            Assert.AreEqual(41L, table.Rows[0][0]);
            Assert.AreEqual(DBNull.Value, table.Rows[1][0]);
            if (dataSet)
            {
                var set = (DataSet)result;
                Assert.AreEqual(2, set.Tables.Count);
                Assert.AreEqual("Other", set.Tables[1].Columns[0].ColumnName);
                Assert.AreEqual(42L, set.Tables[1].Rows[0][0]);
            }
            Assert.AreEqual(1, started);
            Assert.AreEqual(1, completed);
            Assert.IsFalse(disposed);
            Assert.AreSame(command, adapter.SelectCommand);
            Assert.AreEqual(initiallyOpen ? ConnectionState.Open : ConnectionState.Closed, connection.State);
        }
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task FillFailure_ReportsOriginalExceptionAndAllowsCommandReuse(bool asynchronous, bool dataSet)
    {
        foreach (var initiallyOpen in new[] { false, true })
        {
            using var connection = new SQLiteConnection("Data Source=:memory:;Pooling=False;");
            if (initiallyOpen) connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM missing_table";
            using var adapter = new SQLiteDataAdapter();
            var monitor = new DefaultSqlMonitor();
            Exception observed = null;
            var completed = 0;
            monitor.SqlExecuting += context => context.Handled = true;
            monitor.SqlExecuted += context =>
            {
                context.Handled = true;
                observed = context.Exception;
                completed++;
            };
            var error = await Assert.ThrowsAsync<SQLiteException>(async () =>
            {
                using var result = await Fill(adapter, command, monitor, asynchronous, dataSet);
            });
            Assert.AreSame(error, observed);
            Assert.AreEqual(1, completed);
            Assert.AreEqual(initiallyOpen ? ConnectionState.Open : ConnectionState.Closed, connection.State);
            // 实际再次执行，验证失败没有释放调用方命令或使连接不可用。
            command.CommandText = "SELECT 7 AS Value";
            using var recovered = await Fill(adapter, command, null, asynchronous, dataSet);
            var table = dataSet ? ((DataSet)recovered).Tables[0] : (DataTable)recovered;
            Assert.AreEqual(7L, table.Rows[0][0]);
        }
    }

    private static async Task<IDisposable> Fill(SQLiteDataAdapter adapter, SQLiteCommand command,
        ISqlMonitor monitor, bool asynchronous, bool dataSet)
    {
        // Async 入口目前仍调用同步 Fill；这里只验证其结果和异常契约，不宣称异步 I/O。
        if (dataSet) return asynchronous
            ? await adapter.ExecuteDataSetAsync(command, monitor) : adapter.ExecuteDataSet(command, monitor);
        return asynchronous
            ? await adapter.ExecuteDataTableAsync(command, monitor) : adapter.ExecuteDataTable(command, monitor);
    }
}
