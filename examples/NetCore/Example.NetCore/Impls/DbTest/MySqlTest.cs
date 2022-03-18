using System;
using Example.Application.Contracts;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Sean.Utility.Contracts;

namespace Example.NetCore.Impls.DbTest
{
    /// <summary>
    /// MySql
    /// </summary>
    public class MySqlTest : ISimpleDo
    {
        private readonly ILogger _logger;
        private readonly ICheckInLogService _checkInLogService;

        public MySqlTest(
            IConfiguration configuration,
            ISimpleLogger<MySqlTest> logger,
            ICheckInLogService checkInLogService)
        {
            _logger = logger;
            _checkInLogService = checkInLogService;
        }

        public void Execute()
        {
            var list = _checkInLogService.SearchAsync(100000, 1, 3).Result;
            _logger.LogInfo($"从数据库中查询到数据：{Environment.NewLine}{JsonConvert.SerializeObject(list,Formatting.Indented)}");
        }
    }
}