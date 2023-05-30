using Example.Dapper.Application.Contracts;
using Sean.Utility.Contracts;

namespace Example.Dapper.ConsoleApp.Impls
{
    public class DBTest : ISimpleDo
    {
        private readonly ILogger _logger;
        private readonly ITestService _testService;

        public DBTest(
            ILogger<DBTest> logger,
            ITestService testService
            )
        {
            _logger = logger;
            _testService = testService;
        }

        public void Execute()
        {
            _testService.TestCRUDAsync().Wait();
            //_testService.TestCRUDWithTransactionAsync().Wait();
        }
    }
}
