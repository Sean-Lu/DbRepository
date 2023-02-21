using System;
using Example.Application.Contracts;
using Newtonsoft.Json;
using Sean.Utility.Contracts;

namespace Example.NetFramework.Impls.DbTest
{
    /// <summary>
    /// MySql
    /// </summary>
    public class MySqlTest : ISimpleDo
    {
        private readonly ILogger _logger;
        private readonly ICheckInLogService _checkInLogService;

        public MySqlTest(
            ILogger<MySqlTest> logger,
            ICheckInLogService checkInLogService
            )
        {
            _logger = logger;
            _checkInLogService = checkInLogService;
        }

        public void Execute()
        {
            var list = _checkInLogService.SearchAsync(100010, 1, 3).Result;
            _logger.LogInfo($"查询结果：{Environment.NewLine}{JsonConvert.SerializeObject(list, Formatting.Indented)}");
        }
    }
}