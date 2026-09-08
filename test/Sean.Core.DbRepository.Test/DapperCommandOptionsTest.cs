using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sean.Core.DbRepository.Dapper;

namespace Sean.Core.DbRepository.Test;

/// <summary>
/// 仅验证 ADO.NET 命令配置和输出回写契约，不模拟数据库的存储过程实现。
/// </summary>
[TestClass]
public class DapperCommandOptionsTest
{
    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task AllEntries_ForwardCommandOptionsAndDisposeCommand(bool generic, bool asynchronous)
    {
        var repository = CreateRepository(generic);
        using var connection = new ProbeConnection();
        connection.Open();
        foreach (var type in new[] { CommandType.Text, CommandType.StoredProcedure })
        foreach (var timeout in new int?[] { null, 19 })
        {
            var command = new DefaultSqlCommand("sample_command")
            {
                Connection = connection, CommandType = type, CommandTimeout = timeout
            };
            // 默认超时来自提供程序；这里只设置命令级超时，不改变仓储现有默认值语义。
            foreach (var invoke in Entries(repository, asynchronous))
            {
                var result = await invoke(command);
                (result as IDisposable)?.Dispose();
                var actual = connection.Commands.Last();
                Assert.AreEqual(type, actual.CommandType);
                Assert.AreEqual(timeout ?? 30, actual.CommandTimeout);
                Assert.AreEqual(command.Sql, actual.CommandText);
                Assert.IsTrue(actual.WasDisposed);
                Assert.AreEqual(ConnectionState.Open, connection.State);
            }
        }
        Assert.AreEqual(32, connection.Commands.Count);
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task Execute_OutputParameterIsWrittenBackOnlyAfterSuccess(bool generic, bool asynchronous)
    {
        var repository = CreateRepository(generic);
        using var connection = new ProbeConnection();
        connection.Open();
        var target = new OutputTarget { Value = -1 };
        var parameters = new DynamicParameters();
        parameters.Add(nameof(OutputTarget.Value), dbType: DbType.Int64, direction: ParameterDirection.Output);
        var command = new DefaultSqlCommand("sample_command", parameters)
        {
            Connection = connection,
            CommandType = CommandType.StoredProcedure,
            OutputParameterOptions = new OutputParameterOptions<OutputTarget>
            {
                OutputTarget = target,
                OutputPropertyInfo = typeof(OutputTarget).GetProperty(nameof(OutputTarget.Value))
            }
        };
        var count = asynchronous ? await repository.ExecuteAsync(command) : repository.Execute(command);
        Assert.AreEqual(1, count);
        Assert.AreEqual(55, target.Value);
        Assert.AreEqual(ParameterDirection.Output, connection.Commands.Last().OutputDirection);
        Assert.IsTrue(connection.Commands.Last().WasDisposed);

        target.Value = -1;
        connection.Failure = new InvalidOperationException("模拟执行失败");
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            if (asynchronous) await repository.ExecuteAsync(command);
            else repository.Execute(command);
        });
        Assert.AreSame(connection.Failure, exception);
        Assert.AreEqual(-1, target.Value);
        Assert.IsTrue(connection.Commands.Last().WasDisposed);
        Assert.AreEqual(ConnectionState.Open, connection.State);
    }

    private static IEnumerable<Func<ISqlCommand, Task<object>>> Entries(BaseRepository repository, bool asynchronous)
    {
        if (asynchronous)
        {
            yield return async c => await repository.ExecuteAsync(c);
            yield return async c => await repository.QueryAsync<int>(c);
            yield return async c => await repository.GetAsync<int>(c);
            yield return async c => await repository.ExecuteScalarAsync<int>(c);
            yield return async c => await repository.ExecuteScalarAsync(c);
            yield return async c => await repository.ExecuteDataTableAsync(c);
            yield return async c => await repository.ExecuteDataSetAsync(c);
            yield return async c => await repository.ExecuteReaderAsync(c);
        }
        else
        {
            yield return c => Task.FromResult<object>(repository.Execute(c));
            yield return c => Task.FromResult<object>(repository.Query<int>(c));
            yield return c => Task.FromResult<object>(repository.Get<int>(c));
            yield return c => Task.FromResult<object>(repository.ExecuteScalar<int>(c));
            yield return c => Task.FromResult(repository.ExecuteScalar(c));
            yield return c => Task.FromResult<object>(repository.ExecuteDataTable(c));
            yield return c => Task.FromResult<object>(repository.ExecuteDataSet(c));
            yield return c => Task.FromResult<object>(repository.ExecuteReader(c));
        }
    }

    private static BaseRepository CreateRepository(bool generic) => generic ? new GenericRepository() : new Repository();
    private sealed class Repository() : DapperBaseRepository("Data Source=:memory:", SQLiteFactory.Instance);
    private sealed class GenericRepository() : DapperBaseRepository<OutputTarget>("Data Source=:memory:", SQLiteFactory.Instance);
    private sealed class OutputTarget { public int Value { get; set; } }

    private sealed class ProbeConnection : DbConnection
    {
        private ConnectionState _state;
        public List<ProbeCommand> Commands { get; } = new();
        public Exception Failure { get; set; }
        public override string ConnectionString { get; set; } = "probe";
        public override string Database => "probe";
        public override string DataSource => "probe";
        public override string ServerVersion => "1";
        public override ConnectionState State => _state;
        public override void Open() => _state = ConnectionState.Open;
        public override void Close() => _state = ConnectionState.Closed;
        protected override void Dispose(bool disposing)
        {
            if (disposing) Close();
            base.Dispose(disposing);
        }
        public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
        protected override DbCommand CreateDbCommand()
        {
            var command = new ProbeCommand(this);
            Commands.Add(command);
            return command;
        }
    }

    private sealed class ProbeCommand(ProbeConnection owner) : DbCommand
    {
        private readonly Parameters _parameters = new();
        private DataTable _table;
        public bool WasDisposed { get; private set; }
        public ParameterDirection? OutputDirection { get; private set; }
        public override string CommandText { get; set; }
        public override int CommandTimeout { get; set; } = 30;
        public override CommandType CommandType { get; set; } = CommandType.Text;
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection DbConnection { get; set; } = owner;
        protected override DbTransaction DbTransaction { get; set; }
        protected override DbParameterCollection DbParameterCollection => _parameters;
        public override void Cancel() { }
        public override void Prepare() { }
        protected override DbParameter CreateDbParameter() => new Parameter();
        public override int ExecuteNonQuery()
        {
            // 即使提供程序已经赋值，只要执行抛错，ORM 就不应回写调用方目标。
            foreach (DbParameter parameter in _parameters)
                if (parameter.Direction == ParameterDirection.Output)
                {
                    // Dapper 同步路径会清空参数集合，记录执行时的真实方向而非释放后的集合。
                    OutputDirection = parameter.Direction;
                    parameter.Value = 55L;
                }
            if (owner.Failure != null) throw owner.Failure;
            return 1;
        }
        public override object ExecuteScalar() => 7;
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            _table = new DataTable();
            _table.Columns.Add("Value", typeof(int));
            _table.Rows.Add(7);
            return _table.CreateDataReader();
        }
        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            if (disposing) _table?.Dispose();
            base.Dispose(disposing);
        }
    }

    private sealed class Parameter : DbParameter
    {
        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;
        public override bool IsNullable { get; set; }
        public override string ParameterName { get; set; }
        public override string SourceColumn { get; set; }
        public override object Value { get; set; }
        public override bool SourceColumnNullMapping { get; set; }
        public override int Size { get; set; }
        public override void ResetDbType() { }
    }

    private sealed class Parameters : DbParameterCollection
    {
        private readonly List<DbParameter> _items = new();
        public override int Count => _items.Count;
        public override object SyncRoot => ((ICollection)_items).SyncRoot;
        public override int Add(object value) { _items.Add((DbParameter)value); return _items.Count - 1; }
        public override void AddRange(Array values) { foreach (var value in values) Add(value); }
        public override void Clear() => _items.Clear();
        public override bool Contains(object value) => _items.Contains((DbParameter)value);
        public override bool Contains(string value) => IndexOf(value) >= 0;
        public override void CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
        public override IEnumerator GetEnumerator() => _items.GetEnumerator();
        public override int IndexOf(object value) => _items.IndexOf((DbParameter)value);
        public override int IndexOf(string parameterName) => _items.FindIndex(p => p.ParameterName == parameterName);
        public override void Insert(int index, object value) => _items.Insert(index, (DbParameter)value);
        public override void Remove(object value) => _items.Remove((DbParameter)value);
        public override void RemoveAt(int index) => _items.RemoveAt(index);
        public override void RemoveAt(string parameterName) => RemoveAt(IndexOf(parameterName));
        protected override DbParameter GetParameter(int index) => _items[index];
        protected override DbParameter GetParameter(string parameterName) => _items[IndexOf(parameterName)];
        protected override void SetParameter(int index, DbParameter value) => _items[index] = value;
        protected override void SetParameter(string parameterName, DbParameter value)
        {
            var index = IndexOf(parameterName);
            if (index < 0) _items.Add(value);
            else _items[index] = value;
        }
    }
}
