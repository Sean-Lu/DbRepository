using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SQLite;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sean.Core.DbRepository.CodeFirst;
using Sean.Core.DbRepository.DbFirst;

namespace Sean.Core.DbRepository.Test;

/// <summary>
/// CodeFirst 与 DbFirst 共享生成器的并发隔离测试。
/// </summary>
[TestClass]
[DoNotParallelize]
public class GeneratorConcurrencyCorrectnessTest
{
    [TestMethod]
    public async Task DatabaseUpgrader_SerializesInitializeAndSqlGenerationByGeneratorInstance()
    {
        var original = SqlGeneratorFactory.GetSqlGenerator(DatabaseType.SQLite);
        var generator = new TrackingSqlGenerator();
        try
        {
            SqlGeneratorFactory.SetSqlGenerator(DatabaseType.SQLite, generator);
            Assert.AreSame(generator, SqlGeneratorFactory.GetSqlGenerator(DatabaseType.SQLite));

            var tasks = Enumerable.Range(0, 24).Select(index => Task.Run(() =>
            {
                var marker = $"sql-generator-{index}";
                var upgrader = new DatabaseUpgrader(CreateDbFactory(marker));
                upgrader.Upgrade(typeof(ConcurrencyEntity), _ => marker);
            }));

            await Task.WhenAll(tasks);

            Assert.AreEqual(1, generator.MaximumConcurrency);
            Assert.AreEqual(0, generator.ConnectionMismatches.Count);
        }
        finally
        {
            SqlGeneratorFactory.SetSqlGenerator(DatabaseType.SQLite, original);
        }
    }

    [TestMethod]
    public async Task MetadataRead_SerializesInitializeAndReadByGeneratorInstance()
    {
        var original = CodeGeneratorFactory.GetCodeGenerator(DatabaseType.SQLite);
        var generator = new TrackingCodeGenerator();
        try
        {
            CodeGeneratorFactory.SetCodeGenerator(DatabaseType.SQLite, generator);
            Assert.AreSame(generator, CodeGeneratorFactory.GetCodeGenerator(DatabaseType.SQLite));

            var tasks = Enumerable.Range(0, 24).Select(index => Task.Run(() =>
            {
                var marker = $"code-generator-{index}";
                var dbFactory = CreateDbFactory(marker);
                var probe = new MetadataConcurrencyProbe();
                probe.Initialize(dbFactory);
                probe.ReadDbMissingFields(marker);
            }));

            await Task.WhenAll(tasks);

            Assert.AreEqual(1, generator.MaximumConcurrency);
            Assert.AreEqual(0, generator.ConnectionMismatches.Count);
        }
        finally
        {
            CodeGeneratorFactory.SetCodeGenerator(DatabaseType.SQLite, original);
        }
    }

    [TestMethod]
    public void GeneratorLock_IsReleasedAfterGenerationThrows()
    {
        var original = SqlGeneratorFactory.GetSqlGenerator(DatabaseType.SQLite);
        var generator = new TrackingSqlGenerator { ThrowOnNextGeneration = true };
        try
        {
            SqlGeneratorFactory.SetSqlGenerator(DatabaseType.SQLite, generator);
            var failedUpgrader = new DatabaseUpgrader(CreateDbFactory("failed"));
            Assert.Throws<InvalidOperationException>(() =>
                failedUpgrader.Upgrade(typeof(ConcurrencyEntity), _ => "failed"));

            var successfulUpgrade = Task.Run(() =>
            {
                var upgrader = new DatabaseUpgrader(CreateDbFactory("success"));
                upgrader.Upgrade(typeof(ConcurrencyEntity), _ => "success");
            });

            Assert.IsTrue(successfulUpgrade.Wait(TimeSpan.FromSeconds(5)), "生成异常后未释放生成器锁。");
            Assert.IsFalse(successfulUpgrade.IsFaulted);
            Assert.AreEqual(1, generator.MaximumConcurrency);
        }
        finally
        {
            SqlGeneratorFactory.SetSqlGenerator(DatabaseType.SQLite, original);
        }
    }

    private static DbFactory CreateDbFactory(string connectionString)
    {
        return new DbFactory(new MultiConnectionSettings(
            ConnectionStringOptions.Create(connectionString, SQLiteFactory.Instance)));
    }

    [Table("ConcurrencyEntity")]
    private sealed class ConcurrencyEntity
    {
        public int Id { get; set; }
    }

    private sealed class TrackingSqlGenerator : BaseSqlGenerator
    {
        private int _activeOperations;
        private int _maximumConcurrency;

        public TrackingSqlGenerator() : base(DatabaseType.SQLite)
        {
        }

        public int MaximumConcurrency => _maximumConcurrency;
        public ConcurrentQueue<string> ConnectionMismatches { get; } = new();
        public bool ThrowOnNextGeneration { get; set; }

        public override void Initialize(DbFactory dbFactory)
        {
            UpdateMaximum(Interlocked.Increment(ref _activeOperations));
            base.Initialize(dbFactory);
            Thread.Sleep(5);
        }

        public override List<string> GetCreateTableSql(Type entityType, bool ignoreIfExists = false,
            Func<string, string> tableNameFunc = null)
        {
            return CompleteGeneration(tableNameFunc);
        }

        public override List<string> GetUpgradeSql(Type entityType, Func<string, string> tableNameFunc = null)
        {
            return CompleteGeneration(tableNameFunc);
        }

        private List<string> CompleteGeneration(Func<string, string> tableNameFunc)
        {
            try
            {
                var expected = tableNameFunc?.Invoke(string.Empty);
                var actual = _db.ConnectionSettings.GetConnectionString();
                if (!string.Equals(expected, actual, StringComparison.Ordinal))
                {
                    ConnectionMismatches.Enqueue($"{expected}|{actual}");
                }

                Thread.Sleep(5);
                if (ThrowOnNextGeneration)
                {
                    ThrowOnNextGeneration = false;
                    throw new InvalidOperationException("模拟 SQL 生成失败。");
                }

                return new List<string>();
            }
            finally
            {
                Interlocked.Decrement(ref _activeOperations);
            }
        }

        private void UpdateMaximum(int current)
        {
            int maximum;
            do
            {
                maximum = _maximumConcurrency;
                if (current <= maximum)
                {
                    return;
                }
            } while (Interlocked.CompareExchange(ref _maximumConcurrency, current, maximum) != maximum);
        }
    }

    private sealed class TrackingCodeGenerator : BaseCodeGenerator, ICodeGenerator
    {
        private int _activeOperations;
        private int _maximumConcurrency;

        public TrackingCodeGenerator() : base(DatabaseType.SQLite)
        {
        }

        public int MaximumConcurrency => _maximumConcurrency;
        public ConcurrentQueue<string> ConnectionMismatches { get; } = new();

        public override void Initialize(DbFactory dbFactory)
        {
            UpdateMaximum(Interlocked.Increment(ref _activeOperations));
            base.Initialize(dbFactory);
            Thread.Sleep(5);
        }

        public TableInfoModel GetTableInfo(string tableName)
        {
            return new TableInfoModel();
        }

        public List<TableFieldModel> GetTableFieldInfo(string tableName)
        {
            try
            {
                var actual = _db.ConnectionSettings.GetConnectionString();
                if (!string.Equals(tableName, actual, StringComparison.Ordinal))
                {
                    ConnectionMismatches.Enqueue($"{tableName}|{actual}");
                }

                Thread.Sleep(5);
                return new List<TableFieldModel>();
            }
            finally
            {
                Interlocked.Decrement(ref _activeOperations);
            }
        }

        public List<TableFieldReferenceModel> GetTableFieldReferenceInfo(string tableName)
        {
            return new List<TableFieldReferenceModel>();
        }

        private void UpdateMaximum(int current)
        {
            int maximum;
            do
            {
                maximum = _maximumConcurrency;
                if (current <= maximum)
                {
                    return;
                }
            } while (Interlocked.CompareExchange(ref _maximumConcurrency, current, maximum) != maximum);
        }
    }

    private sealed class MetadataConcurrencyProbe : SqlGeneratorForSQLite
    {
        public List<EntityFieldInfo> ReadDbMissingFields(string tableName)
        {
            return GetDbMissingTableFields(typeof(ConcurrencyEntity), tableName);
        }
    }
}
