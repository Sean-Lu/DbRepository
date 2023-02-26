using Example.Application.Contracts;
using Microsoft.Extensions.Configuration;
using Sean.Utility.Contracts;

namespace Example.NetCore.ConsoleApp.Impls
{
    public class DBTest : ISimpleDo
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly ITestService _testService;
        private readonly ICheckInLogService _checkInLogService;

        public DBTest(
            ISimpleLogger<DBTest> logger,
            IConfiguration configuration,
            ITestService testService,
            ICheckInLogService checkInLogService)
        {
            _logger = logger;
            _configuration = configuration;
            _testService = testService;
            _checkInLogService = checkInLogService;
        }

        public void Execute()
        {
            _testService.TestCRUDAsync().Wait();
            //_testService.TestCRUDWithTransactionAsync().Wait();
        }
    }
}