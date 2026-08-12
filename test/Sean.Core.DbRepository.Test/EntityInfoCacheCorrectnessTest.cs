using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository.Test;

/// <summary>
/// 实体元数据与 XML 注释缓存的回归测试。
/// </summary>
[TestClass]
[DoNotParallelize]
public class EntityInfoCacheCorrectnessTest
{
    [TestInitialize]
    public void Initialize()
    {
        EntityInfoCache.Clear();
    }

    [TestCleanup]
    public void Cleanup()
    {
        EntityInfoCache.Clear();
    }

    [TestMethod]
    public async Task ConcurrentFirstAccess_PublishesTheSameCompleteEntityInfo()
    {
        var tasks = Enumerable.Range(0, 64)
            .Select(_ => Task.Run(() => typeof(ConcurrentCacheEntity).GetEntityInfo()))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.IsTrue(results.All(result => ReferenceEquals(results[0], result)));
        Assert.AreEqual(2, results[0].FieldInfos.Count);
        Assert.AreEqual(1, EntityInfoCache.Count());
    }

    [TestMethod]
    public void XmlDescriptions_AreLoadedOncePerAssemblyAndAttributeStillTakesPriority()
    {
        var xmlEntity = typeof(EntityInfo).GetEntityInfo();
        var secondEntity = typeof(EntityFieldInfo).GetEntityInfo();
        var attributeEntity = typeof(AttributeDescriptionEntity).GetEntityInfo();

        Assert.AreEqual("所有字段信息", xmlEntity.FieldInfos
            .Single(field => field.PropertyName == nameof(EntityInfo.FieldInfos)).FieldDescription);
        Assert.AreEqual("特性表说明", attributeEntity.TableDescription);
        Assert.AreEqual("特性字段说明", attributeEntity.FieldInfos.Single().FieldDescription);
        Assert.IsNotNull(secondEntity);
        Assert.AreEqual(1, GetXmlCacheCount());
    }

    [TestMethod]
    public void RemoveAndClear_PreserveRefreshAndCacheManagementSemantics()
    {
        var first = typeof(EntityInfo).GetEntityInfo();
        var removed = EntityInfoCache.Remove(typeof(EntityInfo));

        Assert.AreSame(first, removed);
        Assert.IsFalse(EntityInfoCache.ContainsKey(typeof(EntityInfo)));
        Assert.AreEqual(0, GetXmlCacheCount());

        var rebuilt = typeof(EntityInfo).GetEntityInfo();
        Assert.AreNotSame(first, rebuilt);
        Assert.AreEqual(1, GetXmlCacheCount());

        EntityInfoCache.Clear();
        Assert.AreEqual(0, EntityInfoCache.Count());
        Assert.AreEqual(0, GetXmlCacheCount());
    }

    [TestMethod]
    public void AnonymousTypes_RemainOutsideThePermanentCache()
    {
        var anonymous = new { Id = 1, Name = "匿名类型" };
        var first = anonymous.GetType().GetEntityInfo();
        var second = anonymous.GetType().GetEntityInfo();

        Assert.AreNotSame(first, second);
        Assert.IsFalse(EntityInfoCache.ContainsKey(anonymous.GetType()));
        CollectionAssert.AreEqual(new[] { "Id", "Name" },
            first.FieldInfos.Select(field => field.FieldName).ToArray());
    }

    private static int GetXmlCacheCount()
    {
        var field = typeof(EntityInfoCache).GetField("_xmlDescriptionCache",
            BindingFlags.Static | BindingFlags.NonPublic);
        var cache = (ICollection)field?.GetValue(null);
        return cache?.Count ?? -1;
    }

    private sealed class ConcurrentCacheEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    [System.ComponentModel.DescriptionAttribute("特性表说明")]
    private sealed class AttributeDescriptionEntity
    {
        [System.ComponentModel.DescriptionAttribute("特性字段说明")]
        public string Value { get; set; }
    }
}
