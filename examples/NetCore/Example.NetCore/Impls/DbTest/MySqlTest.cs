using System;
using Example.Application.Contracts;
using Example.Application.Dtos;
using Example.Domain.Entities;
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
        private readonly ITestService _testService;
        private readonly ICheckInLogService _checkInLogService;

        public MySqlTest(
            IConfiguration configuration,
            ISimpleLogger<MySqlTest> logger,
            ITestService testService,
            ICheckInLogService checkInLogService)
        {
            _logger = logger;
            _testService = testService;
            _checkInLogService = checkInLogService;
        }

        public void Execute()
        {
            TestSearch();
        }

        private void TestAdd()
        {
            var testAddResult = _testService.AddAsync(new TestDto
            {
                Id = 2,
                UserId = 10000,
                UserName = "TestName",
                Country = CountryType.China,
                IsVip = true,
                AccountBalance = 99.92M,
                Remark = "Test12'3",
                CreateTime = DateTime.Now
            }).Result;
            var testDto = _testService.GetByIdAsync(2).Result;
        }

        private void TestSearch()
        {
            var list = _checkInLogService.SearchAsync(100000, 1, 3).Result;
            _logger.LogInfo($"从数据库中查询到数据：{Environment.NewLine}{JsonConvert.SerializeObject(list, Formatting.Indented)}");
        }

        private void TestAutoTransaction()
        {
            var test1 = _testService.ExecuteAutoTransactionTest().Result;
        }
    }
}