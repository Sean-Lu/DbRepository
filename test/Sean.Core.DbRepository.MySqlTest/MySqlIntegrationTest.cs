using System;
using System.Data.Common;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySql.Data.MySqlClient;
using Sean.Core.DbRepository.DbFirst;
using Sean.Core.DbRepository.Extensions;

[assembly: DoNotParallelize]

namespace Sean.Core.DbRepository.MySqlTest;

/// <summary>
/// 显式启用的真实 MySQL 测试；每个用例只拥有自己成功创建的临时数据库。
/// </summary>
[TestClass]
public class MySqlIntegrationTest
{
    private string _adminConnectionString;
    private string _ownedDatabase;
    private DbFactory _factory;

    [TestInitialize]
    public void Initialize()
    {
        // 先检查开关：默认不读取连接配置，更不会探测本机示例配置或连接服务器。
        if (Environment.GetEnvironmentVariable("SEAN_MYSQL_TESTS") != "1")
            Assert.Inconclusive("MySQL 集成测试默认关闭；设置 SEAN_MYSQL_TESTS=1 才会执行。");

        var configured = Environment.GetEnvironmentVariable("SEAN_MYSQL_CONNECTION_STRING");
        Assert.IsFalse(string.IsNullOrWhiteSpace(configured),
            "已启用 MySQL 集成测试，但缺少 SEAN_MYSQL_CONNECTION_STRING。");

        MySqlConnectionStringBuilder builder;
        try
        {
            builder = new MySqlConnectionStringBuilder(configured);
        }
        catch (ArgumentException)
        {
            // 不附带解析异常，避免无效配置内容进入测试日志。
            Assert.Fail("SEAN_MYSQL_CONNECTION_STRING 格式无效。");
            return;
        }
        Assert.IsFalse(string.IsNullOrWhiteSpace(builder.UserID), "必须明确配置 MySQL User ID。");
        // 忽略外部配置中的数据库，禁止在已有数据库中建表；所有连接关闭连接池。
        builder.Database = "";
        builder.Pooling = false;
        builder.ConnectionTimeout = 10;
        builder.DefaultCommandTimeout = 30;
        _adminConnectionString = builder.ConnectionString;
        var database = $"sean_orm_g4_{Guid.NewGuid():N}";
        ExecuteAdmin($"CREATE DATABASE `{database}` CHARACTER SET utf8mb4");
        // 不使用 IF NOT EXISTS；仅 CREATE 成功后才记录清理所有权。
        _ownedDatabase = database;
        builder.Database = database;
        _factory = new DbFactory(builder.ConnectionString, MySqlClientFactory.Instance);
    }

    [TestCleanup]
    public void Cleanup()
    {
        // 测试中的 using 与 DbFactory 内部连接均已释放，只删除本次成功创建的精确名称。
        if (_ownedDatabase == null) return;
        ExecuteAdmin($"DROP DATABASE `{_ownedDatabase}`");
        _ownedDatabase = null;
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Transaction_PersistsOnlyCommittedChanges(bool commit)
    {
        _factory.ExecuteNonQuery("CREATE TABLE entries (Id INT PRIMARY KEY, Name VARCHAR(40) NOT NULL) ENGINE=InnoDB");
        using (var connection = _factory.OpenNewConnection())
        using (var transaction = connection.BeginTransaction())
        {
            Assert.AreEqual(1, _factory.ExecuteNonQuery(transaction,
                "INSERT INTO entries (Id, Name) VALUES (@id, @name)",
                new[] { new MySqlParameter("@id", 1), new MySqlParameter("@name", "提交或回滚") }));
            Assert.AreEqual(1L, Convert.ToInt64(_factory.ExecuteScalar(transaction, "SELECT COUNT(*) FROM entries")));
            if (commit) transaction.Commit();
            else transaction.Rollback();
        }
        // 新连接读取，避免只验证当前事务内部的可见性。
        Assert.AreEqual(commit ? 1L : 0L, Convert.ToInt64(_factory.ExecuteScalar("SELECT COUNT(*) FROM entries")));
    }

    [TestMethod]
    public void UniqueConstraint_RollbackRemovesEarlierInsert()
    {
        _factory.ExecuteNonQuery("CREATE TABLE entries (Id INT PRIMARY KEY, Code VARCHAR(40) NOT NULL UNIQUE) ENGINE=InnoDB");
        _factory.ExecuteNonQuery("INSERT INTO entries VALUES (1, 'existing')");
        using (var connection = _factory.OpenNewConnection())
        using (var transaction = connection.BeginTransaction())
        {
            Assert.AreEqual(1, _factory.ExecuteNonQuery(transaction, "INSERT INTO entries VALUES (2, 'new')"));
            var error = Assert.Throws<MySqlException>(() =>
                _factory.ExecuteNonQuery(transaction, "INSERT INTO entries VALUES (3, 'existing')"));
            Assert.AreEqual(1062, error.Number, "必须是真实的唯一约束冲突，而非任意 SQL 异常。");
            transaction.Rollback();
        }
        Assert.AreEqual(1L, Convert.ToInt64(_factory.ExecuteScalar("SELECT COUNT(*) FROM entries")));
        Assert.AreEqual("existing", Convert.ToString(_factory.ExecuteScalar("SELECT Code FROM entries WHERE Id = 1")));
    }

    [TestMethod]
    public void DbFirst_ReadsFieldsAndForeignKeyMetadata()
    {
        _factory.ExecuteNonQuery("CREATE TABLE parent (Id INT PRIMARY KEY) ENGINE=InnoDB");
        _factory.ExecuteNonQuery(@"CREATE TABLE child (
            Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
            ParentId INT NULL,
            Name VARCHAR(40) NOT NULL DEFAULT 'guest',
            Amount DECIMAL(12,2) NOT NULL DEFAULT 0,
            CONSTRAINT fk_child_parent FOREIGN KEY (ParentId) REFERENCES parent(Id)
        ) ENGINE=InnoDB COMMENT='metadata sample'");
        var generator = new CodeGeneratorForMySql();
        generator.Initialize(_factory);

        var table = generator.GetTableInfo("child");
        Assert.IsNotNull(table);
        Assert.AreEqual(_ownedDatabase, table.TableSchema);
        Assert.AreEqual("child", table.TableName);
        Assert.AreEqual("metadata sample", table.TableComment);
        var fields = generator.GetTableFieldInfo("child");
        CollectionAssert.AreEqual(new[] { "Id", "ParentId", "Name", "Amount" }, fields.Select(f => f.FieldName).ToArray());
        Assert.IsTrue(fields.All(f => f.TableSchema == _ownedDatabase && f.TableName == "child"));
        var id = fields.Single(f => f.FieldName == "Id");
        Assert.AreEqual("int", id.FieldType);
        Assert.AreEqual(true, id.IsPrimaryKey);
        Assert.AreEqual(true, id.IsAutoIncrement);
        Assert.AreEqual(false, id.IsNullable);
        var parent = fields.Single(f => f.FieldName == "ParentId");
        Assert.AreEqual(true, parent.IsNullable);
        Assert.AreEqual(true, parent.IsForeignKey);
        Assert.AreEqual(false, parent.IsPrimaryKey);
        var name = fields.Single(f => f.FieldName == "Name");
        Assert.AreEqual("varchar", name.FieldType);
        Assert.AreEqual(40, name.StringMaxLength);
        Assert.AreEqual(false, name.IsNullable);
        Assert.AreEqual("guest", name.FieldDefault);
        var amount = fields.Single(f => f.FieldName == "Amount");
        Assert.AreEqual("decimal", amount.FieldType);
        Assert.AreEqual(12, amount.NumericPrecision);
        Assert.AreEqual(2, amount.NumericScale);

        var keys = generator.GetTableFieldReferenceInfo("child");
        Assert.AreEqual(1, keys.Count);
        Assert.AreEqual("fk_child_parent", keys[0].ForeignKeyName);
        Assert.AreEqual(_ownedDatabase, keys[0].TableSchema);
        Assert.AreEqual("child", keys[0].TableName);
        Assert.AreEqual("ParentId", keys[0].FieldName);
        Assert.AreEqual(_ownedDatabase, keys[0].ReferencedTableSchema);
        Assert.AreEqual("parent", keys[0].ReferencedTableName);
        Assert.AreEqual("Id", keys[0].ReferencedFieldName);
        Assert.AreEqual(0, generator.GetTableFieldInfo("missing").Count);
        Assert.AreEqual(0, generator.GetTableFieldReferenceInfo("missing").Count);
    }

    [TestMethod]
    [DataRow("table")]
    [DataRow("fields")]
    [DataRow("references")]
    public void DbFirst_TreatsQuotedTableNamesAsValues(string query)
    {
        const string tableName = "owner's child";
        _factory.ExecuteNonQuery("CREATE TABLE parent (Id INT PRIMARY KEY) ENGINE=InnoDB");
        // 建表处使用标识符引号，独立验证元数据查询中的字符串值处理。
        _factory.ExecuteNonQuery($"CREATE TABLE `{tableName}` (Id INT PRIMARY KEY, ParentId INT, FOREIGN KEY (ParentId) REFERENCES parent(Id)) ENGINE=InnoDB");
        var generator = new CodeGeneratorForMySql();
        generator.Initialize(_factory);
        const string missingName = "missing' OR '1'='1";
        switch (query)
        {
            case "table":
                Assert.AreEqual(tableName, generator.GetTableInfo(tableName)?.TableName);
                Assert.IsNull(generator.GetTableInfo(missingName));
                Assert.IsNull(generator.GetTableInfo(null));
                break;
            case "fields":
                var fields = generator.GetTableFieldInfo(tableName);
                CollectionAssert.AreEqual(new[] { "Id", "ParentId" }, fields.Select(f => f.FieldName).ToArray());
                Assert.IsTrue(fields.All(f => f.TableName == tableName));
                Assert.AreEqual(0, generator.GetTableFieldInfo(missingName).Count);
                Assert.AreEqual(0, generator.GetTableFieldInfo(null).Count);
                break;
            case "references":
                var keys = generator.GetTableFieldReferenceInfo(tableName);
                Assert.AreEqual(1, keys.Count);
                Assert.AreEqual(tableName, keys[0].TableName);
                Assert.AreEqual("ParentId", keys[0].FieldName);
                Assert.AreEqual("parent", keys[0].ReferencedTableName);
                Assert.AreEqual(0, generator.GetTableFieldReferenceInfo(missingName).Count);
                Assert.AreEqual(0, generator.GetTableFieldReferenceInfo(null).Count);
                break;
        }
    }

    [TestMethod]
    public void DbFirst_DoesNotApplyBusinessStringHandlerToNames()
    {
        _factory.ExecuteNonQuery("CREATE TABLE sample (Id INT PRIMARY KEY) ENGINE=InnoDB");
        var generator = new CodeGeneratorForMySql();
        generator.Initialize(_factory);
        var options = DbContextConfiguration.Options;
        var previous = options.GetTypeHandler(typeof(string));
        try
        {
            // 内部元数据名称不是业务数据，不应进入调用方的字符串转换流程。
            options.AddTypeHandler(typeof(string), new RejectStringHandler());
            Assert.AreEqual("sample", generator.GetTableInfo("sample")?.TableName);
            Assert.AreEqual(1, generator.GetTableFieldInfo("sample").Count);
            Assert.AreEqual(0, generator.GetTableFieldReferenceInfo("sample").Count);
        }
        finally
        {
            if (previous == null) options.RemoveTypeHandler(typeof(string));
            else options.AddTypeHandler(typeof(string), previous);
        }
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public void SchemaLookup_PreservesNamesInBothEscapeModes(bool field, bool noBackslashEscapes)
    {
        using var connection = _factory.OpenNewConnection();
        // 只改变本用例连接的会话设置，不修改服务器全局配置。
        _factory.ExecuteNonQuery(connection, noBackslashEscapes
            ? "SET SESSION sql_mode='NO_BACKSLASH_ESCAPES'" : "SET SESSION sql_mode=''");
        const string tableName = "owner's sample";
        const string fieldName = "value\\'s name";
        _factory.ExecuteNonQuery(connection, $"CREATE TABLE `{tableName}` (`{fieldName}` INT) ENGINE=InnoDB");
        long Count(string table, string column) => Convert.ToInt64(_factory.ExecuteScalar(connection, field
            ? DatabaseType.MySql.GetSqlForTableFieldExists(connection, table, column)
            : DatabaseType.MySql.GetSqlForTableExists(connection, table)));
        Assert.AreEqual(1L, Count(tableName, fieldName));
        Assert.AreEqual(0L, Count("missing' OR 1=1 -- ", fieldName));
        Assert.AreEqual(0L, Count("missing\\' OR 1=1 -- ", fieldName));
        if (field)
        {
            Assert.AreEqual(0L, Count(tableName, "missing' OR 1=1 -- "));
            Assert.AreEqual(0L, Count(tableName, "missing\\' OR 1=1 -- "));
        }
    }

    [TestMethod]
    [DataRow("IndexedValue", false)]
    [DataRow("PrimaryReference", true)]
    [DataRow("UniqueReference", true)]
    public void DbFirst_IdentifiesForeignKeysByConstraintsNotIndexKind(string fieldName, bool expected)
    {
        _factory.ExecuteNonQuery("CREATE TABLE parent (Id INT PRIMARY KEY) ENGINE=InnoDB");
        _factory.ExecuteNonQuery(@"CREATE TABLE child (
            PrimaryReference INT PRIMARY KEY,
            UniqueReference INT UNIQUE,
            IndexedValue INT,
            INDEX ix_value (IndexedValue),
            FOREIGN KEY (PrimaryReference) REFERENCES parent(Id),
            FOREIGN KEY (UniqueReference) REFERENCES parent(Id)
        ) ENGINE=InnoDB");
        var generator = new CodeGeneratorForMySql();
        generator.Initialize(_factory);
        // 索引类型不能代替外键约束：普通索引不是外键，主键/唯一键也可以同时是外键。
        var fields = generator.GetTableFieldInfo("child");
        Assert.AreEqual(3, fields.Count);
        Assert.AreEqual(expected, fields.Single(f => f.FieldName == fieldName).IsForeignKey);
    }

    [TestMethod]
    public void DbFirst_DoesNotTreatUniqueNotNullIndexAsPrimaryKey()
    {
        _factory.ExecuteNonQuery("CREATE TABLE sample (Code INT NOT NULL UNIQUE, Value INT) ENGINE=InnoDB");
        var generator = new CodeGeneratorForMySql();
        generator.Initialize(_factory);
        // 存储引擎可选择唯一索引组织数据，但它不等于用户声明的 PRIMARY KEY 约束。
        var fields = generator.GetTableFieldInfo("sample");
        Assert.AreEqual(2, fields.Count);
        Assert.AreEqual(false, fields.Single(f => f.FieldName == "Code").IsPrimaryKey);
    }

    [TestMethod]
    public void DbFirst_IdentifiesEveryCompositePrimaryKeyColumn()
    {
        _factory.ExecuteNonQuery(@"CREATE TABLE sample (
            TenantId INT, Id INT, Code INT NOT NULL UNIQUE,
            PRIMARY KEY (TenantId, Id)
        ) ENGINE=InnoDB");
        var generator = new CodeGeneratorForMySql();
        generator.Initialize(_factory);
        var fields = generator.GetTableFieldInfo("sample");
        CollectionAssert.AreEqual(new[] { "TenantId", "Id" },
            fields.Where(f => f.IsPrimaryKey == true).Select(f => f.FieldName).ToArray());
        Assert.AreEqual(false, fields.Single(f => f.FieldName == "Code").IsPrimaryKey);
    }

    private sealed class RejectStringHandler : ITypeHandler
    {
        public void Set(DbParameter parameter, object value, DatabaseType databaseType)
            => throw new InvalidOperationException("元数据查询不应调用业务字符串处理器。");
    }

    private void ExecuteAdmin(string sql)
    {
        try
        {
            using var connection = new MySqlConnection(_adminConnectionString);
            connection.Open();
            using var command = new MySqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }
        catch (MySqlException error)
        {
            // 配置和连接失败必须失败，不得转成跳过；只输出错误编号和安全的临时库名称。
            Assert.Fail($"MySQL 临时库操作失败（错误 {error.Number}）：{sql}");
        }
    }
}
