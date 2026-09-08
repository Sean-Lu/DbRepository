using System;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sean.Core.DbRepository.DbFirst;

namespace Sean.Core.DbRepository.Test;

[TestClass]
public class DbFirstSqliteMetadataTest
{
    [TestMethod]
    [DataRow("sample", false)]
    [DataRow("sample", true)]
    [DataRow("owner's sample", false)]
    [DataRow("owner's sample", true)]
    public void Metadata_ReadsActualColumnsAndForeignKeys(string tableName, bool references)
    {
        var path = Path.Combine(Path.GetTempPath(), $"Sean.DbFirst.{Guid.NewGuid():N}.db");
        try
        {
            var factory = new DbFactory(new MultiConnectionSettings(new ConnectionStringOptions(
                $"Data Source={path};Pooling=False;", SQLiteFactory.Instance)));
            // 表名使用标识符引号建表，与生成器内部的字符串字面量查询相互独立。
            factory.ExecuteNonQuery("CREATE TABLE parent (Id INTEGER PRIMARY KEY)");
            factory.ExecuteNonQuery($"CREATE TABLE \"{tableName}\" (Id INTEGER NOT NULL PRIMARY KEY, ParentId INTEGER REFERENCES parent(Id), Name TEXT NOT NULL DEFAULT 'guest')");
            var generator = new CodeGeneratorForSQLite();
            generator.Initialize(factory);
            if (references)
            {
                var keys = generator.GetTableFieldReferenceInfo(tableName);
                Assert.AreEqual(1, keys.Count);
                Assert.AreEqual(tableName, keys[0].TableName);
                Assert.AreEqual("ParentId", keys[0].FieldName);
                Assert.AreEqual("parent", keys[0].ReferencedTableName);
                Assert.AreEqual("Id", keys[0].ReferencedFieldName);
                Assert.AreEqual(0, generator.GetTableFieldReferenceInfo("missing").Count);
            }
            else
            {
                var fields = generator.GetTableFieldInfo(tableName);
                Assert.AreEqual(3, fields.Count);
                Assert.IsTrue(fields.All(field => field.TableName == tableName));
                var id = fields.Single(field => field.FieldName == "Id");
                Assert.AreEqual("INTEGER", id.FieldType);
                Assert.AreEqual(true, id.IsPrimaryKey);
                Assert.AreEqual(false, id.IsNullable);
                Assert.IsNull(id.FieldDefault);
                var parent = fields.Single(field => field.FieldName == "ParentId");
                Assert.AreEqual(true, parent.IsNullable);
                Assert.AreEqual(false, parent.IsPrimaryKey);
                var name = fields.Single(field => field.FieldName == "Name");
                Assert.AreEqual("TEXT", name.FieldType);
                Assert.AreEqual(false, name.IsNullable);
                Assert.AreEqual("'guest'", name.FieldDefault);
                Assert.AreEqual(0, generator.GetTableFieldInfo("missing").Count);
            }
        }
        finally
        {
            // 仅删除本用例创建的文件，连接池已关闭。
            File.Delete(path);
        }
    }
}
