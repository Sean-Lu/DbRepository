using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SQLite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sean.Core.DbRepository.Test;

[TestClass]
public class UpdateBuilderExecutionTest
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Update_SameFieldInSetAndWherePreservesOldAndNewValues(bool parameterized)
    {
        using var fixture = new Fixture();
        var builder = UpdateableSqlBuilder<Row>.Create(DatabaseType.SQLite)
            .UpdateFields(row => row.Name)
            .SetParameter(new { Name = "O'Brien" })
            .Where(row => row.Name == "old")
            .SetSqlParameterized(parameterized);
        var command = builder.Build();
        command.Connection = fixture.Connection;
        Assert.AreEqual(1, fixture.Repository.Execute(command));
        Assert.AreEqual("O'Brien", fixture.Name(7));
        Assert.AreEqual("keep", fixture.Name(8));
        Assert.AreEqual(20L, fixture.Repository.ExecuteScalar<long>("SELECT Amount FROM update_rows WHERE row_id = 7", connection: fixture.Connection));
        // 重复构建后的 WHERE 仍匹配旧值，不能被 SET 值覆盖而再次更新同一行。
        var repeated = builder.Build();
        repeated.Connection = fixture.Connection;
        Assert.AreEqual(0, fixture.Repository.Execute(repeated));
        Assert.AreEqual(command.Sql, repeated.Sql);
    }

    [TestMethod]
    public void Execution_DoesNotRemoveCallerParametersNeededByNextCommand()
    {
        using var fixture = new Fixture();
        var parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["Id"] = 7,
            ["Name"] = "changed"
        };
        Assert.AreEqual("old", fixture.Repository.ExecuteScalar<string>(
            "SELECT display_name FROM update_rows WHERE row_id = @Id", parameters, connection: fixture.Connection));
        Assert.AreEqual(1, fixture.Repository.Execute(
            "UPDATE update_rows SET display_name = @Name WHERE row_id = @Id", parameters, connection: fixture.Connection));
        Assert.AreEqual("changed", fixture.Name(7));
        Assert.AreEqual("keep", fixture.Name(8));
        Assert.AreEqual(2, parameters.Count);
    }

    [TestMethod]
    public void Update_WithoutExplicitConditionUsesMappedPrimaryKey()
    {
        using var fixture = new Fixture();
        var command = UpdateableSqlBuilder<Row>.Create(DatabaseType.SQLite)
            .UpdateFields(row => row.Name)
            .SetParameter(new Row { Id = 7, Name = "changed" })
            .Build();
        command.Connection = fixture.Connection;
        Assert.AreEqual(1, fixture.Repository.Execute(command));
        Assert.AreEqual("changed", fixture.Name(7));
        Assert.AreEqual("keep", fixture.Name(8));
        Assert.AreEqual(2L, fixture.Repository.ExecuteScalar<long>("SELECT COUNT(*) FROM update_rows", connection: fixture.Connection));
    }

    [TestMethod]
    public void Update_WithoutKeyOrConditionRequiresExplicitFullTablePermission()
    {
        using var fixture = new Fixture();
        var builder = UpdateableSqlBuilder<KeylessRow>.Create(DatabaseType.SQLite)
            .SetParameter(new { Name = "changed" });
        Assert.Throws<ArgumentException>(() => builder.Build());
        Assert.AreEqual("old", fixture.Name(7));
        Assert.AreEqual("keep", fixture.Name(8));
        var command = builder.AllowEmptyWhereClause().Build();
        command.Connection = fixture.Connection;
        Assert.AreEqual(2, fixture.Repository.Execute(command));
        Assert.AreEqual("changed", fixture.Name(7));
        Assert.AreEqual("changed", fixture.Name(8));
    }

    private sealed class Fixture : IDisposable
    {
        public SQLiteConnection Connection { get; } = new("Data Source=:memory:;Pooling=False;");
        public Repository Repository { get; } = new();
        public Fixture()
        {
            try
            {
                Connection.Open();
                Repository.Execute("CREATE TABLE update_rows (row_id INTEGER PRIMARY KEY, display_name TEXT, Amount INTEGER);"
                    + "INSERT INTO update_rows VALUES (7, 'old', 20), (8, 'keep', 30);", connection: Connection);
            }
            catch
            {
                Connection.Dispose();
                throw;
            }
        }
        public string Name(int id) => Repository.ExecuteScalar<string>(
            "SELECT display_name FROM update_rows WHERE row_id = @Id", new { Id = id }, connection: Connection);
        public void Dispose() => Connection.Dispose();
    }

    [Table("update_rows")]
    private sealed class Row
    {
        [Key, Column("row_id")]
        public int Id { get; set; }
        [Column("display_name")]
        public string Name { get; set; }
        public int Amount { get; set; }
    }
    [Table("update_rows")]
    private sealed class KeylessRow
    {
        [Column("display_name")]
        public string Name { get; set; }
    }
    private sealed class Repository() : BaseRepository("Data Source=:memory:;Pooling=False;", SQLiteFactory.Instance);
}
