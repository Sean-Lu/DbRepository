using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sean.Core.DbRepository.Util;

namespace Sean.Core.DbRepository.Test;

[TestClass]
public class EnumParameterCorrectnessTest
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task WideEnumParameters_PreserveValuesOutsideInt32(bool asynchronous)
    {
        var repository = new Repository();
        foreach (var value in new object[] { SignedWide.Positive, SignedWide.Negative, UnsignedWide.Value, UnsignedLong.Value })
        {
            var expected = Convert.ToInt64(value);
            var result = asynchronous
                ? await repository.ExecuteScalarAsync<long>("SELECT @Value", new { Value = value })
                : repository.ExecuteScalar<long>("SELECT @Value", new { Value = value });
            Assert.AreEqual(expected, result, value.GetType().Name);
        }
    }

    [TestMethod]
    public void EnumParameters_PreserveInt32BindingAndUnsignedMaximum()
    {
        foreach (var value in new object[] { Small.Value, SignedWide.Small, UnsignedWide.Small, UnsignedLong.Small })
        {
            var parameters = SqlParameterUtil.ConvertToDbParameters(DatabaseType.SQLite,
                new Dictionary<string, object> { ["Value"] = value }, () => new SQLiteParameter());
            Assert.AreEqual(DbType.Int32, parameters[0].DbType);
            Assert.AreEqual(7, parameters[0].Value);
        }
        // SQLite 不支持完整的 UInt64 存储范围，此处只验证 ORM 不截断参数，不声明驱动可执行。
        var maximum = SqlParameterUtil.ConvertToDbParameters(DatabaseType.SQLite,
            new Dictionary<string, object> { ["Value"] = UnsignedLong.Maximum }, () => new SQLiteParameter())[0];
        Assert.AreEqual(DbType.UInt64, maximum.DbType);
        Assert.AreEqual(ulong.MaxValue, maximum.Value);
    }

    private enum Small : byte { Value = 7 }
    private enum SignedWide : long { Small = 7, Positive = 2147483648L, Negative = -2147483649L }
    private enum UnsignedWide : uint { Small = 7, Value = 4294967295U }
    private enum UnsignedLong : ulong { Small = 7, Value = 4294967296UL, Maximum = ulong.MaxValue }
    private sealed class Repository() : BaseRepository("Data Source=:memory:;Pooling=False;", SQLiteFactory.Instance);
}
