using Example.Dapper.Application.Contracts;
using Sean.Utility.Contracts;

namespace Example.Dapper.ConsoleApp.Impls
{
    public class DBTest : ISimpleDo
    {
        private readonly ILogger _logger;
        private readonly ITestService _testService;
        private readonly ICheckInLogService _checkInLogService;

        public DBTest(
            ILogger<DBTest> logger,
            ITestService testService,
            ICheckInLogService checkInLogService
        )
        {
            _logger = logger;
            _testService = testService;
            _checkInLogService = checkInLogService;
        }

        public void Execute()
        {
            //var list = _checkInLogService.SearchAsync(100010, 1, 3).Result;
            //_logger.LogInfo($"查询结果：{Environment.NewLine}{JsonConvert.SerializeObject(list, Formatting.Indented)}");

            _testService.TestCRUDAsync().Wait();
            //_testService.TestCRUDWithTransactionAsync().Wait();
        }
    }
}
