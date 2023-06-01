using Example.Dapper.Application.Contracts;
using Sean.Utility.Contracts;

namespace Example.Dapper.ConsoleApp.Impls
{
    public class DBTest : ISimpleDo
    {
        private readonly ILogger _logger;
        private readonly ITestService _testService;
        private readonly ITestSimpleService _testSimpleService;

        public DBTest(
            ILogger<DBTest> logger,
            ITestService testService,
            ITestSimpleService testSimpleService
            )
        {
            _logger = logger;
            _testService = testService;
            _testSimpleService = testSimpleService;
        }

        public void Execute()
        {
            _testService.TestCRUDAsync().Wait();
            //_testService.TestCRUDWithTransactionAsync().Wait();

            //_testSimpleService.TestCRUDAsync().Wait();
        }
    }
}
