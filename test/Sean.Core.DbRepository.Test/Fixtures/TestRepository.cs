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
            // 每个测试仓储使用独立文件，防止不同用例的清表、并发写入互相影响。
            DataSource = Path.Combine(Path.GetTempPath(), $"Sean.DbRepository.Test.{Guid.NewGuid():N}.db"),
            Version = 3,
            Pooling = true,
            BusyTimeout = 30000,
            JournalMode = SQLiteJournalModeEnum.Wal
        };
        return ConnectionStringOptions.Create(builder.ConnectionString, SQLiteFactory.Instance);
    }
}

internal sealed class TestRepository : DapperBaseRepository<TestEntity>, IDisposable
{
    private readonly string _connectionString;
    private bool _disposed;

    internal string DatabasePath { get; }

    public TestRepository() : this(TestInfrastructure.CreateConnectionOptions())
    {
    }

    private TestRepository(ConnectionStringOptions options) : base(options)
    {
        _connectionString = options.ConnectionString;
        DatabasePath = new SQLiteConnectionStringBuilder(_connectionString).DataSource;
    }

    public override string TableName()
    {
        var tableName = base.TableName();
        AutoCreateTable(tableName);
        return tableName;
    }

    public void Dispose()
    {
        if (_disposed) return;
        // 只清理当前唯一连接串的池，不能用 ClearAllPools 干扰其他正在执行的测试。
        if (File.Exists(DatabasePath))
        {
            using var connection = new SQLiteConnection(_connectionString);
            // ClearPool 需要已打开连接的底层数据库句柄，未打开的新连接不能定位文件对应的池。
            connection.Open();
            SQLiteConnection.ClearPool(connection);
        }
        // 路径由私有构造链生成，仅删除本仓储拥有的数据库和 WAL 辅助文件，不清理旧 test.db。
        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            File.Delete(DatabasePath + suffix);
        }
        _disposed = true;
    }
}

internal sealed class TestLogger
{
    public void LogInfo(string message)
    {
        Console.WriteLine(message);
    }
}
