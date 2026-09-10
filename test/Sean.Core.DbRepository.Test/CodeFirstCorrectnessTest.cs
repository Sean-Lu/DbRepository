using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using System.Data.SQLite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sean.Core.DbRepository.CodeFirst;
using Sean.Core.DbRepository.DbFirst;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository.Test;

[TestClass]
public class CodeFirstCorrectnessTest
{
    [TestMethod]
    public void MySqlStringWithoutMaxLengthShouldUseVarchar255()
    {
        var sql = GetCreateTableSql<UnboundedStringEntity>(DatabaseType.MySql);

        StringAssert.Contains(sql, "`Value` varchar(255)");
        Assert.IsFalse(sql.Contains("`Value` varchar,", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BooleanDefaultsShouldUseSupportedLiterals()
    {
        var databaseTypes = new[]
        {
            DatabaseType.PostgreSql,
            DatabaseType.OpenGauss,
            DatabaseType.HighgoDB,
            DatabaseType.IvorySQL,
            DatabaseType.KingbaseES,
            DatabaseType.Firebird,
            DatabaseType.DuckDB,
            DatabaseType.QuestDB
        };

        foreach (var databaseType in databaseTypes)
        {
            var sql = GetCreateTableSql<BooleanDefaultEntity>(databaseType);
            StringAssert.Contains(sql, "DEFAULT TRUE", databaseType.ToString());
            StringAssert.Contains(sql, "DEFAULT FALSE", databaseType.ToString());
        }
    }

    [TestMethod]
    public void DdlTextShouldEscapeSingleQuotes()
    {
        var databaseTypes = new[]
        {
            DatabaseType.MySql,
            DatabaseType.MariaDB,
            DatabaseType.TiDB,
            DatabaseType.OceanBase,
            DatabaseType.SqlServer,
            DatabaseType.Firebird,
            DatabaseType.PostgreSql,
            DatabaseType.OpenGauss,
            DatabaseType.ClickHouse,
            DatabaseType.Dameng,
            DatabaseType.Xugu
        };

        foreach (var databaseType in databaseTypes)
        {
            var sql = GetCreateTableSql<DdlTextEntity>(databaseType);
            StringAssert.Contains(sql, "DEFAULT 'O''Brien'", databaseType.ToString());
            StringAssert.Contains(sql, "Owner''s table", databaseType.ToString());
            StringAssert.Contains(sql, "User''s value", databaseType.ToString());
        }

        var oracleSql = GetCreateTableSql<DdlTextEntity>(DatabaseType.Oracle);
        StringAssert.Contains(oracleSql, "execute immediate 'CREATE TABLE \"Owner''sTable\"");
        StringAssert.Contains(oracleSql, "DEFAULT ''O''''Brien''");
        StringAssert.Contains(oracleSql, "Owner''''s table");
        StringAssert.Contains(oracleSql, "User''''s value");
    }

    [TestMethod]
    public void MySqlCompatibleDdlTextWithBackslashShouldFailClearly()
    {
        var databaseTypes = new[]
        {
            DatabaseType.MySql,
            DatabaseType.MariaDB,
            DatabaseType.TiDB,
            DatabaseType.OceanBase
        };

        foreach (var databaseType in databaseTypes)
        {
            var exception = Assert.Throws<NotSupportedException>(
                () => GetCreateTableSql<MySqlBackslashDdlEntity>(databaseType), databaseType.ToString());

            StringAssert.Contains(exception.Message, databaseType.ToString());
            StringAssert.Contains(exception.Message, "NO_BACKSLASH_ESCAPES");
        }
    }

    [TestMethod]
    public void NumericDefaultsShouldIgnoreCurrentCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");

            var sql = GetCreateTableSql<NumericDefaultEntity>(DatabaseType.MySql);

            StringAssert.Contains(sql, "DEFAULT 1234.5");
            Assert.IsFalse(sql.Contains("DEFAULT 1234,5", StringComparison.Ordinal));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [TestMethod]
    public void SQLiteShouldOnlyUseAutoincrementForIntegerPrimaryKey()
    {
        var nonPrimaryIdentitySql = GetCreateTableSql<SQLiteNonPrimaryIdentityEntity>(DatabaseType.SQLite);
        var nonIntegerPrimaryKeySql = GetCreateTableSql<SQLiteNonIntegerIdentityEntity>(DatabaseType.SQLite);
        var compositePrimaryKeySql = GetCreateTableSql<SQLiteCompositePrimaryKeyEntity>(DatabaseType.SQLite);

        Assert.IsFalse(nonPrimaryIdentitySql.Contains("AUTOINCREMENT", StringComparison.Ordinal));
        StringAssert.Contains(nonPrimaryIdentitySql, "PRIMARY KEY (`Id`)");
        Assert.IsFalse(nonIntegerPrimaryKeySql.Contains("AUTOINCREMENT", StringComparison.Ordinal));
        StringAssert.Contains(nonIntegerPrimaryKeySql, "`Id` BIGINT NOT NULL");
        StringAssert.Contains(nonIntegerPrimaryKeySql, "PRIMARY KEY (`Id`)");
        Assert.IsFalse(compositePrimaryKeySql.Contains("AUTOINCREMENT", StringComparison.Ordinal));
        StringAssert.Contains(compositePrimaryKeySql, "PRIMARY KEY (`Id`,`TenantId`)");
    }

    [TestMethod]
    public void SQLiteCompositePrimaryKeySqlShouldCreateCompositeConstraint()
    {
        var sql = GetCreateTableSql<SQLiteCompositePrimaryKeyEntity>(DatabaseType.SQLite);
        using var connection = new SQLiteConnection("Data Source=:memory:;Version=3;New=True;");
        connection.Open();

        using (var createCommand = connection.CreateCommand())
        {
            createCommand.CommandText = sql;
            createCommand.ExecuteNonQuery();
        }

        using var schemaCommand = connection.CreateCommand();
        schemaCommand.CommandText = "PRAGMA table_info(`SQLiteCompositePrimaryKey`);";
        using var reader = schemaCommand.ExecuteReader();
        var primaryKeyPositions = new Dictionary<string, long>();
        while (reader.Read())
        {
            primaryKeyPositions[reader.GetString(1)] = reader.GetInt64(5);
        }

        Assert.AreEqual(1L, primaryKeyPositions["Id"]);
        Assert.AreEqual(2L, primaryKeyPositions["TenantId"]);
    }

    [TestMethod]
    public void ClickHouseAndQuestDbShouldSkipNormalIndexAndRejectUniqueIndex()
    {
        foreach (var databaseType in new[] { DatabaseType.ClickHouse, DatabaseType.QuestDB })
        {
            var normalIndexSql = GetCreateTableSql<NormalIndexedEntity>(databaseType);
            var exception = Assert.Throws<NotSupportedException>(
                () => GetCreateTableSql<UniqueIndexedEntity>(databaseType), databaseType.ToString());

            Assert.IsFalse(normalIndexSql.Contains("CREATE INDEX", StringComparison.OrdinalIgnoreCase),
                databaseType.ToString());
            StringAssert.Contains(exception.Message, databaseType.ToString());
            StringAssert.Contains(exception.Message, "通用 CodeFirst");
        }
    }

    [TestMethod]
    public void SqlServerIgnoreIfExistsShouldCoverAllRelatedStatements()
    {
        var sql = GetCreateTableSql<IndexedEntity>(DatabaseType.SqlServer, true);

        StringAssert.StartsWith(sql, "IF NOT EXISTS");
        StringAssert.Contains(sql, $"{Environment.NewLine}BEGIN{Environment.NewLine}CREATE TABLE");
        StringAssert.Contains(sql, "CREATE INDEX");
        StringAssert.Contains(sql, "CREATE UNIQUE INDEX");
        Assert.IsTrue(sql.EndsWith($"END;{Environment.NewLine}", StringComparison.Ordinal));
        Assert.IsTrue(sql.LastIndexOf("CREATE UNIQUE INDEX", StringComparison.Ordinal) <
                      sql.LastIndexOf("END;", StringComparison.Ordinal));
    }

    [TestMethod]
    public void UnsupportedMetadataShouldThrowClearException()
    {
        var generator = new UnsupportedMetadataProbe();

        var dbMissingException = Assert.Throws<NotSupportedException>(() => generator.ReadDbMissingFields());
        var entityMissingException = Assert.Throws<NotSupportedException>(() => generator.ReadEntityMissingFields());

        StringAssert.Contains(dbMissingException.Message, "ClickHouse");
        StringAssert.Contains(dbMissingException.Message, "不支持读取表结构");
        StringAssert.Contains(entityMissingException.Message, "ClickHouse");
        StringAssert.Contains(entityMissingException.Message, "不支持读取表结构");
    }

    [TestMethod]
    [DoNotParallelize]
    [DataRow(DatabaseType.PostgreSql, true)]
    [DataRow(DatabaseType.MySql, false)]
    public void MetadataFieldComparisonShouldRespectDialect(DatabaseType databaseType, bool caseSensitive)
    {
        var originalCodeGenerator = CodeGeneratorFactory.GetCodeGenerator(databaseType);
        try
        {
            CodeGeneratorFactory.SetCodeGenerator(databaseType, new CaseDifferentMetadataCodeGenerator());
            var generator = new MetadataComparisonProbe(databaseType);

            var dbMissingFields = generator.ReadDbMissingFields();
            var entityMissingFields = generator.ReadEntityMissingFields();

            Assert.HasCount(caseSensitive ? 1 : 0, dbMissingFields);
            Assert.HasCount(caseSensitive ? 1 : 0, entityMissingFields);
            if (caseSensitive)
            {
                Assert.AreEqual("Value", dbMissingFields.Single().FieldName);
                Assert.AreEqual("value", entityMissingFields.Single().FieldName);
            }
        }
        finally
        {
            CodeGeneratorFactory.SetCodeGenerator(databaseType, originalCodeGenerator);
        }
    }

    [TestMethod]
    public void OracleUpgradeShouldEscapeDefaultAndCommentAtBothLiteralLevels()
    {
        var generator = new OracleUpgradeSqlGenerator();

        var sql = string.Join(Environment.NewLine, generator.GetUpgradeSql<OracleUpgradeEntity>());

        StringAssert.Contains(sql,
            "execute immediate 'ALTER TABLE \"OracleUpgrade\" ADD \"AddedValue\" NVARCHAR2(50) DEFAULT ''O''''Brien''';");
        StringAssert.Contains(sql,
            "execute immediate 'COMMENT ON COLUMN \"OracleUpgrade\".\"AddedValue\" IS ''User''''s value''';");
    }

    private static string GetCreateTableSql<TEntity>(DatabaseType databaseType, bool ignoreIfExists = false)
    {
        var generator = SqlGeneratorFactory.GetSqlGenerator(databaseType);
        return string.Join(Environment.NewLine, generator.GetCreateTableSql<TEntity>(ignoreIfExists));
    }

    [Table("UnboundedString")]
    private sealed class UnboundedStringEntity
    {
        [Column("Value")]
        public string Value { get; set; }
    }

    [Table("BooleanDefaults")]
    private sealed class BooleanDefaultEntity
    {
        [DefaultValue(true)]
        public bool IsEnabled { get; set; }

        [DefaultValue(false)]
        public bool IsDeleted { get; set; }
    }

    [Table("Owner'sTable")]
    [System.ComponentModel.DescriptionAttribute("Owner's table")]
    private sealed class DdlTextEntity
    {
        [MaxLength(50)]
        [DefaultValue("O'Brien")]
        [System.ComponentModel.DescriptionAttribute("User's value")]
        public string Value { get; set; }
    }

    [Table("MySqlBackslashDdl")]
    private sealed class MySqlBackslashDdlEntity
    {
        [MaxLength(100)]
        [DefaultValue("C:\\O'Brien")]
        public string Value { get; set; }
    }

    [Table("NumericDefault")]
    private sealed class NumericDefaultEntity
    {
        [Numeric(18, 2)]
        [DefaultValue(typeof(decimal), "1234.5")]
        public decimal Amount { get; set; }
    }

    [Table("SQLiteNonPrimaryIdentity")]
    private sealed class SQLiteNonPrimaryIdentityEntity
    {
        [Key]
        public long Id { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Sequence { get; set; }
    }

    [Table("SQLiteNonIntegerIdentity")]
    private sealed class SQLiteNonIntegerIdentityEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column(TypeName = "BIGINT")]
        public long Id { get; set; }
    }

    [Table("SQLiteCompositePrimaryKey")]
    private sealed class SQLiteCompositePrimaryKeyEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Key]
        public int TenantId { get; set; }
    }

    [Table("Indexed")]
    [Index(nameof(Name), IndexType = DbIndexType.Normal)]
    [Index(nameof(Code), IndexType = DbIndexType.Unique)]
    private sealed class IndexedEntity
    {
        [MaxLength(50)]
        public string Name { get; set; }

        [MaxLength(50)]
        public string Code { get; set; }
    }

    [Table("NormalIndexed")]
    [Index(nameof(Name), IndexType = DbIndexType.Normal)]
    private sealed class NormalIndexedEntity
    {
        [MaxLength(50)]
        public string Name { get; set; }
    }

    [Table("UniqueIndexed")]
    [Index(nameof(Code), IndexType = DbIndexType.Unique)]
    private sealed class UniqueIndexedEntity
    {
        [MaxLength(50)]
        public string Code { get; set; }
    }

    [Table("OracleUpgrade")]
    private sealed class OracleUpgradeEntity
    {
        [MaxLength(50)]
        [DefaultValue("O'Brien")]
        [System.ComponentModel.DescriptionAttribute("User's value")]
        public string AddedValue { get; set; }
    }

    private sealed class UnsupportedMetadataProbe : SqlGeneratorForClickHouse
    {
        public List<EntityFieldInfo> ReadDbMissingFields()
        {
            return GetDbMissingTableFields(typeof(UnboundedStringEntity), "UnboundedString");
        }

        public List<TableFieldModel> ReadEntityMissingFields()
        {
            return GetEntityMissingTableFields(typeof(UnboundedStringEntity), "UnboundedString");
        }
    }

    private sealed class MetadataComparisonProbe : SqlGeneratorForPostgreSql
    {
        public MetadataComparisonProbe(DatabaseType databaseType) : base(databaseType) { }

        public List<EntityFieldInfo> ReadDbMissingFields()
        {
            return GetDbMissingTableFields(typeof(UnboundedStringEntity), "UnboundedString");
        }

        public List<TableFieldModel> ReadEntityMissingFields()
        {
            return GetEntityMissingTableFields(typeof(UnboundedStringEntity), "UnboundedString");
        }
    }

    private sealed class CaseDifferentMetadataCodeGenerator : CodeGeneratorForPostgreSql
    {
        public override List<TableFieldModel> GetTableFieldInfo(string tableName)
        {
            return new List<TableFieldModel>
            {
                new()
                {
                    FieldName = "value"
                }
            };
        }
    }

    private sealed class OracleUpgradeSqlGenerator : SqlGeneratorForOracle
    {
        protected override bool IsTableExists(string tableName)
        {
            return true;
        }

        protected override List<EntityFieldInfo> GetDbMissingTableFields(Type entityType, string tableName)
        {
            return entityType.GetEntityInfo().FieldInfos
                .Where(fieldInfo => fieldInfo.PropertyName == nameof(OracleUpgradeEntity.AddedValue))
                .ToList();
        }
    }
}
