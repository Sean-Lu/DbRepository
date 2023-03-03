using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Example.ADO.NETCore.Domain.Contracts;
using Example.ADO.NETCore.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Sean.Core.DbRepository;
using Sean.Core.DbRepository.Extensions;
using Sean.Utility.Contracts;

namespace Example.ADO.NETCore.Domain.Repositories
{
    public class TestRepository : EntityBaseRepository<TestEntity>, ITestRepository// Using ADO.NET
    //public class TestRepository : BaseRepository<TestEntity>, ITestRepository// Using Dapper
    {
        private readonly ILogger _logger;

        public TestRepository(
            IConfiguration configuration,
            ISimpleLogger<TestRepository> logger
            ) : base(configuration)// MySQL
            //) : base(configuration, "test_SQLite")// SQLite
            //) : base(configuration, "test_SqlServer")// SqlServer
            //) : base(configuration, "test_SqlServerCe")// SqlServerCe
            //) : base(configuration, "test_Oracle")// Oracle
            //) : base(configuration, "test_PostgreSql")// PostgreSql
            //) : base(configuration, "test_Firebird")// Firebird
            //) : base(configuration, "test_Informix")// Informix
            //) : base(configuration, "test_DB2")// DB2
        {
            _logger = logger;
        }

        protected override void OnSqlExecuting(SqlExecutingContext context)
        {
            base.OnSqlExecuting(context);

            //_logger.LogInfo($"SQL准备执行: {context.Sql}{Environment.NewLine}参数：{JsonConvert.SerializeObject(context.SqlParameter, Formatting.Indented, new DbParameterCollectionConverter())}");
        }

        protected override void OnSqlExecuted(SqlExecutedContext context)
        {
            base.OnSqlExecuted(context);

            //_logger.LogInfo($"SQL已经执行: {context.Sql}{Environment.NewLine}参数：{JsonConvert.SerializeObject(context.SqlParameter, Formatting.Indented, new DbParameterCollectionConverter())}");
        }

        public override string TableName()
        {
            var tableName = base.TableName();
            AutoCreateTable(tableName);// 自动创建表（如果表不存在）
            return tableName;
        }

        public override string CreateTableSql(string tableName)
        {
            if (DbType == DatabaseType.MySql)
            {
                return $@"CREATE TABLE `{tableName}` (
  `Id` bigint NOT NULL AUTO_INCREMENT COMMENT '自增主键',
  `UserId` bigint NOT NULL COMMENT '用户id',
  `UserName` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL COMMENT '用户名称',
  `Age` int NOT NULL DEFAULT '0' COMMENT '年龄',
  `Sex` tinyint NOT NULL DEFAULT '0' COMMENT '性别',
  `PhoneNumber` varchar(50) DEFAULT NULL COMMENT '电话号码',
  `Email` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL COMMENT '邮箱',
  `IsVip` bit(1) NOT NULL DEFAULT b'0' COMMENT '是否是VIP用户',
  `IsBlack` bit(1) NOT NULL DEFAULT b'0' COMMENT '是否是黑名单用户',
  `Country` int NOT NULL DEFAULT '0' COMMENT '国家',
  `AccountBalance` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '账户余额',
  `AccountBalance2` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '账户余额',
  `Status` int NOT NULL DEFAULT '0' COMMENT '状态',
  `Remark` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL COMMENT '备注',
  `CreateTime` datetime NOT NULL COMMENT '创建时间',
  `UpdateTime` datetime ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='测试表（仅供测试使用）';";
            }

            if (DbType == DatabaseType.SQLite)
            {
                return $@"CREATE TABLE `{tableName}` (
  `Id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, -- COMMENT '自增主键',
  `UserId` INTEGER NOT NULL, -- COMMENT '用户id',
  `UserName` TEXT DEFAULT NULL, -- COMMENT '用户名称',
  `Age` INTEGER NOT NULL DEFAULT 0, -- COMMENT '年龄',
  `Sex` INTEGER NOT NULL DEFAULT 0, -- COMMENT '性别',
  `PhoneNumber` TEXT DEFAULT NULL, -- COMMENT '电话号码',
  `Email` TEXT DEFAULT NULL, -- COMMENT '邮箱',
  `IsVip` INTEGER NOT NULL DEFAULT 0, -- COMMENT '是否是VIP用户',
  `IsBlack` INTEGER NOT NULL DEFAULT 0, -- COMMENT '是否是黑名单用户',
  `Country` INTEGER NOT NULL DEFAULT 0, -- COMMENT '国家',
  `AccountBalance` decimal(18,2) NOT NULL DEFAULT 0, -- COMMENT '账户余额',
  `AccountBalance2` decimal(18,2) NOT NULL DEFAULT 0, -- COMMENT '账户余额',
  `Status` INTEGER NOT NULL DEFAULT 0, -- COMMENT '状态',
  `Remark` TEXT DEFAULT NULL, -- COMMENT '备注',
  `CreateTime` TEXT NOT NULL, -- COMMENT '创建时间',
  `UpdateTime` TEXT -- COMMENT '更新时间'
);";
            }

            throw new NotImplementedException();
        }

        public async Task<bool> TestCRUDAsync(IDbTransaction trans = null)
        {
            var testModel = new TestEntity
            {
                UserId = 10001,
                UserName = "Test01",
                Age = 18,
                IsVip = true,
                AccountBalance = 99.95M,
                Remark = "Test",
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            };
            var addResult = await AddAsync(testModel, true, transaction: trans);
            _logger.LogDebug($"######Add result: {addResult}");
            if (!addResult)
            {
                return false;
            }

            testModel.AccountBalance = 1000M;
            testModel.Age = 20;
            var updateResult = await UpdateAsync(testModel, entity => new { entity.AccountBalance, entity.Age }, transaction: trans) > 0;
            _logger.LogDebug($"######Update result: {updateResult}");
            if (!updateResult)
            {
                return false;
            }

            var incrResult = await IncrementAsync(1, entity => entity.AccountBalance, entity => entity.Id == testModel.Id, transaction: trans);
            _logger.LogDebug($"######Increment result: {incrResult}");
            if (!incrResult)
            {
                return false;
            }

            var orderBy = OrderByConditionBuilder<TestEntity>.Build(OrderByType.Asc, entity => entity.UserId);
            orderBy.Next = OrderByConditionBuilder<TestEntity>.Build(OrderByType.Asc, entity => entity.Id);
            var queryResult = (await QueryAsync(entity => true, orderBy, 1, 3))?.ToList();
            _logger.LogDebug($"######Query result: {JsonConvert.SerializeObject(queryResult, Formatting.Indented)}");

            var getResult = await GetAsync(entity => entity.UserId == 10001);
            _logger.LogDebug($"######Get result: {JsonConvert.SerializeObject(getResult, Formatting.Indented)}");

            var sqlCommand = this.CreateQueryableBuilder(true)
                .Where(entity => entity.Age >= 18 && entity.IsVip)
                .Page(1, 3)
                .Build();
            var executeDataTableResult = await ExecuteDataTableAsync(sqlCommand);
            _logger.LogDebug($"######ExecuteDataTable result: {JsonConvert.SerializeObject(executeDataTableResult, Formatting.Indented)}");

            var deleteResult = await DeleteAsync(testModel, transaction: trans);
            _logger.LogDebug($"######Delete result: {deleteResult}");
            if (!deleteResult)
            {
                return false;
            }

            return true;
        }

        public async Task<bool> TestCRUDWithTransactionAsync()
        {
            using (var trans = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                if (Transaction.Current != null)
                {
                    Transaction.Current.TransactionCompleted += (sender, args) =>
                    {
                        _logger.LogDebug("###################环境事务执行结束");
                    };
                }

                if (!await TestCRUDAsync())
                {
                    return false;
                }

                trans.Complete();
            }

            return true;
        }
    }
}
