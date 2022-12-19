using System;
using System.Linq;
using Example.Domain.Entities;
using Example.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Sean.Core.DbRepository.Dapper;
using Sean.Utility.Contracts;

namespace Example.NetCore.Impls.DbTest
{
    /// <summary>
    /// SQLite
    /// </summary>
    public class SQLiteTest : BaseRepository<TestEntity>, ISimpleDo
    {
        private readonly ILogger _logger;

        public SQLiteTest(
            IConfiguration configuration,
            ISimpleLogger<TestRepository> logger
            ) : base(configuration, "test_SQLite")
        {
            _logger = logger;
        }

        public override string TableName()
        {
            var tableName = base.TableName();
            AutoCreateTable(tableName);
            return tableName;
        }

        public override string CreateTableSql(string tableName)
        {
            var sql = $@"CREATE TABLE `{tableName}` (
  `Id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
  `UserId` INTEGER NOT NULL,
  `UserName` TEXT,
  `Age` INTEGER NOT NULL DEFAULT 0,
  `Sex` INTEGER NOT NULL DEFAULT 0,
  `PhoneNumber` TEXT,
  `Email` TEXT,
  `IsVip` INTEGER NOT NULL DEFAULT 0,
  `IsBlack` INTEGER NOT NULL DEFAULT 0,
  `Country` INTEGER NOT NULL DEFAULT 0,
  `AccountBalance` decimal(18,2) NOT NULL DEFAULT 0,
  `AccountBalance2` decimal(18,2) NOT NULL DEFAULT 0,
  `Status` INTEGER NOT NULL DEFAULT 0,
  `Remark` TEXT,
  `CreateTime` TEXT NOT NULL,
  `UpdateTime` TEXT
);";
            return sql;
        }

        public void Execute()
        {
            TestCRUDWithTransaction();
        }

        private void TestCRUDWithTransaction()
        {
            var result = ExecuteAutoTransaction(trans =>
            {
                var entity = new TestEntity
                {
                    UserId = 10001,
                    UserName = "Test01",
                    Age = 18,
                    IsVip = true,
                    AccountBalance = 99.95M,
                    Remark = "Test",
                    CreateTime = DateTime.Now,
                };
                var addResult = AddAsync(entity, true, transaction: trans).Result;
                _logger.LogDebug($"######Add result: {addResult}");

                var queryResult = QueryAsync(entity => true, null, 1, 3).Result?.ToList();
                _logger.LogDebug($"######Query result: {JsonConvert.SerializeObject(queryResult, Formatting.Indented)}");

                var deleteResult = DeleteAsync(entity, trans).Result;
                _logger.LogDebug($"######Delete result: {deleteResult}");

                return true;
            });
        }
    }
}
