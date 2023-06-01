using Example.ADO.NETCore.Application.Contracts;
using Microsoft.Extensions.Configuration;
using Sean.Utility.Contracts;

namespace Example.ADO.NETCore.ConsoleApp.Impls
{
    public class DBTest : ISimpleDo
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly ITestService _testService;
        private readonly ITestSimpleService _testSimpleService;

        public DBTest(
            ISimpleLogger<DBTest> logger,
            IConfiguration configuration,
            ITestService testService,
            ITestSimpleService testSimpleService
            )
        {
            _logger = logger;
            _configuration = configuration;
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