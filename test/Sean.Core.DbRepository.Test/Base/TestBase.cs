using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sean.Core.DbRepository.Test
{
    public abstract class TestBase
    {
        protected void AssertSqlParameters(IDictionary<string, object> expectedDictionary, IDictionary<string, object> actualDictionary)
        {
            Assert.IsNotNull(actualDictionary);
            Assert.AreEqual(expectedDictionary.Count, actualDictionary.Count);
            foreach (var key in expectedDictionary.Keys)
            {
                Assert.IsTrue(actualDictionary.ContainsKey(key), $"The <{nameof(actualDictionary)}> does not contain key <{key}>.");
                Assert.IsTrue(Equals(expectedDictionary[key], actualDictionary[key]), $"Dictionary key: <{key}>, the expected value is <{expectedDictionary[key]}>, the actual value is <{actualDictionary[key]}>.");
            }
        }

        protected void AssertFields(List<string> expectedFields, List<string> actualFields)
        {
            Assert.AreEqual(expectedFields.Count, actualFields.Count);
            foreach (var field in expectedFields)
            {
                Assert.IsTrue(actualFields.Contains(field), $"The {nameof(actualFields)} does not contain <{field}>.");
            }
        }
    }
}