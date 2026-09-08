using System;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sean.Core.DbRepository.Test;

[TestClass]
public class SchemaLookupCoverageTest
{
    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task Lookup_CachesOnlyPositiveResultsAndAllowsBypass(bool asynchronous, bool field)
    {
        using var fixture = new Fixture();
        async Task<bool> Lookup(bool cache = true) => field
            ? asynchronous ? await fixture.Repository.IsTableFieldExistsAsync("sample", "Value", useCache: cache)
                : fixture.Repository.IsTableFieldExists("sample", "Value", useCache: cache)
            : asynchronous ? await fixture.Repository.IsTableExistsAsync("sample", useCache: cache)
                : fixture.Repository.IsTableExists("sample", useCache: cache);

        fixture.Repository.Execute("CREATE TABLE sample (Id INTEGER)");
        if (!field) fixture.Repository.Execute("DROP TABLE sample");
        Assert.IsFalse(await Lookup());
        fixture.Repository.Execute(field ? "ALTER TABLE sample ADD Value INTEGER" : "CREATE TABLE sample (Value INTEGER)");
        // 不缓存不存在的结果，建表或新增字段后应立即可见。
        Assert.IsTrue(await Lookup(false));
        Assert.IsFalse(field
            ? TableInfoCache.IsTableFieldExists(fixture.Master, true, "sample", "Value")
            : TableInfoCache.IsTableExists(fixture.Master, true, "sample"));
        Assert.IsTrue(await Lookup());
        fixture.Repository.Execute("DROP TABLE sample");
        // 保留现有正向缓存语义；外部 DDL 后可显式绕过缓存获取数据库状态。
        Assert.IsTrue(await Lookup());
        Assert.IsFalse(await Lookup(false));
        TableInfoCache.RemoveTable(fixture.Master, true, "sample");
        Assert.IsFalse(await Lookup());
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Lookup_SeparatesMasterAndSecondaryAndRejectsEmptyNames(bool asynchronous)
    {
        using var fixture = new Fixture();
        fixture.Repository.Execute("CREATE TABLE sample (Value INTEGER)");
        async Task<bool> Table(string name, bool master) => asynchronous
            ? await fixture.Repository.IsTableExistsAsync(name, master) : fixture.Repository.IsTableExists(name, master);
        async Task<bool> Field(string table, string name, bool master) => asynchronous
            ? await fixture.Repository.IsTableFieldExistsAsync(table, name, master)
            : fixture.Repository.IsTableFieldExists(table, name, master);
        Assert.IsTrue(await Table("sample", true));
        Assert.IsTrue(await Field("sample", "Value", true));
        Assert.IsFalse(await Table("sample", false));
        Assert.IsFalse(await Field("sample", "Value", false));
        foreach (var name in new[] { null, "", " " })
        {
            Assert.IsFalse(await Table(name, true));
            Assert.IsFalse(await Field(name, "Value", true));
            Assert.IsFalse(await Field("sample", name, true));
        }
        fixture.Repository.Execute("CREATE TABLE sample (Value INTEGER)", master: false);
        Assert.IsTrue(await Table("sample", false));
        Assert.IsTrue(await Field("sample", "Value", false));
    }

    private sealed class Fixture : IDisposable
    {
        private readonly string _masterPath = Path.Combine(Path.GetTempPath(), $"Sean.Schema.Master.{Guid.NewGuid():N}.db");
        private readonly string _secondaryPath = Path.Combine(Path.GetTempPath(), $"Sean.Schema.Secondary.{Guid.NewGuid():N}.db");
        public string Master { get; }
        private string Secondary { get; }
        public Repository Repository { get; }
        public Fixture()
        {
            Master = $"Data Source={_masterPath};Pooling=False;";
            Secondary = $"Data Source={_secondaryPath};Pooling=False;";
            Repository = new Repository(new MultiConnectionSettings(new[]
            {
                new ConnectionStringOptions(Master, SQLiteFactory.Instance),
                new ConnectionStringOptions(Secondary, SQLiteFactory.Instance, master: false)
            }));
        }
        public void Dispose()
        {
            // 只清理本用例的缓存键与数据库文件，不影响并行用例。
            TableInfoCache.RemoveTable(Master, true, "sample");
            TableInfoCache.RemoveTable(Secondary, false, "sample");
            File.Delete(_masterPath);
            File.Delete(_secondaryPath);
        }
    }

    private sealed class Repository(MultiConnectionSettings settings) : BaseRepository(settings);
}
