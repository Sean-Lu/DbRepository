using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySql.Data.MySqlClient;
using Sean.Core.DbRepository.Dapper;
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

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public void Values_RoundTripWithBothParameterAndLiteralExecution(bool parameterized, bool noBackslashEscapes)
    {
        using var connection = _factory.OpenNewConnection();
        _factory.ExecuteNonQuery(connection, noBackslashEscapes
            ? "SET SESSION sql_mode='NO_BACKSLASH_ESCAPES'" : "SET SESSION sql_mode=''");
        _factory.ExecuteNonQuery(connection, @"CREATE TABLE sample (
            TextValue TEXT NOT NULL, UnsignedValue BIGINT UNSIGNED NOT NULL,
            Amount DECIMAL(20,6) NOT NULL, CreatedAt DATETIME NOT NULL
        ) ENGINE=InnoDB");
        var expected = new ValueRow
        {
            TextValue = "中文\\'O'Brien\0末尾\\",
            UnsignedValue = UnsignedNumber.Maximum,
            Amount = 123456789.123456m,
            CreatedAt = new DateTime(2026, 9, 10, 12, 34, 56)
        };
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var command = SqlFactory.CreateInsertableBuilder<ValueRow>(DatabaseType.MySql)
                .SetParameter(expected)
                .SetSqlParameterized(parameterized)
                .Build();
            command.Connection = connection;
            Assert.AreEqual(1, _factory.ExecuteNonQuery(command));
            // 从真实列读回，而非仅比较生成 SQL，检查驱动绑定、字符串编码及实体类型转换。
            var actual = _factory.Get<ValueRow>(connection, "SELECT TextValue, UnsignedValue, Amount, CreatedAt FROM sample");
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.TextValue, actual.TextValue);
            Assert.AreEqual(expected.UnsignedValue, actual.UnsignedValue);
            Assert.AreEqual(expected.Amount, actual.Amount);
            Assert.AreEqual(expected.CreatedAt, actual.CreatedAt);
        }
        finally { CultureInfo.CurrentCulture = previousCulture; }
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task Add_ReturnsLargeIdentityAndRespectsCallerTransaction(bool asynchronous, bool externalTransaction)
    {
        _factory.ExecuteNonQuery("CREATE TABLE sample (row_id BIGINT PRIMARY KEY AUTO_INCREMENT, display_name VARCHAR(40) NOT NULL) ENGINE=InnoDB AUTO_INCREMENT=2147483648");
        var repository = new SampleRepository(_factory.ConnectionSettings);
        using var connection = externalTransaction ? _factory.OpenNewConnection() : null;
        using var transaction = connection?.BeginTransaction();
        var entity = new SampleRow { Name = "O'Brien" };
        Assert.IsTrue(asynchronous
            ? await repository.AddAsync(entity, returnAutoIncrementId: true, transaction: transaction)
            : repository.Add(entity, returnAutoIncrementId: true, transaction: transaction));
        Assert.AreEqual(2147483648L, entity.Id);
        const string readName = "SELECT display_name FROM sample WHERE row_id=2147483648";
        Assert.AreEqual(entity.Name, Convert.ToString(externalTransaction
            ? _factory.ExecuteScalar(transaction, readName) : _factory.ExecuteScalar(readName)));
        if (externalTransaction)
        {
            // 回写成功不代表允许提前提交；调用方仍能回滚，连接也必须保持可用。
            Assert.AreSame(connection, transaction.Connection);
            transaction.Rollback();
            Assert.AreEqual(System.Data.ConnectionState.Open, connection.State);
        }
        Assert.AreEqual(externalTransaction ? 0L : 1L,
            Convert.ToInt64(_factory.ExecuteScalar("SELECT COUNT(*) FROM sample")));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task PageQuery_ReturnsFilteredTotalsAndMappedColumns(bool asynchronous)
    {
        _factory.ExecuteNonQuery("CREATE TABLE sample (row_id BIGINT PRIMARY KEY, display_name VARCHAR(40) NOT NULL) ENGINE=InnoDB");
        _factory.ExecuteNonQuery("INSERT INTO sample VALUES (5,'five'),(1,'one'),(4,'four'),(2,'skip'),(3,'three')");
        var repository = new SampleRepository(_factory.ConnectionSettings);
        var orderBy = OrderByCondition.Create<SampleRow>(OrderByType.Asc, row => row.Id);
        foreach (var pageNumber in new[] { 2, 3 })
        {
            var page = asynchronous
                ? await repository.PageQueryAsync(row => row.Name != "skip", orderBy, pageNumber, 2)
                : repository.PageQuery(row => row.Name != "skip", orderBy, pageNumber, 2);
            Assert.AreEqual(4, page.Total);
            Assert.AreEqual(pageNumber, page.PageNumber);
            Assert.AreEqual(2, page.PageSize);
            CollectionAssert.AreEqual(pageNumber == 2 ? new[] { 4L, 5L } : Array.Empty<long>(),
                page.List.Select(row => row.Id).ToArray());
            if (pageNumber == 2)
                CollectionAssert.AreEqual(new[] { "four", "five" }, page.List.Select(row => row.Name).ToArray());
        }
    }

    [Table("sample")]
    private sealed class SampleRow
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("row_id")]
        public long Id { get; set; }
        [Column("display_name")]
        public string Name { get; set; }
    }

    private sealed class SampleRepository(MultiConnectionSettings settings) : BaseRepository<SampleRow>(settings);

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task DapperReader_PreservesResultsAndConnectionOwnership(bool generic, bool asynchronous)
    {
        _factory.ExecuteNonQuery("CREATE TABLE sample (row_id INT PRIMARY KEY, display_name VARCHAR(40)) ENGINE=InnoDB");
        _factory.ExecuteNonQuery("INSERT INTO sample VALUES (1,'existing')");
        BaseRepository repository = generic
            ? new DapperSampleRepository(_factory.ConnectionSettings)
            : new DapperRepository(_factory.ConnectionSettings);
        // 分别验证内部连接、调用方提供的关闭连接，以及调用方事务。
        foreach (var ownership in new[] { "internal", "closed", "transaction" })
        {
            using var connection = ownership == "internal" ? null
                : _factory.CreateConnection();
            if (ownership == "transaction") connection.Open();
            using var transaction = ownership == "transaction" ? connection.BeginTransaction() : null;
            if (transaction != null)
                _factory.ExecuteNonQuery(transaction, "INSERT INTO sample VALUES (2,'uncommitted')");
            DbConnection actualConnection = null;
            Action<SqlExecutingContext> observe = context =>
            {
                actualConnection = (DbConnection)context.Connection;
            };
            repository.Factory.SqlMonitor.SqlExecuting += observe;
            try
            {
                var command = new DefaultSqlCommand(DatabaseType.MySql)
                {
                    Sql = "SELECT display_name FROM sample WHERE row_id=@Id; SELECT @Text AS TextValue",
                    Parameter = new { Id = transaction == null ? 1 : 2, Text = "中文 O'Brien" },
                    Connection = connection,
                    Transaction = transaction
                };
                using (var reader = asynchronous
                    ? await repository.ExecuteReaderAsync(command) : repository.ExecuteReader(command))
                {
                    Assert.IsNotNull(actualConnection);
                    Assert.AreEqual(System.Data.ConnectionState.Open, actualConnection.State);
                    Assert.IsTrue(reader.Read());
                    Assert.AreEqual(transaction == null ? "existing" : "uncommitted", reader.GetString(0));
                    Assert.IsFalse(reader.Read());
                    Assert.IsTrue(reader.NextResult());
                    Assert.IsTrue(reader.Read());
                    Assert.AreEqual("中文 O'Brien", reader.GetString(0));
                    Assert.IsFalse(reader.Read());
                    Assert.IsFalse(reader.NextResult());
                }
                if (ownership == "internal")
                    Assert.AreEqual(System.Data.ConnectionState.Closed, actualConnection.State);
                else
                {
                    Assert.AreSame(connection, actualConnection);
                    if (transaction != null)
                    {
                        Assert.AreEqual(System.Data.ConnectionState.Open, connection.State);
                        Assert.AreSame(connection, transaction.Connection);
                        transaction.Rollback();
                    }
                    else
                    {
                        Assert.AreEqual(System.Data.ConnectionState.Closed, connection.State);
                        connection.Open();
                    }
                    Assert.AreEqual(42L, Convert.ToInt64(_factory.ExecuteScalar(connection, "SELECT 42")));
                }
                Assert.AreEqual(1L, Convert.ToInt64(_factory.ExecuteScalar("SELECT COUNT(*) FROM sample")));
            }
            finally
            {
                repository.Factory.SqlMonitor.SqlExecuting -= observe;
                // 断言失败时兜底清理内部连接；所有权断言发生在此之前。
                if (ownership == "internal") actualConnection?.Dispose();
            }
        }
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task DapperProcedure_WritesOutputAfterExecutionAndReaderDisposal(bool generic, bool asynchronous)
    {
        _factory.ExecuteNonQuery("CREATE PROCEDURE sample_output(IN InputValue BIGINT, OUT Value BIGINT) " +
            "BEGIN SELECT InputValue AS ResultValue; SET Value = InputValue + 1; END");
        BaseRepository repository = generic
            ? new DapperSampleRepository(_factory.ConnectionSettings)
            : new DapperRepository(_factory.ConnectionSettings);
        foreach (var returnReader in new[] { false, true })
        {
            var parameters = new global::Dapper.DynamicParameters();
            parameters.Add("InputValue", 2147483648L, System.Data.DbType.Int64);
            parameters.Add("Value", dbType: System.Data.DbType.Int64,
                direction: System.Data.ParameterDirection.Output);
            var target = new ProcedureOutput { Value = -1 };
            var command = new DefaultSqlCommand("sample_output", parameters)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            if (returnReader)
            {
                // 输出值在结果集之后设置；未读完就释放 Reader 也必须能拿到最终值。
                using (var reader = asynchronous
                    ? await repository.ExecuteReaderAsync(command) : repository.ExecuteReader(command))
                {
                    Assert.IsTrue(reader.Read());
                    Assert.AreEqual(2147483648L, reader.GetInt64(0));
                }
                Assert.AreEqual(2147483649L, parameters.Get<long>("Value"));
            }
            else
            {
                command.OutputParameterOptions = new OutputParameterOptions<ProcedureOutput>
                {
                    OutputTarget = target,
                    OutputPropertyInfo = typeof(ProcedureOutput).GetProperty(nameof(ProcedureOutput.Value))
                };
                if (asynchronous) await repository.ExecuteAsync(command);
                else repository.Execute(command);
                Assert.AreEqual(2147483649L, target.Value);
            }
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task NativeProcedure_PreservesOutputParameters(bool asynchronous)
    {
        _factory.ExecuteNonQuery("CREATE PROCEDURE sample_output(IN InputValue BIGINT, OUT Value BIGINT) " +
            "BEGIN SELECT InputValue AS ResultValue; SET Value = InputValue + 1; END");
        foreach (var execution in new[] { "execute", "query", "reader" })
        {
            var output = new MySqlParameter("Value", MySqlDbType.Int64)
            {
                Direction = System.Data.ParameterDirection.Output
            };
            var target = new ProcedureOutput { Value = -1 };
            var command = new DefaultSqlCommand("sample_output", new DbParameter[]
            {
                new MySqlParameter("InputValue", MySqlDbType.Int64) { Value = 2147483648L }, output
            })
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            if (execution == "reader")
            {
                // 流式结果交给调用方释放后，再读取驱动参数上的输出值。
                using (var reader = asynchronous
                    ? await _factory.ExecuteReaderAsync(command) : _factory.ExecuteReader(command))
                {
                    Assert.IsTrue(reader.Read());
                    Assert.AreEqual(2147483648L, reader.GetInt64(0));
                }
            }
            else
            {
                command.OutputParameterOptions = new OutputParameterOptions<ProcedureOutput>
                {
                    OutputTarget = target,
                    OutputPropertyInfo = typeof(ProcedureOutput).GetProperty(nameof(ProcedureOutput.Value))
                };
                if (execution == "execute")
                {
                    if (asynchronous) await _factory.ExecuteNonQueryAsync(command);
                    else _factory.ExecuteNonQuery(command);
                }
                else
                {
                    var rows = asynchronous
                        ? await _factory.QueryAsync<long>(command) : _factory.Query<long>(command);
                    CollectionAssert.AreEqual(new[] { 2147483648L }, rows.ToArray());
                }
                Assert.AreEqual(2147483649L, target.Value, execution);
            }
            Assert.AreEqual(2147483649L, Convert.ToInt64(output.Value), execution);
        }
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task BatchInsert_PreservesCallerTransactionOnLaterBatchFailure(bool asynchronous, bool externalTransaction)
    {
        _factory.ExecuteNonQuery("CREATE TABLE sample (row_id BIGINT PRIMARY KEY AUTO_INCREMENT, " +
            "display_name VARCHAR(40) NOT NULL UNIQUE) ENGINE=InnoDB");
        var repository = new BatchSampleRepository(_factory.ConnectionSettings);
        using var connection = externalTransaction ? _factory.OpenNewConnection() : null;
        using var transaction = connection?.BeginTransaction();
        var rows = new[]
        {
            new SampleRow { Name = "first" }, new SampleRow { Name = "second" },
            new SampleRow { Name = "first" }
        };
        // 每批两行：第一批成功，第二批触发唯一键冲突，不依赖无效 SQL 模拟失败。
        var error = await Assert.ThrowsAsync<MySqlException>(async () =>
        {
            if (asynchronous) await repository.AddAsync(rows, transaction: transaction);
            else repository.Add(rows, transaction: transaction);
        });
        Assert.AreEqual(1062, error.Number);
        if (externalTransaction)
        {
            Assert.AreSame(connection, transaction.Connection);
            Assert.AreEqual(System.Data.ConnectionState.Open, connection.State);
            Assert.AreEqual(2L, Convert.ToInt64(_factory.ExecuteScalar(transaction, "SELECT COUNT(*) FROM sample")));
            transaction.Rollback();
        }
        // 保持既有语义：未提供事务允许部分成功；调用方事务可撤销此前的批次。
        var names = _factory.Query<string>("SELECT display_name FROM sample ORDER BY row_id");
        CollectionAssert.AreEqual(externalTransaction ? Array.Empty<string>() : new[] { "first", "second" }, names.ToArray());
    }

    private sealed class BatchSampleRepository : BaseRepository<SampleRow>
    {
        public BatchSampleRepository(MultiConnectionSettings settings) : base(settings) => BulkEntityCount = 2;
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void CodeFirst_UpgradePreservesExistingDataAndIsRepeatable(bool existingTable)
    {
        const string tableName = "deployed_sample";
        if (existingTable)
        {
            _factory.ExecuteNonQuery("CREATE TABLE deployed_sample (Id BIGINT PRIMARY KEY, Legacy VARCHAR(40)) ENGINE=InnoDB");
            _factory.ExecuteNonQuery("INSERT INTO deployed_sample VALUES (7,'keep')");
        }
        var generator = new Sean.Core.DbRepository.CodeFirst.SqlGeneratorForMySql();
        generator.Initialize(_factory);
        var statements = generator.GetUpgradeSql<UpgradeRow>(_ => tableName);
        Assert.IsTrue(statements.Count > 0);
        foreach (var sql in statements) _factory.ExecuteNonQuery(sql);
        if (existingTable)
        {
            // 新增字段不能删除旧行或实体未定义的历史列。
            Assert.AreEqual("keep", _factory.ExecuteScalar<string>("SELECT Legacy FROM deployed_sample WHERE Id=7"));
            Assert.AreEqual("O'Brien", _factory.ExecuteScalar<string>("SELECT Name FROM deployed_sample WHERE Id=7"));
        }
        _factory.ExecuteNonQuery("INSERT INTO deployed_sample (Id) VALUES (8)");
        Assert.AreEqual("O'Brien", _factory.ExecuteScalar<string>("SELECT Name FROM deployed_sample WHERE Id=8"));
        Assert.AreEqual(existingTable ? 2L : 1L, _factory.ExecuteScalar<long>("SELECT COUNT(*) FROM deployed_sample"));
        Assert.AreEqual(1048, Assert.Throws<MySqlException>(() =>
            _factory.ExecuteNonQuery("INSERT INTO deployed_sample (Id,Name) VALUES (9,NULL)")).Number);
        Assert.AreEqual(1062, Assert.Throws<MySqlException>(() =>
            _factory.ExecuteNonQuery("INSERT INTO deployed_sample (Id) VALUES (8)")).Number);
        Assert.AreEqual(0, generator.GetUpgradeSql<UpgradeRow>(_ => tableName).Count);
        Assert.AreEqual(0L, _factory.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='entity_template'"));
    }

    [TestMethod]
    public void CodeFirst_UpgradeDoesNotAddExistingColumnWithDifferentCase()
    {
        _factory.ExecuteNonQuery("CREATE TABLE deployed_sample (`id` BIGINT PRIMARY KEY, Legacy VARCHAR(40)) ENGINE=InnoDB");
        _factory.ExecuteNonQuery("INSERT INTO deployed_sample VALUES (7,'keep')");
        var generator = new Sean.Core.DbRepository.CodeFirst.SqlGeneratorForMySql();
        generator.Initialize(_factory);
        // id 已存在而 Name 确实缺失；既不能重复添加 id，也不能漏掉真正的新字段。
        foreach (var sql in generator.GetUpgradeSql<UpgradeRow>(_ => "deployed_sample"))
            _factory.ExecuteNonQuery(sql);
        Assert.AreEqual("O'Brien", _factory.ExecuteScalar<string>("SELECT Name FROM deployed_sample WHERE Id=7"));
        Assert.AreEqual("keep", _factory.ExecuteScalar<string>("SELECT Legacy FROM deployed_sample WHERE Id=7"));
        Assert.AreEqual(0, generator.GetUpgradeSql<UpgradeRow>(_ => "deployed_sample").Count);
    }

    [TestMethod]
    [DataRow("native", false)]
    [DataRow("native", true)]
    [DataRow("dapper", false)]
    [DataRow("dapper", true)]
    [DataRow("dapper-generic", false)]
    [DataRow("dapper-generic", true)]
    public async Task DataSet_PreservesEmptyResultsDuplicateColumnsAndBinaryValues(string executor, bool asynchronous)
    {
        _factory.ExecuteNonQuery("CREATE TABLE sample (Id INT PRIMARY KEY, Payload VARBINARY(8), OptionalValue VARCHAR(20))");
        _factory.ExecuteNonQuery("INSERT INTO sample VALUES (7, X'0001FF', NULL)");
        BaseRepository repository = executor == "native" ? new SampleRepository(_factory.ConnectionSettings)
            : executor == "dapper" ? new DapperRepository(_factory.ConnectionSettings)
            : new DapperSampleRepository(_factory.ConnectionSettings);
        var command = new DefaultSqlCommand(
            "SELECT Id FROM sample WHERE Id=@Missing; " +
            "SELECT Id AS Value, Id+1 AS Value, Payload, OptionalValue FROM sample WHERE Id=@Id",
            new { Missing = -1, Id = 7 });
        using var dataSet = asynchronous
            ? await repository.ExecuteDataSetAsync(command) : repository.ExecuteDataSet(command);
        Assert.AreEqual(2, dataSet.Tables.Count);
        // 空结果也必须保留列结构，不能导致后面的结果集错位。
        Assert.AreEqual(0, dataSet.Tables[0].Rows.Count);
        Assert.AreEqual("Id", dataSet.Tables[0].Columns[0].ColumnName);
        var table = dataSet.Tables[1];
        Assert.AreEqual(1, table.Rows.Count);
        Assert.AreEqual(4, table.Columns.Count);
        Assert.AreEqual("Value", table.Columns[0].ColumnName);
        Assert.AreEqual("Value1", table.Columns[1].ColumnName);
        Assert.AreEqual(7L, Convert.ToInt64(table.Rows[0][0]));
        Assert.AreEqual(8L, Convert.ToInt64(table.Rows[0][1]));
        CollectionAssert.AreEqual(new byte[] { 0, 1, 255 }, (byte[])table.Rows[0]["Payload"]);
        Assert.IsTrue(table.Rows[0].IsNull("OptionalValue"));
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task UpdateAndDelete_RespectConditionsAndCallerRollback(bool dapper, bool asynchronous)
    {
        _factory.ExecuteNonQuery("CREATE TABLE sample (row_id BIGINT PRIMARY KEY, display_name VARCHAR(40)) ENGINE=InnoDB");
        _factory.ExecuteNonQuery("INSERT INTO sample VALUES (1,'old'),(2,'keep')");
        BaseRepository<SampleRow> repository = dapper
            ? new DapperSampleRepository(_factory.ConnectionSettings) : new SampleRepository(_factory.ConnectionSettings);
        using var connection = _factory.OpenNewConnection();
        using var transaction = connection.BeginTransaction();
        var entity = new SampleRow { Id = 1, Name = "中文 O'Brien\\end" };
        // SET 与 WHERE 使用同一字段的不同值，必须各自绑定，且只更新指定列。
        var updated = asynchronous
            ? await repository.UpdateAsync(entity, row => row.Name, row => row.Name == "old", transaction)
            : repository.Update(entity, row => row.Name, row => row.Name == "old", transaction);
        Assert.AreEqual(1, updated);
        Assert.AreEqual(entity.Name, Convert.ToString(_factory.ExecuteScalar(transaction,
            "SELECT display_name FROM sample WHERE row_id=1")));
        Assert.AreEqual("keep", Convert.ToString(_factory.ExecuteScalar(transaction,
            "SELECT display_name FROM sample WHERE row_id=2")));
        var deleted = asynchronous
            ? await repository.DeleteAsync(row => row.Name == entity.Name, transaction)
            : repository.Delete(row => row.Name == entity.Name, transaction);
        Assert.AreEqual(1, deleted);
        Assert.AreEqual(1L, Convert.ToInt64(_factory.ExecuteScalar(transaction, "SELECT COUNT(*) FROM sample")));
        Assert.AreEqual("keep", Convert.ToString(_factory.ExecuteScalar(transaction, "SELECT display_name FROM sample")));
        Assert.AreSame(connection, transaction.Connection);
        transaction.Rollback();
        // 新连接确认两次写入均未越过调用方事务提交。
        CollectionAssert.AreEqual(new[] { "old", "keep" },
            _factory.Query<string>("SELECT display_name FROM sample ORDER BY row_id").ToArray());
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task AddOrUpdate_PreservesDataOnConstraintFailureAndCallerRollback(bool dapper, bool asynchronous)
    {
        _factory.ExecuteNonQuery("CREATE TABLE names (Name VARCHAR(40) PRIMARY KEY) ENGINE=InnoDB");
        _factory.ExecuteNonQuery("INSERT INTO names VALUES ('old'),('new'),('keep')");
        _factory.ExecuteNonQuery("CREATE TABLE sample (row_id BIGINT PRIMARY KEY, display_name VARCHAR(40), " +
            "FOREIGN KEY (display_name) REFERENCES names(Name)) ENGINE=InnoDB");
        _factory.ExecuteNonQuery("INSERT INTO sample VALUES (1,'old'),(2,'keep')");
        BaseRepository<SampleRow> repository = dapper
            ? new DapperSampleRepository(_factory.ConnectionSettings) : new SampleRepository(_factory.ConnectionSettings);
        using var connection = _factory.OpenNewConnection();
        using var transaction = connection.BeginTransaction();
        foreach (var id in new[] { 1L, 3L })
        {
            var entity = new SampleRow { Id = id, Name = "new" };
            Assert.IsTrue(asynchronous
                ? await repository.AddOrUpdateAsync(entity, transaction: transaction)
                : repository.AddOrUpdate(entity, transaction: transaction));
        }
        // MySQL 此入口沿用 REPLACE：外键失败不能把此前的有效行删除。
        var error = await Assert.ThrowsAsync<MySqlException>(async () =>
        {
            var invalid = new SampleRow { Id = 1, Name = "missing-parent" };
            if (asynchronous) await repository.AddOrUpdateAsync(invalid, transaction: transaction);
            else repository.AddOrUpdate(invalid, transaction: transaction);
        });
        Assert.AreEqual(1452, error.Number);
        CollectionAssert.AreEqual(new[] { "new", "keep", "new" },
            _factory.Query<string>(transaction, "SELECT display_name FROM sample ORDER BY row_id").ToArray());
        Assert.AreSame(connection, transaction.Connection);
        transaction.Rollback();
        CollectionAssert.AreEqual(new[] { "old", "keep" },
            _factory.Query<string>("SELECT display_name FROM sample ORDER BY row_id").ToArray());
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task ConcurrentAdd_ReturnsIdentityOfEachInsertedRow(bool dapper)
    {
        _factory.ExecuteNonQuery("CREATE TABLE sample (row_id BIGINT PRIMARY KEY AUTO_INCREMENT, " +
            "display_name VARCHAR(40) NOT NULL UNIQUE) ENGINE=InnoDB");
        BaseRepository<SampleRow> repository = dapper
            ? new DapperSampleRepository(_factory.ConnectionSettings) : new SampleRepository(_factory.ConnectionSettings);
        var entities = Enumerable.Range(0, 8).Select(index => new SampleRow { Name = $"request-{index}" }).ToArray();
        var start = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var writes = entities.Select(async entity =>
        {
            await start.Task;
            Assert.IsTrue(await repository.AddAsync(entity, returnAutoIncrementId: true));
        }).ToArray();
        // 统一释放起跑信号；不依赖固定延时，也不假定服务器分配 ID 的先后顺序。
        start.SetResult(true);
        await Task.WhenAll(writes);
        var actual = _factory.Query<SampleRow>("SELECT row_id,display_name FROM sample")
            .ToDictionary(row => row.Id, row => row.Name);
        Assert.AreEqual(entities.Length, actual.Count);
        Assert.AreEqual(entities.Length, entities.Select(entity => entity.Id).Distinct().Count());
        foreach (var entity in entities)
        {
            Assert.IsTrue(actual.TryGetValue(entity.Id, out var name), $"未找到回写 ID：{entity.Id}");
            Assert.AreEqual(entity.Name, name);
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task NativeQuery_MapsTextGuidToScalarAndEntity(bool asynchronous)
    {
        var expected = Guid.Parse("6a7010f5-2d91-4a04-b2d1-47ead5908d42");
        _factory.ExecuteNonQuery("CREATE TABLE guid_values (Value VARCHAR(40), OptionalValue VARCHAR(40), MissingValue VARCHAR(40))");
        _factory.ExecuteNonQuery("INSERT INTO guid_values VALUES (@value,@value,NULL)",
            new[] { new MySqlParameter("value", expected.ToString("D")) });
        // VARCHAR 由真实驱动返回字符串，不能依赖驱动替框架转换为 Guid。
        Assert.IsInstanceOfType<string>(_factory.ExecuteScalar("SELECT Value FROM guid_values"));
        var value = asynchronous ? await _factory.GetAsync<Guid>("SELECT Value FROM guid_values")
            : _factory.Get<Guid>("SELECT Value FROM guid_values");
        Assert.AreEqual(expected, value);
        var entity = asynchronous ? await _factory.GetAsync<GuidRow>("SELECT * FROM guid_values")
            : _factory.Get<GuidRow>("SELECT * FROM guid_values");
        Assert.AreEqual(expected, entity.Value);
        Assert.AreEqual(expected, entity.OptionalValue);
        Assert.IsNull(entity.MissingValue);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task NativeScalar_ConvertsGuidText(bool asynchronous)
    {
        var expected = Guid.Parse("6a7010f5-2d91-4a04-b2d1-47ead5908d42");
        _factory.ExecuteNonQuery("CREATE TABLE guid_values (Value VARCHAR(40))");
        _factory.ExecuteNonQuery("INSERT INTO guid_values VALUES (@value)",
            new[] { new MySqlParameter("value", expected.ToString("D")) });
        const string sql = "SELECT Value FROM guid_values";
        Assert.IsInstanceOfType<string>(_factory.ExecuteScalar(sql));
        Assert.AreEqual(expected, asynchronous ? await _factory.ExecuteScalarAsync<Guid>(sql)
            : _factory.ExecuteScalar<Guid>(sql));
        Assert.AreEqual(expected, asynchronous ? await _factory.ExecuteScalarAsync<Guid?>(sql)
            : _factory.ExecuteScalar<Guid?>(sql));
    }

    private sealed class GuidRow
    {
        public Guid Value { get; set; }
        public Guid? OptionalValue { get; set; }
        public Guid? MissingValue { get; set; }
    }

    [Table("entity_template")]
    private sealed class UpgradeRow
    {
        [Key]
        public long Id { get; set; }
        [Required, System.ComponentModel.DefaultValue("O'Brien")]
        public string Name { get; set; }
    }

    private sealed class ProcedureOutput { public long Value { get; set; } }
    private sealed class DapperRepository(MultiConnectionSettings settings) : DapperBaseRepository(settings);
    private sealed class DapperSampleRepository(MultiConnectionSettings settings) : DapperBaseRepository<SampleRow>(settings);

    private enum UnsignedNumber : ulong { Maximum = ulong.MaxValue }
    [Table("sample")]
    private sealed class ValueRow
    {
        public string TextValue { get; set; }
        public UnsignedNumber UnsignedValue { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
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
