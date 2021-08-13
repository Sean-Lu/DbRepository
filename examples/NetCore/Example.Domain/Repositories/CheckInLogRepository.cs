using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Example.Domain.Contracts;
using Example.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Sean.Core.DbRepository;
using Sean.Core.DbRepository.Dapper.Impls;
using Sean.Utility.Contracts;

namespace Example.Domain.Repositories
{
    public class CheckInLogRepository : BaseRepository<CheckInLogEntity>, ICheckInLogRepository
    {
        private readonly ILogger _logger;

        public CheckInLogRepository(
            IConfiguration configuration,
            ISimpleLogger<CheckInLogRepository> logger) : base(configuration)
        {
            _logger = logger;
        }

        public override void OutputExecutedSql(string sql, object param)
        {
            _logger.LogInfo($"执行了SQL: {sql}");
        }


        public async Task<IEnumerable<CheckInLogEntity>> SearchAsync(long userId, int pageIndex, int pageSize)
        {
            var param = new { UserId = userId, CheckInType = 1 };

            //// SqlFactory示例1：
            //var sqlFactory = NewSqlFactory(true)
            //    .Page(pageIndex, pageSize)
            //    .Where($"{nameof(CheckInLogEntity.UserId)}=@{nameof(CheckInLogEntity.UserId)}")
            //    .SetParameter(param);

            // SqlFactory示例2：
            var sqlFactory = NewSqlFactory(true)
                .Page(pageIndex, pageSize)
                .WhereField(nameof(CheckInLogEntity.UserId), SqlOperation.Equal, WhereSqlKeyword.None)
                .WhereField(nameof(CheckInLogEntity.CheckInType), SqlOperation.Equal, WhereSqlKeyword.And)
                .SetParameter(param);

            //// 返回结果示例1：
            //return await ExecuteAsync(async connection => await connection.QueryAsync<CheckInLogEntity>(sqlFactory.QueryPageSql, param));

            // 返回结果示例2：
            return await QueryPageAsync(sqlFactory);
        }
    }
}
