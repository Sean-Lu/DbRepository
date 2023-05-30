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

        public DBTest(
            ISimpleLogger<DBTest> logger,
            IConfiguration configuration,
            ITestService testService
            )
        {
            _logger = logger;
            _configuration = configuration;
            _testService = testService;
        }

        public void Execute()
        {
            _testService.TestCRUDAsync().Wait();
            //_testService.TestCRUDWithTransactionAsync().Wait();
        }
    }
}