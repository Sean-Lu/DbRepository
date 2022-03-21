using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Example.Domain.Contracts;
using Example.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Sean.Core.DbRepository;
using Sean.Utility.Contracts;

namespace Example.Domain.Repositories
{
    public class CheckInLogRepository : BaseRepository<CheckInLogEntity>, ICheckInLogRepository
    {
        /// <summary>
        /// 用于分表
        /// </summary>
        public DateTime SubTableDate { get; set; }

        private readonly ILogger _logger;

        public CheckInLogRepository(
            IConfiguration configuration,
            ISimpleLogger<CheckInLogRepository> logger) : base(configuration)
        {
            _logger = logger;
        }

        public override void OutputExecutedSql(string sql, object param)
        {
            _logger.LogInfo($"执行了SQL: {sql}{Environment.NewLine}参数：{JsonConvert.SerializeObject(param, Formatting.Indented)}");
        }

        public override string TableName()
        {
            var tableName = SubTableDate != DateTime.MinValue
                ? $"{MainTableName}_{SubTableDate:yyyyMM}"// 自定义表名规则：按时间分表
                : base.TableName();
            CreateTableIfNotExist(tableName, true);// 自动创建表（如果表不存在）
            return tableName;
        }

        public override string CreateTableSql(string tableName)
        {
            var sql = $@"CREATE TABLE `{tableName}` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '自增主键',
  `UserId` bigint(20) NOT NULL COMMENT '用户ID',
  `CheckInType` int(2) NOT NULL COMMENT '签到类型',
  `CreateTime` datetime NOT NULL COMMENT '创建时间',
  `IP` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL COMMENT 'IP地址',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci ROW_FORMAT=DYNAMIC COMMENT='签到明细日志表';";
            return sql;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            return await DeleteAsync(NewSqlFactory(false)
                .WhereField(entity => entity.Id, SqlOperation.Equal)
                .SetParameter(new { Id = id })) > 0;
        }

        public async Task<bool> UpdateAsync(long id, int checkInType)
        {
            // 只更新部分字段（CheckInType），需要设置参数autoIncludeFields=false，否则会更新所有字段
            return await UpdateAsync(NewSqlFactory(false)
                .IncludeFields(entity => entity.CheckInType)
                .WhereField(entity => entity.Id, SqlOperation.Equal)
                .SetParameter(new { Id = id, CheckInType = checkInType })) > 0;
        }

        public async Task<IEnumerable<CheckInLogEntity>> SearchAsync(long userId, int pageIndex, int pageSize)
        {
            #region SqlFactory示例
            //// SqlFactory示例1：
            //var sqlFactory = NewSqlFactory(true)
            //    .Page(pageIndex, pageSize)
            //    .Where($"{nameof(CheckInLogEntity.UserId)} = @{nameof(CheckInLogEntity.UserId)} AND {nameof(CheckInLogEntity.CheckInType)} IN @{nameof(CheckInLogEntity.CheckInType)}")
            //    .OrderBy($"{nameof(CheckInLogEntity.UserId)} ASC, {nameof(CheckInLogEntity.CreateTime)} DESC")
            //    .SetParameter(new { UserId = userId, CheckInType = new[] { 1, 2 } });

            //// SqlFactory示例2：
            //var sqlFactory = NewSqlFactory(true)
            //    .Page(pageIndex, pageSize)
            //    .WhereField(nameof(CheckInLogEntity.UserId), SqlOperation.Equal, WhereSqlKeyword.None)
            //    .WhereField(nameof(CheckInLogEntity.CheckInType), SqlOperation.In, WhereSqlKeyword.And)
            //    .OrderByField(OrderByType.Asc, nameof(CheckInLogEntity.UserId))
            //    .OrderByField(OrderByType.Desc, nameof(CheckInLogEntity.CreateTime))
            //    .SetParameter(new { UserId = userId, CheckInType = new[] { 1, 2 } });

            // SqlFactory示例3：
            var sqlFactory = NewSqlFactory(true)
                .Page(pageIndex, pageSize)
                .WhereField(entity => entity.UserId, SqlOperation.Equal, WhereSqlKeyword.None)
                .WhereField(entity => entity.CheckInType, SqlOperation.In, WhereSqlKeyword.And)
                .WhereField(entity => entity.CreateTime, SqlOperation.GreaterOrEqual, WhereSqlKeyword.And, paramName: "StartTime")
                .WhereField(entity => entity.CreateTime, SqlOperation.Less, WhereSqlKeyword.And, paramName: "EndTime")
                .OrderByField(OrderByType.Asc, entity => entity.UserId)
                .OrderByField(OrderByType.Desc, entity => entity.CreateTime)
                .SetParameter(new
                {
                    UserId = userId,
                    CheckInType = new[] { 1, 2 },
                    StartTime = DateTime.Parse("2020-1-1 00:00:00"),
                    EndTime = DateTime.Now
                });
            #endregion

            #region 返回结果示例
            //// 返回结果示例1：
            //return await ExecuteAsync(async connection => await connection.QueryAsync<CheckInLogEntity>(this, sqlFactory), false);

            // 返回结果示例2：
            return await QueryAsync(sqlFactory, false);
            #endregion
        }

        public async Task<IEnumerable<CheckInLogEntity>> GetAllAsync()
        {
            //return await QueryAsync(NewSqlFactory(false), false);

            return await QueryAsync(entity => true, master: false);
        }
    }
}
