using System;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sean.Utility.Format;

namespace Sean.Core.DbRepository.Test;

[TestClass]
public class GuidScalarConversionTest
{
    [TestMethod]
    [DataRow(0, false)]
    [DataRow(0, true)]
    [DataRow(1, false)]
    [DataRow(1, true)]
    [DataRow(2, false)]
    [DataRow(2, true)]
    public async Task Scalar_ConvertsGuidTextAndPreservesOtherConversions(int entry, bool asynchronous)
    {
        using var connection = new SQLiteConnection("Data Source=:memory:;Pooling=False;");
        connection.Open();
        using var transaction = entry == 1 ? connection.BeginTransaction() : null;
        var factory = new DbFactory(connection.ConnectionString, SQLiteFactory.Instance);
        async Task<T> Execute<T>(string sql)
        {
            if (entry == 0) return asynchronous ? await factory.ExecuteScalarAsync<T>(connection, sql)
                : factory.ExecuteScalar<T>(connection, sql);
            if (entry == 1) return asynchronous ? await factory.ExecuteScalarAsync<T>(transaction, sql)
                : factory.ExecuteScalar<T>(transaction, sql);
            var command = new DefaultSqlCommand(sql) { Connection = connection };
            return asynchronous ? await factory.ExecuteScalarAsync<T>(command) : factory.ExecuteScalar<T>(command);
        }
        var expected = Guid.Parse("6a7010f5-2d91-4a04-b2d1-47ead5908d42");
        foreach (var format in new[] { "D", "N" })
        {
            var sql = $"SELECT '{expected.ToString(format)}'";
            Assert.AreEqual(expected, await Execute<Guid>(sql));
            Assert.AreEqual(expected, await Execute<Guid?>(sql));
        }
        // SQL NULL 与没有返回行不同，均保持原标量转换器的默认值行为。
        foreach (var sql in new[] { "SELECT NULL", "SELECT 1 WHERE 1=0" })
        {
            var raw = entry == 1 ? factory.ExecuteScalar(transaction, sql) : factory.ExecuteScalar(connection, sql);
            Assert.AreEqual(ObjectConvert.ChangeType<Guid>(raw), await Execute<Guid>(sql));
            Assert.AreEqual(ObjectConvert.ChangeType<Guid?>(raw), await Execute<Guid?>(sql));
        }
        Assert.AreEqual(42, await Execute<int>("SELECT '42'"));
        Assert.AreEqual("text", await Execute<string>("SELECT 'text'"));
        await Assert.ThrowsAsync<InvalidCastException>(async () => await Execute<Guid>("SELECT 'invalid'"));
        await Assert.ThrowsAsync<InvalidCastException>(async () => await Execute<Guid?>("SELECT 'invalid'"));
        Assert.AreEqual(ConnectionState.Open, connection.State);
        if (transaction != null) Assert.AreSame(connection, transaction.Connection);
    }
}
