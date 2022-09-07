using System;
using Example.Domain.Contracts;
using Example.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Sean.Core.DbRepository;
using Sean.Core.DbRepository.Dapper;
using Sean.Utility.Contracts;

namespace Example.Domain.Repositories
{
    public class TestRepository : BaseRepository<TestEntity>, ITestRepository
    {
        private readonly ILogger _logger;

        public TestRepository(
            IConfiguration configuration,
            ISimpleLogger<TestRepository> logger) : base(configuration)
        {
            _logger = logger;
        }

        public override void OnSqlExecuted(SqlExecutedContext context)
        {
            _logger.LogInfo($"执行了SQL: {context.Sql}{Environment.NewLine}参数：{JsonConvert.SerializeObject(context.SqlParameter, Formatting.Indented)}");
        }

        public override string TableName()
        {
            var tableName = base.TableName();
            CreateTableIfNotExist(tableName, true);// 自动创建表（如果表不存在）
            return tableName;
        }

        public override string CreateTableSql(string tableName)
        {
            var sql = $@"CREATE TABLE `{tableName}` (
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
  `UpdateTime` datetime NOT NULL ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='测试表（仅供测试使用）';";
            return sql;
        }
    }
}
