using Example.Application.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sean.Core.Ioc;
using Sean.Utility.Extensions;
using Sean.Utility.Impls.Log;
using System.Collections.Generic;

namespace Sean.Core.DbRepository.Test
{
    public abstract class TestBase
    {
        static TestBase()
        {
            IocContainer.Instance.ConfigureServices(services =>
            {
                services.AddApplicationDI();
            });

            #region 配置Logger
            SimpleLocalLoggerBase.DateTimeFormat = time => time.ToLongDateTime();
            #endregion
        }

        protected void AssertSqlParameters(IDictionary<string, object> expectedParameters, IDictionary<string, object> actualParameters)
        {
            Assert.AreEqual(expectedParameters.Count, actualParameters.Count);
            foreach (var key in expectedParameters.Keys)
            {
                Assert.IsTrue(actualParameters.ContainsKey(key), $"The {nameof(actualParameters)} does not contain key <{key}>.");
                Assert.IsTrue(expectedParameters[key].Equals(actualParameters[key]), $"The expected value is <{expectedParameters[key]}>, the actual value is <{actualParameters[key]}>.");
            }
        }
    }
}
