using System;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sean.Core.DbRepository.Test;

[TestClass]
[DoNotParallelize] // 数据行复用同一类型注册，不能互相覆盖处理器。
public class TypeHandlerExecutionTest
{
    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task Handler_OverridesBindingAndPreservesConnectionAfterFailure(bool asynchronous, bool enumValue)
    {
        object value = enumValue ? HandlerEnum.Value : new CustomValue();
        var type = value.GetType();
        var options = DbContextConfiguration.Options;
        var previous = options.GetTypeHandler(type);
        var handler = new Handler();
        using var connection = new SQLiteConnection("Data Source=:memory:;Pooling=False;");
        connection.Open();
        var repository = new Repository();
        async Task<string> Execute() => asynchronous
            ? await repository.ExecuteScalarAsync<string>("SELECT @Value", new { Value = value }, connection: connection)
            : repository.ExecuteScalar<string>("SELECT @Value", new { Value = value }, connection: connection);
        try
        {
            // 只注册本测试独有的类型，不替换其他用例使用的内置类型处理器。
            options.AddTypeHandler(type, handler);
            Assert.AreEqual("custom-value", await Execute());
            Assert.AreSame(value, handler.OriginalValue);
            Assert.AreEqual(DatabaseType.SQLite, handler.DatabaseType);
            Assert.AreEqual(ConnectionState.Open, connection.State);

            handler.Failure = new InvalidOperationException("类型处理器失败");
            var error = await Assert.ThrowsAsync<InvalidOperationException>(async () => { await Execute(); });
            Assert.AreSame(handler.Failure, error);
            Assert.AreEqual(ConnectionState.Open, connection.State);
            // 实际再次执行证明处理器异常没有释放外部连接，且失败没有缓存为永久转换结果。
            handler.Failure = null;
            Assert.AreEqual("custom-value", await Execute());
        }
        finally
        {
            if (previous == null) options.RemoveTypeHandler(type);
            else options.AddTypeHandler(type, previous);
        }
    }

    private sealed class Handler : ITypeHandler
    {
        public object OriginalValue { get; private set; }
        public DatabaseType DatabaseType { get; private set; }
        public Exception Failure { get; set; }
        public void Set(DbParameter parameter, object value, DatabaseType databaseType)
        {
            OriginalValue = value;
            DatabaseType = databaseType;
            if (Failure != null) throw Failure;
            // 枚举默认按整数绑定，处理器必须仍能覆盖为业务自定义的字符串表示。
            parameter.DbType = DbType.String;
            parameter.Value = "custom-value";
        }
    }
    private sealed class CustomValue;
    private enum HandlerEnum : long { Value = 2147483648L }
    private sealed class Repository() : BaseRepository("Data Source=:memory:;Pooling=False;", SQLiteFactory.Instance);
}
