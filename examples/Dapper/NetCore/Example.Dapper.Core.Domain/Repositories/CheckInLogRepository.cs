using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Example.Dapper.Core.Domain.Contracts;
using Example.Dapper.Core.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Sean.Core.DbRepository;
using Sean.Core.DbRepository.Dapper;
using Sean.Core.DbRepository.Extensions;
using Sean.Utility.Contracts;

namespace Example.Dapper.Core.Domain.Repositories
{
    //public class CheckInLogRepository : BaseRepository<CheckInLogEntity>, ICheckInLogRepository// Using ADO.NET
    public class CheckInLogRepository : DapperBaseRepository<CheckInLogEntity>, ICheckInLogRepository// Using Dapper
    {
        public DateTime? SubTableDate { get; set; }

        private readonly ILogger _logger;

        public CheckInLogRepository(
            IConfiguration configuration,
            ISimpleLogger<CheckInLogRepository> logger
            ) : base(configuration)
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

            _logger.LogInfo($"SQL已经执行({context.ExecutionElapsed}ms): {context.Sql}{Environment.NewLine}参数：{JsonConvert.SerializeObject(context.SqlParameter, Formatting.Indented)}");
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

        public async Task<IEnumerable<CheckInLogEntity>> SearchAsync(long userId, int pageIndex, int pageSize)
        {
            #region 自定义 SqlCommand 示例
            // SqlCommand 示例1：
            var sql = this.CreateQueryableBuilder(true)
                .Page(pageIndex, pageSize)
                .Where($"{nameof(CheckInLogEntity.UserId)} = @{nameof(CheckInLogEntity.UserId)} AND {nameof(CheckInLogEntity.CheckInType)} IN @{nameof(CheckInLogEntity.CheckInType)}")
                .OrderBy($"{nameof(CheckInLogEntity.UserId)} ASC, {nameof(CheckInLogEntity.CreateTime)} DESC")
                .SetParameter(new { UserId = userId, CheckInType = new[] { 1, 2 } })
                .Build();
            sql.Master = false;// 查询结果来自从库

            // SqlCommand 示例2：
            var sql2 = this.CreateQueryableBuilder(true)
                .Page(pageIndex, pageSize)
                .WhereField(entity => entity.UserId, SqlOperation.Equal)
                .WhereField(entity => nameof(CheckInLogEntity.CheckInType), SqlOperation.In)
                .OrderBy(OrderByType.Asc, nameof(CheckInLogEntity.UserId))
                .OrderBy(OrderByType.Desc, nameof(CheckInLogEntity.CreateTime))
                .SetParameter(new { UserId = userId, CheckInType = new[] { 1, 2 } })
                .Build();
            sql2.Master = false;// 查询结果来自从库

            // SqlCommand 示例3：
            var sql3 = this.CreateQueryableBuilder(true)
                .Page(pageIndex, pageSize)
                .WhereField(entity => entity.UserId, SqlOperation.Equal)
                .WhereField(entity => entity.CheckInType, SqlOperation.In)
                .WhereField(entity => entity.CreateTime, SqlOperation.GreaterOrEqual, paramName: "StartTime")
                .WhereField(entity => entity.CreateTime, SqlOperation.Less, paramName: "EndTime")
                .OrderBy(OrderByType.Asc, entity => entity.UserId)
                .OrderBy(OrderByType.Desc, entity => entity.CreateTime)
                .SetParameter(new
                {
                    UserId = userId,
                    CheckInType = new[] { 1, 2 },
                    StartTime = DateTime.Parse("2020-1-1 00:00:00"),
                    EndTime = DateTime.Now
                })
                .Build();
            sql3.Master = false;// 查询结果来自从库
            #endregion

            #region 返回结果示例
            //// 返回结果示例1：使用自定义 SqlCommand （复杂SQL）
            //return await QueryAsync<CheckInLogEntity>(sql);

            // 返回结果示例2：使用 Expression 表达式树（推荐）
            int[] checkInTypes = { 1, 2 };
            var orderBy = OrderByConditionBuilder<CheckInLogEntity>.Build(OrderByType.Desc, entity => entity.CreateTime);
            orderBy.Next = OrderByConditionBuilder<CheckInLogEntity>.Build(OrderByType.Desc, entity => entity.Id);
            return await QueryAsync(entity => entity.UserId == userId
                                              && checkInTypes.Contains(entity.CheckInType)
                                              && entity.CreateTime >= DateTime.Parse("2020-1-1 00:00:00")
                                              && entity.CreateTime < DateTime.Now, orderBy, pageIndex, pageSize, master: false);// 查询结果来自从库
            #endregion
        }
    }
}
