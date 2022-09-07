using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Example.Domain.Contracts;
using Example.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Sean.Core.DbRepository;
using Sean.Core.DbRepository.Dapper;
using Sean.Core.DbRepository.Dapper.Extensions;
using Sean.Core.DbRepository.Extensions;
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

        public override void OnSqlExecuted(SqlExecutedContext context)
        {
            _logger.LogInfo($"执行了SQL: {context.Sql}{Environment.NewLine}参数：{JsonConvert.SerializeObject(context.SqlParameter, Formatting.Indented)}");
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
            //return await DeleteAsync(this.CreateDeleteableBuilder()
            //    .WhereField(entity => entity.Id, SqlOperation.Equal)
            //    .SetParameter(new { Id = id })
            //    .Build()) > 0;

            return await DeleteAsync(entity => entity.Id == id) > 0;
        }

        public async Task<bool> UpdateAsync(long id, int checkInType)
        {
            //// 只更新部分字段（CheckInType），需要设置参数autoIncludeFields=false，否则会更新所有字段
            //return await UpdateAsync(this.CreateUpdateableBuilder(false)
            //    .IncludeFields(entity => entity.CheckInType)
            //    .WhereField(entity => entity.Id, SqlOperation.Equal)
            //    .SetParameter(new { Id = id, CheckInType = checkInType })
            //    .Build()) > 0;

            return await UpdateAsync(new CheckInLogEntity
            {
                CheckInType = checkInType
            }, entity => new { entity.CheckInType }, entity => entity.Id == id) > 0;
        }

        public async Task<IEnumerable<CheckInLogEntity>> SearchAsync(long userId, int pageIndex, int pageSize)
        {
            #region SqlBuilder 示例
            //// SqlBuilder 示例1：
            //var queryableSql = this.CreateQueryableBuilder(true)
            //    .Page(pageIndex, pageSize)
            //    .Where($"{nameof(CheckInLogEntity.UserId)} = @{nameof(CheckInLogEntity.UserId)} AND {nameof(CheckInLogEntity.CheckInType)} IN @{nameof(CheckInLogEntity.CheckInType)}")
            //    .OrderBy($"{nameof(CheckInLogEntity.UserId)} ASC, {nameof(CheckInLogEntity.CreateTime)} DESC")
            //    .SetParameter(new { UserId = userId, CheckInType = new[] { 1, 2 } })
            //    .Build();

            //// SqlBuilder 示例2：
            //var queryableSql = this.CreateQueryableBuilder(true)
            //    .Page(pageIndex, pageSize)
            //    .WhereField(entity => entity.UserId, SqlOperation.Equal)
            //    .WhereField(entity => nameof(CheckInLogEntity.CheckInType), SqlOperation.In)
            //    .OrderByField(OrderByType.Asc, nameof(CheckInLogEntity.UserId))
            //    .OrderByField(OrderByType.Desc, nameof(CheckInLogEntity.CreateTime))
            //    .SetParameter(new { UserId = userId, CheckInType = new[] { 1, 2 } })
            //    .Build();

            //// SqlBuilder 示例3：
            //var queryableSql = this.CreateQueryableBuilder(true)
            //    .Page(pageIndex, pageSize)
            //    .WhereField(entity => entity.UserId, SqlOperation.Equal)
            //    .WhereField(entity => entity.CheckInType, SqlOperation.In)
            //    .WhereField(entity => entity.CreateTime, SqlOperation.GreaterOrEqual, paramName: "StartTime")
            //    .WhereField(entity => entity.CreateTime, SqlOperation.Less, paramName: "EndTime")
            //    .OrderByField(OrderByType.Asc, entity => entity.UserId)
            //    .OrderByField(OrderByType.Desc, entity => entity.CreateTime)
            //    .SetParameter(new
            //    {
            //        UserId = userId,
            //        CheckInType = new[] { 1, 2 },
            //        StartTime = DateTime.Parse("2020-1-1 00:00:00"),
            //        EndTime = DateTime.Now
            //    })
            //    .Build();
            #endregion

            #region 返回结果示例
            //// 返回结果示例1：使用 SqlBuilder
            //return await ExecuteAsync(async connection => await connection.QueryAsync<CheckInLogEntity>(this, queryableSql), false);

            //// 返回结果示例2：使用 SqlBuilder
            //return await QueryAsync(queryableSql, false);

            // 返回结果示例3：使用 Expression 表达式树（推荐）
            int[] checkInTypes = { 1, 2 };
            var orderByCondition = OrderByConditionBuilder<CheckInLogEntity>.Build(OrderByType.Asc, entity => entity.UserId);
            orderByCondition.Next = OrderByConditionBuilder<CheckInLogEntity>.Build(OrderByType.Desc, entity => entity.CreateTime);
            return await QueryAsync(entity => entity.UserId == userId
                                              && checkInTypes.Contains(entity.CheckInType)
                                              && entity.CreateTime >= DateTime.Parse("2020-1-1 00:00:00")
                                              && entity.CreateTime < DateTime.Now, orderByCondition, pageIndex, pageSize, master: false);
            #endregion
        }

        public async Task<IEnumerable<CheckInLogEntity>> GetAllAsync()
        {
            return await QueryAsync(entity => true, master: false);// 从库查询表所有数据
        }
    }
}
