using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sean.Core.DbRepository.Dapper;

namespace Sean.Core.DbRepository.Test;

/// <summary>
/// 验证自增主键回写的目标类型、边界值及异常语义；不修改各数据库的自增 SQL。
/// </summary>
[TestClass]
public class IdentityPrimaryKeyTypeTest
{
    [TestMethod]
    [DataRow(typeof(byte), 255L)]
    [DataRow(typeof(short), 32767L)]
    [DataRow(typeof(int), 2147483647L)]
    [DataRow(typeof(long), long.MaxValue)]
    [DataRow(typeof(byte?), 255L)]
    [DataRow(typeof(short?), 32767L)]
    [DataRow(typeof(int?), 2147483647L)]
    [DataRow(typeof(long?), long.MaxValue)]
    [DataRow(typeof(LongIdentity), long.MaxValue)]
    [DataRow(typeof(object), long.MaxValue)]
    [DataRow(typeof(IConvertible), long.MaxValue)]
    public async Task Add_ReturnIdentity_AssignsExactPropertyTypeOnSQLite(Type propertyType, long identity)
    {
        foreach (var useDapper in new[] { false, true })
            foreach (var asynchronous in new[] { false, true })
            {
                await RunTypedTest(nameof(VerifySQLiteIdentityAsync), propertyType,
                    identity, useDapper, asynchronous, false);
            }
    }

    [TestMethod]
    [DataRow(typeof(byte), 256L)]
    [DataRow(typeof(short), 32768L)]
    [DataRow(typeof(int), 2147483648L)]
    [DataRow(typeof(byte?), 256L)]
    [DataRow(typeof(short?), 32768L)]
    [DataRow(typeof(int?), 2147483648L)]
    public async Task Add_ReturnIdentityOverflow_DoesNotTruncateOrOverwriteProperty(Type propertyType, long identity)
    {
        foreach (var useDapper in new[] { false, true })
            foreach (var asynchronous in new[] { false, true })
            {
                await RunTypedTest(nameof(VerifySQLiteIdentityAsync), propertyType,
                    identity, useDapper, asynchronous, true);
            }
    }

    [TestMethod]
    [DataRow(typeof(byte), 255L)]
    [DataRow(typeof(short), 32767L)]
    [DataRow(typeof(int), 2147483647L)]
    [DataRow(typeof(long), long.MaxValue)]
    [DataRow(typeof(byte?), 255L)]
    [DataRow(typeof(short?), 32767L)]
    [DataRow(typeof(int?), 2147483647L)]
    [DataRow(typeof(long?), long.MaxValue)]
    [DataRow(typeof(LongIdentity), long.MaxValue)]
    [DataRow(typeof(object), long.MaxValue)]
    [DataRow(typeof(IConvertible), long.MaxValue)]
    public async Task Add_SeparateIdentityQuery_AssignsExactPropertyType(Type propertyType, long identity)
    {
        foreach (var asynchronous in new[] { false, true })
        {
            await RunTypedTest(nameof(VerifySeparateIdentityAsync), propertyType, identity, asynchronous, false);
        }
    }

    [TestMethod]
    [DataRow(typeof(byte), 256L)]
    [DataRow(typeof(short), 32768L)]
    [DataRow(typeof(int), 2147483648L)]
    [DataRow(typeof(byte?), 256L)]
    [DataRow(typeof(short?), 32768L)]
    [DataRow(typeof(int?), 2147483648L)]
    public async Task Add_SeparateIdentityQueryOverflow_LeavesPropertyUnchanged(Type propertyType, long identity)
    {
        foreach (var asynchronous in new[] { false, true })
        {
            await RunTypedTest(nameof(VerifySeparateIdentityAsync), propertyType, identity, asynchronous, true);
        }
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task Add_WithoutIdentityReturn_PreservesProperty(bool useDapper, bool asynchronous)
    {
        using var connection = OpenDatabase();
        using var transaction = connection.BeginTransaction();
        var repository = CreateRepository<byte?>(useDapper);
        var entity = new IdentityEntity<byte?> { Name = "不要求回写" };

        Assert.IsTrue(asynchronous
            ? await repository.AddAsync(entity, transaction: transaction)
            : repository.Add(entity, transaction: transaction));

        Assert.IsNull(entity.Id);
        Assert.AreEqual(1L, ReadScalar(connection, transaction, "SELECT COUNT(*) FROM IdentityTypes"));
        transaction.Rollback();
    }

    [TestMethod]
    public void IdentityProperty_OriginalAssignmentSupportsLongEnumAndAssignableTypes()
    {
        var entity = new IdentityEntity<LongIdentity>();
        typeof(IdentityEntity<LongIdentity>).GetProperty(nameof(entity.Id)).SetValue(entity, 17L, null);
        Assert.AreEqual(17L, Convert.ToInt64(entity.Id));
        var objectEntity = new IdentityEntity<object>();
        typeof(IdentityEntity<object>).GetProperty(nameof(entity.Id)).SetValue(objectEntity, 19L, null);
        Assert.AreEqual(19L, objectEntity.Id);
        var interfaceEntity = new IdentityEntity<IConvertible>();
        typeof(IdentityEntity<IConvertible>).GetProperty(nameof(entity.Id)).SetValue(interfaceEntity, 23L, null);
        Assert.AreEqual(23L, interfaceEntity.Id);
    }

    [TestMethod]
    [DataRow(typeof(bool))]
    [DataRow(typeof(bool?))]
    [DataRow(typeof(char))]
    [DataRow(typeof(char?))]
    [DataRow(typeof(string))]
    public async Task Add_UnsupportedIdentityType_PreservesOriginalAssignmentFailure(Type propertyType)
    {
        foreach (var asynchronous in new[] { false, true })
        {
            await RunTypedTest(nameof(VerifyUnsupportedIdentityAsync), propertyType, asynchronous);
        }
    }

    [TestMethod]
    public async Task Add_SeparateIdentityQueryFailure_PreservesFalseResult()
    {
        foreach (var asynchronous in new[] { false, true })
            foreach (var identity in new[] { 0L, -1L })
            {
                var repository = new SeparateIdentityRepository<int>(identity);
                var entity = new IdentityEntity<int> { Name = "无有效主键" };
                Assert.IsFalse(asynchronous
                    ? await repository.AddAsync(entity, true)
                    : repository.Add(entity, true));
                Assert.AreEqual(0, entity.Id);
            }

        var failedRepository = new SeparateIdentityRepository<int>(1) { InsertSucceeds = false };
        Assert.IsFalse(failedRepository.Add(new IdentityEntity<int>(), true));
        Assert.IsFalse(await failedRepository.AddAsync(new IdentityEntity<int>(), true));
        Assert.IsNull(failedRepository.IdentityCommand, "INSERT 失败时不能继续查询主键。");
    }

    private static Task RunTypedTest(string methodName, Type propertyType, params object[] arguments)
    {
        // 让同一组数据库断言覆盖各个真实的泛型实体属性类型，避免复制八份实体与测试逻辑。
        var method = typeof(IdentityPrimaryKeyTypeTest).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
        return (Task)method.MakeGenericMethod(propertyType).Invoke(null, arguments);
    }

    private static async Task VerifySQLiteIdentityAsync<TKey>(long identity, bool useDapper, bool asynchronous, bool overflow)
    {
        using var connection = OpenDatabase();
        using var transaction = connection.BeginTransaction();
        using (var seed = connection.CreateCommand())
        {
            seed.Transaction = transaction;
            seed.CommandText = "INSERT INTO IdentityTypes(Id, Name) VALUES(@Id, 'seed')";
            seed.Parameters.AddWithValue("@Id", identity - 1);
            seed.ExecuteNonQuery();
        }
        var repository = CreateRepository<TKey>(useDapper);
        var entity = new IdentityEntity<TKey> { Name = "数据库生成的主键" };
        async Task<bool> Insert()
        {
            return asynchronous
                ? await repository.AddAsync(entity, true, transaction: transaction)
                : repository.Add(entity, true, transaction: transaction);
        }

        if (overflow)
        {
            await Assert.ThrowsAsync<OverflowException>(() => Insert());
            Assert.AreEqual(default(TKey), entity.Id, "转换失败不能截断数值，也不能覆盖实体主键。");
        }
        else
        {
            Assert.IsTrue(await Insert());
            AssertIdentityType(identity, entity);
        }

        Assert.AreEqual(identity, ReadScalar(connection, transaction, "SELECT MAX(Id) FROM IdentityTypes"));
        Assert.AreEqual(2L, ReadScalar(connection, transaction, "SELECT COUNT(*) FROM IdentityTypes"));
        // 回写发生在 INSERT 之后；转换异常不代表数据库未写入，调用方事务仍可完整回滚。
        Assert.AreEqual(ConnectionState.Open, connection.State);
        transaction.Rollback();
        Assert.AreEqual(0L, ReadScalar(connection, null, "SELECT COUNT(*) FROM IdentityTypes"));
    }

    private static async Task VerifySeparateIdentityAsync<TKey>(long identity, bool asynchronous, bool overflow)
    {
        var repository = new SeparateIdentityRepository<TKey>(identity);
        var entity = new IdentityEntity<TKey> { Name = "分步查询主键" };
        async Task<bool> Insert()
        {
            return asynchronous ? await repository.AddAsync(entity, true) : repository.Add(entity, true);
        }
        if (overflow)
        {
            await Assert.ThrowsAsync<OverflowException>(() => Insert());
            Assert.AreEqual(default(TKey), entity.Id);
        }
        else
        {
            Assert.IsTrue(await Insert());
            AssertIdentityType(identity, entity);
        }
        Assert.AreSame(repository.InsertCommand.Connection, repository.IdentityCommand.Connection);
        Assert.AreEqual("SELECT @@IDENTITY AS Id", repository.IdentityCommand.Sql);
        Assert.IsTrue(repository.ConnectionDisposed);
    }

    private static void AssertIdentityType<TKey>(long expected, IdentityEntity<TKey> entity)
    {
        Assert.AreEqual(expected, Convert.ToInt64(entity.Id));
        var targetType = Nullable.GetUnderlyingType(typeof(TKey)) ?? typeof(TKey);
        var expectedType = targetType == typeof(object) || targetType == typeof(IConvertible) ? typeof(long) : targetType;
        Assert.AreEqual(expectedType, ((object)entity.Id).GetType());
    }

    private static async Task VerifyUnsupportedIdentityAsync<TKey>(bool asynchronous)
    {
        var entity = new IdentityEntity<TKey> { Name = "不支持的主键类型" };
        // 类型转换不能把原先会拒绝的 bool/string 等模型变为静默成功。
        Assert.Throws<ArgumentException>(() => typeof(IdentityEntity<TKey>)
            .GetProperty(nameof(entity.Id)).SetValue(entity, 1L, null));
        var repository = new SeparateIdentityRepository<TKey>(1);
        async Task<bool> Insert()
        {
            return asynchronous ? await repository.AddAsync(entity, true) : repository.Add(entity, true);
        }
        await Assert.ThrowsAsync<ArgumentException>(() => Insert());
        Assert.AreEqual(default(TKey), entity.Id);
    }

    private static IBaseRepository<IdentityEntity<TKey>> CreateRepository<TKey>(bool useDapper)
    {
        return useDapper ? new DapperIdentityRepository<TKey>() : new CoreIdentityRepository<TKey>();
    }

    private static SQLiteConnection OpenDatabase()
    {
        var connection = new SQLiteConnection("Data Source=:memory:;Version=3;Pooling=False;");
        try
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE IdentityTypes(Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT)";
            command.ExecuteNonQuery();
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    private static long ReadScalar(SQLiteConnection connection, SQLiteTransaction transaction, string sql)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        return Convert.ToInt64(command.ExecuteScalar());
    }

    private enum LongIdentity : long { }

    [Table("IdentityTypes")]
    private sealed class IdentityEntity<TKey>
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public TKey Id { get; set; }
        public string Name { get; set; }
    }

    private sealed class CoreIdentityRepository<TKey> : BaseRepository<IdentityEntity<TKey>>
    {
        public CoreIdentityRepository() : base("Data Source=:memory:;Version=3;Pooling=False;", SQLiteFactory.Instance) { }
    }

    private sealed class DapperIdentityRepository<TKey> : DapperBaseRepository<IdentityEntity<TKey>>
    {
        public DapperIdentityRepository() : base("Data Source=:memory:;Version=3;Pooling=False;", SQLiteFactory.Instance) { }
    }

    private sealed class SeparateIdentityRepository<TKey> : BaseRepository<IdentityEntity<TKey>>
    {
        private readonly long _identity;
        public bool InsertSucceeds { get; set; } = true;
        public ISqlCommand InsertCommand { get; private set; }
        public ISqlCommand IdentityCommand { get; private set; }
        public bool ConnectionDisposed { get; private set; }

        public SeparateIdentityRepository(long identity)
            : base(new ConnectionStringOptions("Data Source=:memory:;Version=3;Pooling=False;", SQLiteFactory.Instance)
            { DbType = DatabaseType.MsAccess })
        {
            _identity = identity;
        }

        protected override DbConnection OpenNewConnection(bool master = true)
        {
            var connection = base.OpenNewConnection(master);
            connection.Disposed += (_, _) => ConnectionDisposed = true;
            return connection;
        }

        // 仅验证分步查询分支的数值回写与连接复用，Access SQL 并未在真实 Access 上执行。
        public override int Execute(ISqlCommand sqlCommand)
        {
            InsertCommand = sqlCommand;
            return InsertSucceeds ? 1 : 0;
        }
        public override Task<int> ExecuteAsync(ISqlCommand sqlCommand) => Task.FromResult(Execute(sqlCommand));
        public override T ExecuteScalar<T>(ISqlCommand sqlCommand)
        {
            IdentityCommand = sqlCommand;
            return (T)(object)_identity;
        }
        public override Task<T> ExecuteScalarAsync<T>(ISqlCommand sqlCommand) => Task.FromResult(ExecuteScalar<T>(sqlCommand));
    }
}
