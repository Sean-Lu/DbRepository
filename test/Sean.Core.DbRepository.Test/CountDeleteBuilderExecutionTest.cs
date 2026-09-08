using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SQLite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sean.Core.DbRepository.Test;

[TestClass]
public class CountDeleteBuilderExecutionTest
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void JoinedCount_KeepsSameNamedFieldParametersSeparate(bool filterGroup)
    {
        using var connection = OpenDatabase();
        var repository = new Repository();
        var builder = CountableSqlBuilder<Item>.Create(DatabaseType.SQLite)
            .InnerJoin<Group>(item => item.GroupId, group => group.Id, "g")
            .Where(item => item.Name == "item")
            .WhereIF<Group>(filterGroup, group => group.Name == "chosen", "g");
        var command = builder.Build();
        command.Connection = connection;
        Assert.AreEqual(filterGroup ? 1L : 2L, repository.ExecuteScalar<long>(command));
        // 重复 Build 不能重复追加延迟条件或破坏已合并的参数。
        var repeated = builder.Build();
        repeated.Connection = connection;
        Assert.AreEqual(command.Sql, repeated.Sql);
        Assert.AreEqual(filterGroup ? 1L : 2L, repository.ExecuteScalar<long>(repeated));
    }

    [TestMethod]
    public void Delete_DisabledOnlyConditionRequiresExplicitFullTablePermission()
    {
        using var connection = OpenDatabase();
        var repository = new Repository();
        var builder = DeleteableSqlBuilder<Item>.Create(DatabaseType.SQLite)
            .WhereIF(false, item => item.Name == "item");
        Assert.Throws<ArgumentException>(() => builder.Build());
        Assert.AreEqual(3L, repository.ExecuteScalar<long>("SELECT COUNT(*) FROM items", connection: connection));
        var command = builder.AllowEmptyWhereClause().Build();
        command.Connection = connection;
        Assert.AreEqual(3, repository.Execute(command));
        Assert.AreEqual(0L, repository.ExecuteScalar<long>("SELECT COUNT(*) FROM items", connection: connection));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Delete_ConditionalBranchOnlyRemovesMatchingRows(bool deleteItems)
    {
        using var connection = OpenDatabase();
        var repository = new Repository();
        var command = DeleteableSqlBuilder<Item>.Create(DatabaseType.SQLite)
            .WhereIF(deleteItems, item => item.Name == "item", item => item.Name == "other")
            .Build();
        command.Connection = connection;
        Assert.AreEqual(deleteItems ? 2 : 1, repository.Execute(command));
        Assert.AreEqual(deleteItems ? 1L : 2L, repository.ExecuteScalar<long>("SELECT COUNT(*) FROM items", connection: connection));
        Assert.AreEqual(deleteItems ? "other" : "item", repository.ExecuteScalar<string>("SELECT Name FROM items", connection: connection));
    }

    private static SQLiteConnection OpenDatabase()
    {
        var connection = new SQLiteConnection("Data Source=:memory:;Pooling=False;");
        try
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE items (Id INTEGER, GroupId INTEGER, Name TEXT);"
                + "CREATE TABLE groups (Id INTEGER, Name TEXT);"
                + "INSERT INTO groups VALUES (1, 'chosen'), (2, 'other');"
                + "INSERT INTO items VALUES (1, 1, 'item'), (2, 2, 'item'), (3, 1, 'other');";
            command.ExecuteNonQuery();
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    [Table("items")]
    private sealed class Item
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public string Name { get; set; }
    }
    [Table("groups")]
    private sealed class Group
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    private sealed class Repository() : BaseRepository("Data Source=:memory:;Pooling=False;", SQLiteFactory.Instance);
}
