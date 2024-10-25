using Example.ADO.NETCore.Application.Contracts;
using Example.ADO.NETCore.ConsoleApp.Contracts;
using Microsoft.Extensions.Configuration;
using Sean.Utility.Contracts;

namespace Example.ADO.NETCore.ConsoleApp.Impls
{
    public class DBTest : ISimpleDo
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        //private readonly ITestSimpleService _testSimpleService;
        private readonly ITestService _testService;

        public DBTest(
            ISimpleLogger<DBTest> logger,
            IConfiguration configuration,
            //ITestSimpleService testSimpleService,
            ITestService testService
        )
        {
            _logger = logger;
            _configuration = configuration;
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