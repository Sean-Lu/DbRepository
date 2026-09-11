using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sean.Core.DbRepository.Test;

[TestClass]
public class TableInfoCacheIsolationTest
{
    private readonly string _database = "Data Source=cache_" + Guid.NewGuid().ToString("N");
    private string OtherDatabase => _database + "_True_beta";
    private const string Table = "beta_True_gamma";
    private const string OtherTable = "gamma";

    [TestCleanup]
    public void Cleanup()
    {
        // 只清理本用例的键，不清空其他测试或调用方的缓存。
        TableInfoCache.RemoveTable(_database, true, Table);
        TableInfoCache.RemoveTable(OtherDatabase, true, OtherTable);
    }

    [TestMethod]
    public void Lookup_DoesNotConfuseConnectionAndTableNameBoundaries()
    {
        TableInfoCache.AddTable(_database, true, Table);
        Assert.IsTrue(TableInfoCache.IsTableExists(_database, true, Table));
        Assert.IsFalse(TableInfoCache.IsTableExists(OtherDatabase, true, OtherTable));

        TableInfoCache.AddTableField(_database, true, Table, "Id");
        Assert.IsTrue(TableInfoCache.IsTableFieldExists(_database, true, Table, "Id"));
        Assert.IsFalse(TableInfoCache.IsTableFieldExists(OtherDatabase, true, OtherTable, "Id"));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Removal_OnlyInvalidatesTheRequestedDatabaseAndTable(bool removeTable)
    {
        TableInfoCache.AddTableField(_database, true, Table, "Id");
        TableInfoCache.AddTableField(OtherDatabase, true, OtherTable, "Id");
        if (removeTable)
            TableInfoCache.RemoveTable(OtherDatabase, true, OtherTable);
        else
            TableInfoCache.RemoveTableField(OtherDatabase, true, OtherTable, "Id");

        Assert.IsTrue(TableInfoCache.IsTableExists(_database, true, Table));
        Assert.IsTrue(TableInfoCache.IsTableFieldExists(_database, true, Table, "Id"));
        Assert.AreEqual(!removeTable, TableInfoCache.IsTableExists(OtherDatabase, true, OtherTable));
        Assert.IsFalse(TableInfoCache.IsTableFieldExists(OtherDatabase, true, OtherTable, "Id"));
    }
}
