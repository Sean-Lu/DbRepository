using Example.Application.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Example.Infrastructure;

namespace Sean.Core.DbRepository.Test
{
    public abstract class TestBase
    {
        static TestBase()
        {
            DIManager.ConfigureServices(services =>
            {
                services.AddApplicationDI();
            });
        }

        protected void AssertSqlParameters(IDictionary<string, object> expectedDictionary, IDictionary<string, object> actualDictionary)
        {
            Assert.AreEqual(expectedDictionary.Count, actualDictionary.Count);
            foreach (var key in expectedDictionary.Keys)
            {
                Assert.IsTrue(actualDictionary.ContainsKey(key), $"The <{nameof(actualDictionary)}> does not contain key <{key}>.");
                Assert.IsTrue(expectedDictionary[key].Equals(actualDictionary[key]), $"Dictionary key: <{key}>, the expected value is <{expectedDictionary[key]}>, the actual value is <{actualDictionary[key]}>.");
            }
        }
    }
}
