using System;
using System.Collections.Generic;
using Dapper;
using Example.Application.Contracts;
using Example.Domain.Entities;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Sean.Core.DbRepository;
using Sean.Core.DbRepository.Dapper;
using Sean.Core.DbRepository.Dapper.Impls;
using Sean.Core.DbRepository.Factory;
using Sean.Utility.Contracts;
using Sean.Utility.Format;

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
            _logger.LogInfo($"从数据库中查询到数据：{Environment.NewLine}{JsonHelper.SerializeFormatIndented(list)}");
        }
    }
}