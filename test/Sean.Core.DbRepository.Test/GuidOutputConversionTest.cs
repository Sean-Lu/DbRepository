using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sean.Utility.Format;

namespace Sean.Core.DbRepository.Test;

[TestClass]
public class GuidOutputConversionTest
{
    [TestMethod]
    [DataRow(nameof(Target.Value))]
    [DataRow(nameof(Target.OptionalValue))]
    public void Output_ParsesGuidTextAndPreservesFailureAndNullBehavior(string propertyName)
    {
        var expected = Guid.Parse("6a7010f5-2d91-4a04-b2d1-47ead5908d42");
        var target = new Target();
        var property = typeof(Target).GetProperty(propertyName);
        var options = new OutputParameterOptions<Target> { OutputTarget = target, OutputPropertyInfo = property };
        var calls = 0;
        options.ExecuteOutput(name =>
        {
            calls++;
            Assert.AreEqual(propertyName, name);
            return expected.ToString("D");
        });
        Assert.AreEqual(1, calls);
        Assert.AreEqual(expected, property.GetValue(target));
        foreach (var invalid in new object[] { "invalid", new byte[16] })
        {
            Assert.Throws<InvalidCastException>(() => options.ExecuteOutput(_ => invalid));
            Assert.AreEqual(expected, property.GetValue(target));
        }
        var failure = new InvalidOperationException("模拟驱动取值失败");
        Assert.AreSame(failure, Assert.Throws<InvalidOperationException>(() => options.ExecuteOutput(_ => throw failure)));
        Assert.AreEqual(expected, property.GetValue(target));
        // 空值继续用原转换器的规则，不统一成映射实体时的 DBNull 跳过语义。
        foreach (var empty in new object[] { null, DBNull.Value })
        {
            options.ExecuteOutput(_ => empty);
            Assert.AreEqual(ObjectConvert.ChangeType(empty, property.PropertyType), property.GetValue(target));
        }
    }

    private sealed class Target
    {
        public Guid Value { get; set; }
        public Guid? OptionalValue { get; set; }
    }
}
