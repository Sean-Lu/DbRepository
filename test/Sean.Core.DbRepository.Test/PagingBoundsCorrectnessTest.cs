using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SQLite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sean.Core.DbRepository.Test;

/// <summary>
/// 仅验证分页边界计算及生成 SQL；除 SQLite 外不代表真实数据库兼容性验证。
/// </summary>
[TestClass]
public class PagingBoundsCorrectnessTest
{
    [TestMethod]
    [DataRow(int.MaxValue, int.MaxValue)]
    [DataRow(int.MinValue, 1)]
    public void Page_OverflowIsRejectedBeforeGeneratingSql(int pageNumber, int pageSize)
    {
        // 分别保护乘法溢出及 pageNumber - 1 的减法溢出，不扩大分页支持范围。
        Assert.Throws<OverflowException>(() => SqlFactory.CreateQueryableBuilder<PageEntity>(DatabaseType.SQLite).Page(pageNumber, pageSize).Build());
    }

    [TestMethod]
    [DataRow(DatabaseType.QuestDB)]
    [DataRow(DatabaseType.Oracle)]
    [DataRow(DatabaseType.DB2)]
    public void Offset_UpperBoundOverflowIsRejectedAndIntBoundaryStillWorks(DatabaseType databaseType)
    {
        Assert.Throws<OverflowException>(() => SqlFactory.CreateQueryableBuilder<PageEntity>(databaseType).Offset(int.MaxValue, 1).Build());
        Assert.Throws<OverflowException>(() => SqlFactory.CreateQueryableBuilder<PageEntity>(databaseType).Offset(int.MinValue, -1).Build());
        var sql = SqlFactory.CreateQueryableBuilder<PageEntity>(databaseType).Offset(int.MaxValue - 1, 1).Build().Sql;
        StringAssert.Contains(sql, "2147483647");
    }

    [TestMethod]
    public void Access_OffsetWithoutPrimaryKeyIsRejectedButFirstPageIsStillAllowed()
    {
        Assert.Throws<InvalidOperationException>(() => SqlFactory.CreateQueryableBuilder<NoKeyEntity>(DatabaseType.MsAccess).Offset(1, 10).Build());
        var sql = SqlFactory.CreateQueryableBuilder<NoKeyEntity>(DatabaseType.MsAccess).Offset(0, 10).Build().Sql;
        StringAssert.Contains(sql, "SELECT TOP 10");
    }

    [TestMethod]
    public void MissingBoundsAndZeroRowsPreserveExistingSqliteBehavior()
    {
        var plain = SqlFactory.CreateQueryableBuilder<PageEntity>(DatabaseType.SQLite).Build().Sql;
        Assert.AreEqual(plain, SqlFactory.CreateQueryableBuilder<PageEntity>(DatabaseType.SQLite).Page(null, 10).Build().Sql);
        Assert.AreEqual(plain, SqlFactory.CreateQueryableBuilder<PageEntity>(DatabaseType.SQLite).Offset(1, null).Build().Sql);
        StringAssert.Contains(SqlFactory.CreateQueryableBuilder<PageEntity>(DatabaseType.SQLite).Page(1, 0).Build().Sql, "LIMIT 0,0");
        StringAssert.Contains(SqlFactory.CreateQueryableBuilder<PageEntity>(DatabaseType.SQLite).Offset(0, 0).Build().Sql, "LIMIT 0,0");
    }

    [TestMethod]
    public void SQLite_ExecutesNormalAndLargeOffsetPages()
    {
        using var connection = new SQLiteConnection("Data Source=:memory:;Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE PagingBounds(Id INTEGER PRIMARY KEY); INSERT INTO PagingBounds VALUES (1),(2),(3)";
        command.ExecuteNonQuery();
        command.CommandText = SqlFactory.CreateQueryableBuilder<PageEntity>(DatabaseType.SQLite).OrderBy(OrderByType.Asc, entity => entity.Id).Page(2, 2).Build().Sql;
        Assert.AreEqual(3L, command.ExecuteScalar());
        command.CommandText = SqlFactory.CreateQueryableBuilder<PageEntity>(DatabaseType.SQLite).Page(int.MaxValue, 1).Build().Sql;
        Assert.IsNull(command.ExecuteScalar());
        // SQLite 的负 LIMIT 表示不限制条数，负 OFFSET 按零处理，不能统一当作非法参数拒绝。
        command.CommandText = SqlFactory.CreateQueryableBuilder<PageEntity>(DatabaseType.SQLite).OrderBy(OrderByType.Asc, entity => entity.Id).Offset(-1, -1).Build().Sql;
        using var reader = command.ExecuteReader();
        for (var expected = 1; expected <= 3; expected++)
        {
            Assert.IsTrue(reader.Read());
            Assert.AreEqual(expected, reader.GetInt32(0));
        }
        Assert.IsFalse(reader.Read());
    }

    [Table("PagingBounds")]
    private sealed class PageEntity
    {
        [Key]
        public int Id { get; set; }
    }

    private sealed class NoKeyEntity
    {
        public string Name { get; set; }
    }
}
