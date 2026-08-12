using System;
using System.IO;
using System.Text.Json;
using System.Data.SQLite;
using Sean.Core.DbRepository.Dapper;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository.Test;

/// <summary>
/// 测试工程自有的 SQLite 仓储与运行配置，避免单元测试依赖示例项目。
/// </summary>
internal static class TestInfrastructure
{
    static TestInfrastructure()
    {
        DatabaseType.SQLite.SetDbProviderMap(new DbProviderMap("System.Data.SQLite", SQLiteFactory.Instance));
        DbContextConfiguration.Configure(options =>
        {
            options.SynchronousWriteOptions.Enable = true;
            options.SynchronousWriteOptions.LockTimeout = 30000;
            options.SynchronousWriteOptions.OnLockTakenFailed = lockTimeout =>
            {
                Console.WriteLine($"######获取同步写入锁失败({lockTimeout}ms)");
                return true;
            };
            options.BulkEntityCount = 200;
        });
    }

    public static void EnsureInitialized()
    {
        // 调用此方法会触发静态构造函数，确保无仓储依赖的测试也使用相同的数据库配置。
    }

    public static TestRepository CreateRepository()
    {
        return new TestRepository();
    }

    public static TestLogger CreateLogger()
    {
        return new TestLogger();
    }

    public static bool EnablePerformanceComparisonTest
    {
        get
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (!File.Exists(path))
            {
                return false;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(path),
                new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
            return document.RootElement.TryGetProperty("UnitTestOptions", out var options)
                   && options.TryGetProperty("EnablePerformanceComparisonTest", out var enabled)
                   && enabled.GetBoolean();
        }
    }

    internal static ConnectionStringOptions CreateConnectionOptions()
    {
        var builder = new SQLiteConnectionStringBuilder
        {
            DataSource = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test.db"),
            Version = 3,
            Pooling = true,
            BusyTimeout = 30000,
            JournalMode = SQLiteJournalModeEnum.Wal
        };
        return ConnectionStringOptions.Create(builder.ConnectionString, SQLiteFactory.Instance);
    }
}

internal sealed class TestRepository : DapperBaseRepository<TestEntity>
{
    public TestRepository() : base(TestInfrastructure.CreateConnectionOptions())
    {
    }

    public override string TableName()
    {
        var tableName = base.TableName();
        AutoCreateTable(tableName);
        return tableName;
    }
}

internal sealed class TestLogger
{
    public void LogInfo(string message)
    {
        Console.WriteLine(message);
    }
}
