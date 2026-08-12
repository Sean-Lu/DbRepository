using System.Data.SQLite;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository.Test;

/// <summary>
/// DataReader 限量读取行为的回归测试。
/// </summary>
[TestClass]
public class DataReaderReadCountCorrectnessTest
{
    [TestMethod]
    public void GetList_WithPositiveLimit_DoesNotConsumeTheNextRow()
    {
        using var connection = OpenConnection();
        using var command = CreateSequenceCommand(connection);
        using var reader = command.ExecuteReader();

        var values = reader.GetList<long>(1);

        Assert.AreEqual(1, values.Count);
        Assert.AreEqual(1L, values[0]);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(2L, reader.GetInt64(0));
    }

    [TestMethod]
    public void Get_OnlyConsumesTheReturnedRow()
    {
        using var connection = OpenConnection();
        using var command = CreateSequenceCommand(connection);
        using var reader = command.ExecuteReader();

        Assert.AreEqual(1L, reader.Get<long>());
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(2L, reader.GetInt64(0));
    }

    [TestMethod]
    public async Task GetListAsync_WithPositiveLimit_DoesNotConsumeTheNextRow()
    {
        using var connection = OpenConnection();
        using var command = CreateSequenceCommand(connection);
        using var reader = await command.ExecuteReaderAsync();

        var values = await reader.GetListAsync<long>(2);

        Assert.AreEqual(2, values.Count);
        Assert.AreEqual(1L, values[0]);
        Assert.AreEqual(2L, values[1]);
        Assert.IsTrue(await reader.ReadAsync());
        Assert.AreEqual(3L, reader.GetInt64(0));
    }

    [TestMethod]
    public async Task GetAsync_OnlyConsumesTheReturnedRow()
    {
        using var connection = OpenConnection();
        using var command = CreateSequenceCommand(connection);
        using var reader = await command.ExecuteReaderAsync();

        Assert.AreEqual(1L, await reader.GetAsync<long>());
        Assert.IsTrue(await reader.ReadAsync());
        Assert.AreEqual(2L, reader.GetInt64(0));
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow(0)]
    [DataRow(-1)]
    public void GetList_WithNonPositiveOrNullLimit_PreservesReadAllBehavior(int? readCount)
    {
        using var connection = OpenConnection();
        using var command = CreateSequenceCommand(connection);
        using var reader = command.ExecuteReader();

        var values = reader.GetList<long>(readCount);

        CollectionAssert.AreEqual(new[] { 1L, 2L, 3L }, values);
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow(0)]
    [DataRow(-1)]
    public async Task GetListAsync_WithNonPositiveOrNullLimit_PreservesReadAllBehavior(int? readCount)
    {
        using var connection = OpenConnection();
        using var command = CreateSequenceCommand(connection);
        using var reader = await command.ExecuteReaderAsync();

        var values = await reader.GetListAsync<long>(readCount);

        CollectionAssert.AreEqual(new[] { 1L, 2L, 3L }, values);
    }

    private static SQLiteConnection OpenConnection()
    {
        var connection = new SQLiteConnection("Data Source=:memory:;Version=3;New=True;");
        connection.Open();
        return connection;
    }

    private static SQLiteCommand CreateSequenceCommand(SQLiteConnection connection)
    {
        var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3;";
        return command;
    }
}
