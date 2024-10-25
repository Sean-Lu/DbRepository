using Example.Dapper.Application.Contracts;
using Example.Dapper.ConsoleApp.Contracts;
using Sean.Utility.Contracts;

namespace Example.Dapper.ConsoleApp.Impls
{
    public class DBTest : ISimpleDo
    {
        private readonly ILogger _logger;
        //private readonly ITestSimpleService _testSimpleService;
        private readonly ITestService _testService;

        public DBTest(
            ILogger<DBTest> logger,
            //ITestSimpleService testSimpleService,
            ITestService testService
        )
        {
            _logger = logger;
            //_testSimpleService = testSimpleService;
            _testService = testService;
        }

        public void Execute()
        {
            //_testSimpleService.TestCRUDAsync().Wait();

            _testService.TestCRUDAsync().Wait();
            //_testService.TestCRUDWithTransactionAsync().Wait();
        }
    }
}