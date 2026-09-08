using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SQLite;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sean.Core.DbRepository.CodeFirst;

namespace Sean.Core.DbRepository.Test;

[TestClass]
public class CodeFirstSqliteUpgradeTest
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Upgrade_CreatesOrAddsFieldsWithoutLosingDataAndIsRepeatable(bool existingTable)
    {
        var path = Path.Combine(Path.GetTempPath(), $"Sean.Upgrade.{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={path};Pooling=False;";
        const string tableName = "deployed_sample";
        try
        {
            var factory = new DbFactory(new MultiConnectionSettings(new ConnectionStringOptions(
                connectionString, SQLiteFactory.Instance)));
            if (existingTable)
            {
                factory.ExecuteNonQuery("CREATE TABLE deployed_sample (Id INTEGER NOT NULL PRIMARY KEY, Legacy TEXT)");
                factory.ExecuteNonQuery("INSERT INTO deployed_sample (Id, Legacy) VALUES (7, 'keep')");
            }
            var generator = new SqlGeneratorForSQLite();
            generator.Initialize(factory);
            // 表名回调模拟部署环境的实际表名，不能误操作实体默认表名。
            var statements = generator.GetUpgradeSql<UpgradedEntity>(_ => tableName);
            Assert.IsTrue(statements.Count > 0);
            foreach (var sql in statements) factory.ExecuteNonQuery(sql);

            if (existingTable)
            {
                // 实体未定义的历史列不能被删掉，已有行应获得新增字段的默认值。
                Assert.AreEqual("keep", factory.ExecuteScalar<string>("SELECT Legacy FROM deployed_sample WHERE Id = 7"));
                Assert.AreEqual("O'Brien", factory.ExecuteScalar<string>("SELECT Name FROM deployed_sample WHERE Id = 7"));
            }
            factory.ExecuteNonQuery("INSERT INTO deployed_sample (Id) VALUES (8)");
            Assert.AreEqual("O'Brien", factory.ExecuteScalar<string>("SELECT Name FROM deployed_sample WHERE Id = 8"));
            Assert.AreEqual(existingTable ? 2L : 1L, factory.ExecuteScalar<long>("SELECT COUNT(*) FROM deployed_sample"));
            Assert.Throws<SQLiteException>(() => factory.ExecuteNonQuery("INSERT INTO deployed_sample (Id, Name) VALUES (9, NULL)"));
            Assert.Throws<SQLiteException>(() => factory.ExecuteNonQuery("INSERT INTO deployed_sample (Id) VALUES (8)"));
            Assert.AreEqual(0, generator.GetUpgradeSql<UpgradedEntity>(_ => tableName).Count);
            Assert.AreEqual(0L, factory.ExecuteScalar<long>("SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'entity_template'"));
        }
        finally
        {
            // 不清空全局缓存，只清理本用例独有连接对应的表记录和文件。
            TableInfoCache.RemoveTable(connectionString, true, tableName);
            File.Delete(path);
        }
    }

    [Table("entity_template")]
    private sealed class UpgradedEntity
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [DefaultValue("O'Brien")]
        public string Name { get; set; }
    }
}
