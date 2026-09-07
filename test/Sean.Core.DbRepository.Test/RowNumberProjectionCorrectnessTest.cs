using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SQLite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sean.Core.DbRepository.Test;

/// <summary>
/// ROW_NUMBER 派生表只能引用内层输出列。用 SQLite 验证通用语句的结果，不代替真实 SQL Server/DB2 验证。
/// </summary>
[TestClass]
[DoNotParallelize]
public class RowNumberProjectionCorrectnessTest
{
    [TestMethod]
    [DataRow(DatabaseType.SqlServer)]
    [DataRow(DatabaseType.DB2)]
    public void MappedAndJoinedColumnsReferenceDerivedOutputs(DatabaseType dbType)
    {
        var sql = BuildLegacy(dbType, builder => builder.SelectFields(row => new { row.Id, row.OwnerName })
            .OrderBy(OrderByType.Asc, row => row.Id).Page(2, 1));
        StringAssert.StartsWith(sql.Sql, "SELECT t2.Id, t2.OwnerName FROM (");
        using var connection = OpenDatabase();
        using var command = connection.CreateCommand();
        command.CommandText = sql.Sql;
        using var reader = command.ExecuteReader();
        Assert.AreEqual(2, reader.FieldCount, "分页不能把 ROW_NUM 辅助列暴露给调用方。");
        Assert.IsTrue(reader.Read());
        Assert.AreEqual("Id", reader.GetName(0));
        Assert.AreEqual(2, reader.GetInt32(0));
        Assert.AreEqual("OwnerName", reader.GetName(1));
        Assert.AreEqual("owner", reader.GetString(1));
        Assert.IsFalse(reader.Read());
    }

    [TestMethod]
    [DataRow(DatabaseType.SqlServer)]
    [DataRow(DatabaseType.DB2)]
    public void JoinedOnlyProjectionDoesNotRestoreMainFields(DatabaseType dbType)
    {
        var sql = BuildLegacy(dbType, builder => builder.SelectFields(row => row.OwnerName)
            .OrderBy(OrderByType.Asc, row => row.Id).Offset(0, 1));
        StringAssert.StartsWith(sql.Sql, "SELECT t2.OwnerName FROM (");
        using var connection = OpenDatabase();
        using var command = connection.CreateCommand();
        command.CommandText = sql.Sql;
        using var reader = command.ExecuteReader();
        Assert.AreEqual(1, reader.FieldCount);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual("owner", reader.GetString(0));
    }

    [TestMethod]
    [DataRow(DatabaseType.SqlServer)]
    [DataRow(DatabaseType.DB2)]
    public void AggregateAliasIsReadInsteadOfReevaluatingExpression(DatabaseType dbType)
    {
        var sql = BuildLegacy(dbType, builder => builder.SelectFields(row => row.OwnerId)
            .MaxField(row => row.Amount, "Peak").GroupBy(row => row.OwnerId)
            .OrderBy(OrderByType.Asc, row => row.OwnerId).Page(1, 1));
        using var connection = OpenDatabase();
        using var command = connection.CreateCommand();
        command.CommandText = sql.Sql;
        using var reader = command.ExecuteReader();
        Assert.AreEqual(2, reader.FieldCount);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(10, reader.GetInt32(0));
        Assert.AreEqual("Peak", reader.GetName(1));
        Assert.AreEqual(9, reader.GetInt32(1));
        Assert.IsFalse(reader.Read());
    }

    [TestMethod]
    public void ModernSqlServerPagingIsUnchanged()
    {
        var previous = DbContextConfiguration.SqlServerOptions.UseRowNumberForPaging;
        try
        {
            DbContextConfiguration.SqlServerOptions.UseRowNumberForPaging = false;
            var sql = SqlFactory.CreateQueryableBuilder<ProjectionRow>(DatabaseType.SqlServer)
                .SelectFields(row => row.Id).OrderBy(OrderByType.Asc, row => row.Id).Page(2, 1).Build().Sql;
            Assert.AreEqual("SELECT [DB_ID] AS Id FROM [ProjectionMain] ORDER BY [DB_ID] ASC OFFSET 1 ROWS FETCH NEXT 1 ROWS ONLY", sql);
        }
        finally
        {
            DbContextConfiguration.SqlServerOptions.UseRowNumberForPaging = previous;
        }
    }

    private static ISqlCommand BuildLegacy(DatabaseType dbType, Func<IQueryable<ProjectionRow>, IQueryable<ProjectionRow>> configure)
    {
        var previous = DbContextConfiguration.SqlServerOptions.UseRowNumberForPaging;
        try
        {
            DbContextConfiguration.SqlServerOptions.UseRowNumberForPaging = true;
            var builder = configure(SqlFactory.CreateQueryableBuilder<ProjectionRow>(dbType));
            var result = builder.Build();
            Assert.AreEqual(result.Sql, builder.Build().Sql, "重复 Build 不应改变输出列。");
            return result;
        }
        finally
        {
            DbContextConfiguration.SqlServerOptions.UseRowNumberForPaging = previous;
        }
    }

    private static SQLiteConnection OpenDatabase()
    {
        var connection = new SQLiteConnection("Data Source=:memory:;Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE ProjectionMain(DB_ID INTEGER PRIMARY KEY, OwnerId INTEGER, Amount INTEGER); CREATE TABLE ProjectionOwner(Id INTEGER PRIMARY KEY, Name TEXT); INSERT INTO ProjectionOwner VALUES(10,'owner'); INSERT INTO ProjectionMain VALUES(1,10,3),(2,10,9),(3,10,5)";
        command.ExecuteNonQuery();
        return connection;
    }

    [Table("ProjectionMain")]
    [LeftJoin(typeof(ProjectionOwner), nameof(OwnerId), nameof(ProjectionOwner.Id), "o")]
    private sealed class ProjectionRow
    {
        [Key, Column("DB_ID")]
        public int Id { get; set; }
        public int OwnerId { get; set; }
        public int Amount { get; set; }
        [NotMapped, LeftJoinField("o", nameof(ProjectionOwner.Name))]
        public string OwnerName { get; set; }
    }

    [Table("ProjectionOwner")]
    private sealed class ProjectionOwner
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
