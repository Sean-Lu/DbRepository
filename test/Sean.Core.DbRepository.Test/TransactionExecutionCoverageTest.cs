using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sean.Core.DbRepository.Test;

/// <summary>
/// 用独立 SQLite 文件检查事务完成后的真实数据，避免仅凭回调返回值判断提交或回滚。
/// </summary>
[TestClass]
public class TransactionExecutionCoverageTest
{
    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task AutoTransaction_CommitsOnlyOnTrue(bool asynchronous, bool externalConnection)
    {
        foreach (var outcome in new[] { "true", "false", "throw" })
        {
            using var fixture = new Fixture();
            using var connection = externalConnection ? fixture.Repository.Factory.OpenNewConnection() : null;
            var expected = new InvalidOperationException("自动事务回调失败");
            bool Callback(IDbTransaction transaction)
            {
                fixture.Insert(transaction);
                if (outcome == "throw") throw expected;
                return outcome == "true";
            }
            async Task<bool> Execute()
            {
                if (!asynchronous) return fixture.Repository.ExecuteAutoTransaction(Callback, connection: connection);
                return await fixture.Repository.ExecuteAutoTransactionAsync(async transaction =>
                {
                    await Task.Yield();
                    return Callback(transaction);
                }, connection: connection);
            }
            if (outcome == "throw") Assert.AreSame(expected, await Assert.ThrowsAsync<InvalidOperationException>(Execute));
            else Assert.AreEqual(outcome == "true", await Execute());

            Assert.AreEqual(outcome == "true" ? 1L : 0L, fixture.Count());
            Assert.AreEqual(!externalConnection, fixture.ConnectionDisposed);
            if (externalConnection) Assert.AreEqual(ConnectionState.Open, connection.State);
        }
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task ManualTransaction_RequiresExplicitCommit(bool asynchronous, bool externalConnection)
    {
        foreach (var outcome in new[] { "commit", "no-commit", "throw" })
        {
            using var fixture = new Fixture();
            using var connection = externalConnection ? fixture.Repository.Factory.OpenNewConnection() : null;
            var expected = new InvalidOperationException("手动事务回调失败");
            int Callback(IDbTransaction transaction)
            {
                fixture.Insert(transaction);
                if (outcome == "throw") throw expected;
                // ExecuteTransaction 并非自动提交 API，保护原先由调用方显式提交的约定。
                if (outcome == "commit") transaction.Commit();
                return 73;
            }
            async Task<int> Execute()
            {
                if (!asynchronous) return fixture.Repository.ExecuteTransaction(Callback, connection: connection);
                return await fixture.Repository.ExecuteTransactionAsync(async transaction =>
                {
                    await Task.Yield();
                    return Callback(transaction);
                }, connection: connection);
            }
            if (outcome == "throw") Assert.AreSame(expected, await Assert.ThrowsAsync<InvalidOperationException>(Execute));
            else Assert.AreEqual(73, await Execute());

            Assert.AreEqual(outcome == "commit" ? 1L : 0L, fixture.Count());
            Assert.AreEqual(!externalConnection, fixture.ConnectionDisposed);
            if (externalConnection) Assert.AreEqual(ConnectionState.Open, connection.State);
        }
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task CallerTransaction_RemainsUnderCallerControl(bool asynchronous, bool autoTransaction)
    {
        foreach (var outcome in new[] { "true", "false", "throw" })
        {
            using var fixture = new Fixture();
            using var connection = fixture.Repository.Factory.OpenNewConnection();
            using var transaction = connection.BeginTransaction();
            var expected = new InvalidOperationException("外部事务回调失败");
            bool Callback(IDbTransaction actual)
            {
                Assert.AreSame(transaction, actual);
                fixture.Insert(actual);
                if (outcome == "throw") throw expected;
                return outcome == "true";
            }
            async Task<bool> AsyncCallback(IDbTransaction actual)
            {
                await Task.Yield();
                return Callback(actual);
            }
            async Task<bool> Execute()
            {
                if (asynchronous)
                    return autoTransaction
                        ? await fixture.Repository.ExecuteAutoTransactionAsync(AsyncCallback, transaction)
                        : await fixture.Repository.ExecuteTransactionAsync(AsyncCallback, transaction);
                return autoTransaction
                    ? fixture.Repository.ExecuteAutoTransaction(Callback, transaction)
                    : fixture.Repository.ExecuteTransaction(Callback, transaction);
            }
            if (outcome == "throw") Assert.AreSame(expected, await Assert.ThrowsAsync<InvalidOperationException>(Execute));
            else Assert.AreEqual(outcome == "true", await Execute());

            Assert.IsFalse(fixture.ConnectionDisposed);
            Assert.AreSame(connection, transaction.Connection);
            Assert.AreEqual(1L, fixture.Repository.Factory.ExecuteScalar<long>(transaction, "SELECT COUNT(*) FROM sample"));
            // 即使回调返回 false 或抛错，外部事务仍由调用方决定；主动回滚验证它没有被提前提交。
            transaction.Rollback();
            Assert.AreEqual(0L, fixture.Count());
        }
    }

    [TestMethod]
    public async Task TransactionEntries_RejectNullCallback()
    {
        using var fixture = new Fixture();
        Assert.AreEqual("func", Assert.Throws<ArgumentNullException>(() => fixture.Repository.ExecuteTransaction<int>(null)).ParamName);
        Assert.AreEqual("func", Assert.Throws<ArgumentNullException>(() => fixture.Repository.ExecuteAutoTransaction(null)).ParamName);
        Assert.AreEqual("func", (await Assert.ThrowsAsync<ArgumentNullException>(() => fixture.Repository.ExecuteTransactionAsync<int>(null))).ParamName);
        Assert.AreEqual("func", (await Assert.ThrowsAsync<ArgumentNullException>(() => fixture.Repository.ExecuteAutoTransactionAsync(null))).ParamName);
    }

    private sealed class Fixture : IDisposable
    {
        private readonly string _path = Path.Combine(Path.GetTempPath(), $"Sean.Transaction.Test.{Guid.NewGuid():N}.db");
        private readonly List<IDbConnection> _connections = new();
        public Repository Repository { get; }
        public bool ConnectionDisposed { get; private set; }
        public Fixture()
        {
            Repository = new Repository($"Data Source={_path};Pooling=False;");
            Repository.Factory.ExecuteNonQuery("CREATE TABLE sample (Value INTEGER)");
        }
        public void Insert(IDbTransaction transaction)
        {
            _connections.Add(transaction.Connection);
            ((DbConnection)transaction.Connection).Disposed += (_, _) => ConnectionDisposed = true;
            Assert.AreEqual(1, Repository.Factory.ExecuteNonQuery(transaction, "INSERT INTO sample (Value) VALUES (41)"));
        }
        public long Count() => Repository.Factory.ExecuteScalar<long>("SELECT COUNT(*) FROM sample");
        public void Dispose()
        {
            foreach (var connection in _connections) connection.Dispose();
            // 不启用连接池，仅删除本用例创建的唯一文件，不触碰共享 test.db。
            File.Delete(_path);
        }
    }

    private sealed class Repository(string connectionString) : BaseRepository(connectionString, SQLiteFactory.Instance);
}
