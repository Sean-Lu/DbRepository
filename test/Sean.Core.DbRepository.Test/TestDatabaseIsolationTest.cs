using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sean.Core.DbRepository.Test;

[TestClass]
public class TestDatabaseIsolationTest
{
    [TestMethod]
    public void IndependentlyCreatedRepositoriesDoNotShareDatabaseFiles()
    {
        var first = TestInfrastructure.CreateConnectionOptions();
        var second = TestInfrastructure.CreateConnectionOptions();
        Assert.AreNotEqual(first.ConnectionString, second.ConnectionString, "独立测试仓储不能共用 test.db。");
    }

    [TestMethod]
    public void WritesAndCleanupAreIsolatedBetweenRepositories()
    {
        using var first = TestInfrastructure.CreateRepository();
        using var second = TestInfrastructure.CreateRepository();
        var firstPath = first.DatabasePath;
        var secondPath = second.DatabasePath;
        first.Execute(new DefaultSqlCommand("CREATE TABLE IsolationProbe(Id INTEGER); INSERT INTO IsolationProbe VALUES(1)"));
        second.Execute(new DefaultSqlCommand("CREATE TABLE IsolationProbe(Id INTEGER); INSERT INTO IsolationProbe VALUES(2)"));
        Assert.AreEqual(1, first.ExecuteScalar<int>(new DefaultSqlCommand("SELECT Id FROM IsolationProbe")));
        Assert.AreEqual(2, second.ExecuteScalar<int>(new DefaultSqlCommand("SELECT Id FROM IsolationProbe")));

        first.Dispose();
        Assert.IsFalse(File.Exists(firstPath));
        Assert.IsFalse(File.Exists(firstPath + "-wal"));
        Assert.IsFalse(File.Exists(firstPath + "-shm"));
        Assert.IsTrue(File.Exists(secondPath));
        Assert.AreEqual(2, second.ExecuteScalar<int>(new DefaultSqlCommand("SELECT Id FROM IsolationProbe")), "清理一个测试库不能关闭或删除另一个测试库。");
        second.Dispose();
        Assert.IsFalse(File.Exists(secondPath));
    }
}
