using System;
using System.Globalization;
using System.Reflection;
using Example.Application.Contracts;
using Sean.Utility.Contracts;

namespace Example.NetFramework.Impls.DbTest
{
    /// <summary>
    /// SQLite
    /// </summary>
    public class SQLiteTest : ISimpleDo
    {
        private readonly ILogger _logger;
        private readonly ITestService _testService;

        public SQLiteTest(
            ILogger<SQLiteTest> logger,
            ITestService testService
        )
        {
            _logger = logger;
            _testService = testService;
        }

        public void Execute()
        {
            _testService.TestCRUDWithTransactionAsync().Wait();
        }
    }
}
