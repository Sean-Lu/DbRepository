using System;
using System.Threading.Tasks;
using Dapper;
using Example.Dapper.Domain.Contracts;
using Example.Dapper.Model.Entities;
using Newtonsoft.Json;
using Sean.Core.DbRepository;
using Sean.Core.DbRepository.Dapper;
using Sean.Core.DbRepository.Extensions;
using Sean.Utility.Contracts;

namespace Example.Dapper.Domain.Repositories
{
    //public class CheckInLogRepository : EntityBaseRepository<CheckInLogEntity>, ICheckInLogRepository// Using ADO.NET
    public class CheckInLogRepository : BaseRepository<CheckInLogEntity>, ICheckInLogRepository// Using Dapper
    {
        public DateTime? SubTableDate { get; set; }

        private readonly ILogger _logger;

        public CheckInLogRepository(
            ILogger<CheckInLogRepository> logger
            ) : base()
        {
            _logger = logger;
        }

        protected override void OnSqlExecuting(SqlExecutingContext context)
        {
            base.OnSqlExecuting(context);

            //_logger.LogInfo($"SQL准备执行: {context.Sql}{Environment.NewLine}参数：{JsonConvert.SerializeObject(context.SqlParameter, Formatting.Indented)}");
            //context.Handled = true;
        }

        protected override void OnSqlExecuted(SqlExecutedContext context)
        {
            base.OnSqlExecuted(context);

            _logger.LogInfo($"SQL已经执行: {context.Sql}{Environment.NewLine}参数：{JsonConvert.SerializeObject(context.SqlParameter, Formatting.Indented)}");
            context.Handled = true;
        }

        public override string TableName()
        {
            var tableName = SubTableDate.HasValue
                ? $"{MainTableName}_{SubTableDate.Value:yyyyMM}"// 自定义表名规则：按时间分表
                : base.TableName();
            AutoCreateTable(tableName);// 自动创建表（如果表不存在）
            return tableName;
        }

        public override string CreateTableSql(string tableName)
        {
            return $@"CREATE TABLE `{tableName}` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '自增主键',
  `UserId` bigint(20) NOT NULL COMMENT '用户ID',
  `CheckInType` int(2) NOT NULL COMMENT '签到类型',
  `CreateTime` datetime NOT NULL COMMENT '创建时间',
  `IP` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL COMMENT 'IP地址',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci ROW_FORMAT=DYNAMIC COMMENT='签到明细日志表';";
        }

        private async Task TestDapperQueryAsync()
        {
            var sqlCommand = this.CreateQueryableBuilder(true)
                .Where(entity => entity.UserId == 100000)
                .Build();

            #region Dapper > QueryFirst\QueryFirstOrDefault
            // 没有结果返回时，QueryFirst 方法会报错（System.InvalidOperationException:“序列不包含任何元素”），QueryFirstOrDefault 方法会返回默认值
            // 有多个结果返回时，2个方法都会返回第一个结果
            var get1 = await ExecuteAsync(async c => await c.QueryFirstAsync<CheckInLogEntity>(sqlCommand.Sql, sqlCommand.Parameter));
            var get2 = await ExecuteAsync(async c => await c.QueryFirstOrDefaultAsync<CheckInLogEntity>(sqlCommand.Sql, sqlCommand.Parameter));
            #endregion

            #region Dapper > QuerySingle\QuerySingleOrDefault
            // 没有结果返回时，QuerySingle 方法会报错（System.InvalidOperationException:“序列不包含任何元素”），QuerySingleOrDefault 方法会返回默认值
            // 有多个结果返回时，2个方法都会报错（System.InvalidOperationException:“序列包含一个以上的元素”）
            var get3 = await ExecuteAsync(async c => await c.QuerySingleAsync<CheckInLogEntity>(sqlCommand.Sql, sqlCommand.Parameter));
            var get4 = await ExecuteAsync(async c => await c.QuerySingleOrDefaultAsync<CheckInLogEntity>(sqlCommand.Sql, sqlCommand.Parameter));
            #endregion
        }
    }
}
