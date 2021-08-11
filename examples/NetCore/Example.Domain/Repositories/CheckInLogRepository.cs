using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Example.Domain.Contracts;
using Example.Domain.Entities;
using Microsoft.Extensions.Configuration;
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


        public async Task<IEnumerable<CheckInLogEntity>> SearchAsync(long userId, int pageIndex, int pageSize)
        {
            var sql = $"SELECT * FROM `{MainTableName}` WHERE {nameof(CheckInLogEntity.UserId)}=@{nameof(CheckInLogEntity.UserId)} LIMIT {(pageIndex - 1) * pageSize},{pageSize}";
            var entities = await ExecuteAsync(c => c.QueryAsync<CheckInLogEntity>(sql, new { UserId = userId }), false);
            return entities;
        }
    }
}
