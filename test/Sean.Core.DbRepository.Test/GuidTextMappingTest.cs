using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository.Test;

[TestClass]
public class GuidTextMappingTest
{
    [TestMethod]
    [DataRow("D")]
    [DataRow("N")]
    [DataRow("B")]
    public async Task Mapping_ReadsGuidTextWithoutChangingNullDefaults(string format)
    {
        var expected = Guid.Parse("6a7010f5-2d91-4a04-b2d1-47ead5908d42");
        using var table = new DataTable();
        table.Columns.Add("Value", typeof(string));
        table.Columns.Add("OptionalValue", typeof(string));
        table.Rows.Add(expected.ToString(format), expected.ToString(format));
        table.Rows.Add(DBNull.Value, DBNull.Value);
        Assert.AreEqual(expected, table.Rows[0].ToEntity<Guid>());
        Assert.AreEqual(expected, table.Rows[0].ToEntity<Guid?>());
        Assert.AreEqual(Guid.Empty, table.Rows[1].ToEntity<Guid>());
        Assert.IsNull(table.Rows[1].ToEntity<Guid?>());
        var tuple = table.Rows[0].ToEntity<Tuple<Guid, Guid?>>();
        Assert.AreEqual(expected, tuple.Item1);
        Assert.AreEqual(expected, tuple.Item2);
        var row = table.Rows[0].ToEntity<GuidEntity>();
        Assert.AreEqual(expected, row.Value);
        Assert.AreEqual(expected, row.OptionalValue);
        var models = table.ToList<GuidEntity>();
        using var reader = table.CreateDataReader();
        var synchronous = reader.GetList<GuidEntity>();
        using var asyncReader = table.CreateDataReader();
        var asynchronous = await asyncReader.GetListAsync<GuidEntity>();
        foreach (var list in new[] { models, synchronous, asynchronous })
        {
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(expected, list[0].Value);
            Assert.AreEqual(expected, list[0].OptionalValue);
            // DBNull 不应覆盖实体初始化值。
            Assert.AreEqual(GuidEntity.InitialValue, list[1].Value);
            Assert.IsNull(list[1].OptionalValue);
        }
    }

    [TestMethod]
    public void Mapping_KeepsInvalidTextAndBinaryConversionFailures()
    {
        using var table = new DataTable();
        table.Columns.Add("Value", typeof(object));
        foreach (var value in new object[] { "not-a-guid", new byte[16] })
        {
            table.Rows.Clear();
            table.Rows.Add(value);
            // 不把无效值静默变成 Guid.Empty，也不擅自决定二进制 Guid 的字节序。
            Assert.Throws<InvalidCastException>(() => table.Rows[0].ToEntity<Guid>());
            Assert.Throws<InvalidCastException>(() => table.Rows[0].ToEntity<Guid?>());
        }
    }

    private sealed class GuidEntity
    {
        public static readonly Guid InitialValue = Guid.Parse("0fb090f3-25e0-4bfa-947e-e6017e47ecb4");
        public Guid Value { get; set; } = InitialValue;
        public Guid? OptionalValue { get; set; }
    }
}
