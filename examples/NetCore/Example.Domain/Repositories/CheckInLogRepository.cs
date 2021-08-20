﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Example.Domain.Contracts;
using Example.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Sean.Core.DbRepository;
using Sean.Core.DbRepository.Dapper.Extensions;
using Sean.Core.DbRepository.Dapper.Impls;
using Sean.Utility.Contracts;
using Sean.Utility.Format;

namespace Example.Domain.Repositories
{
    public class CheckInLogRepository : BaseRepository<CheckInLogEntity>, ICheckInLogRepository
    {
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
            _logger.LogInfo($"执行了SQL: {sql}{Environment.NewLine}参数：{JsonHelper.SerializeFormatIndented(param)}");
        }

        public override string TableName()
        {
            if (SubTableDate != DateTime.MinValue)
            {
                // 自定义表名规则：按时间分表
                var tableName = $"{MainTableName}_{SubTableDate.ToString("yyyyMM")}";
                CreateTableIfNotExist(tableName, true);
                return tableName;
            }

            return base.TableName();
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
                .OrderByField(OrderByType.Asc, entity => entity.UserId)
                .OrderByField(OrderByType.Desc, entity => entity.CreateTime)
                .SetParameter(new { UserId = userId, CheckInType = new[] { 1, 2 } });
            #endregion

            #region 返回结果示例
            //// 返回结果示例1：
            //return await ExecuteAsync(async connection => await connection.QueryAsync<CheckInLogEntity>(this, sqlFactory), false);

            // 返回结果示例2：
            return await QueryAsync(sqlFactory, false);
            #endregion
        }
    }
}
