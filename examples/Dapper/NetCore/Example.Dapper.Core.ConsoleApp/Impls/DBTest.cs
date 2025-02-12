﻿using Example.Dapper.Core.Application.Contracts;
using Example.Dapper.Core.ConsoleApp.Contracts;
using Microsoft.Extensions.Configuration;
using Sean.Utility.Contracts;

namespace Example.Dapper.Core.ConsoleApp.Impls
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