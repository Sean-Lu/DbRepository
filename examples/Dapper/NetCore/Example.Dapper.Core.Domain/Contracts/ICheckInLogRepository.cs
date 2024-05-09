using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Example.Dapper.Core.Domain.Entities;
using Sean.Core.DbRepository;

namespace Example.Dapper.Core.Domain.Contracts
{
    public interface ICheckInLogRepository : IBaseRepository<CheckInLogEntity>
    {
        /// <summary>
        /// 分表规则：按时间分表
        /// </summary>
        DateTime? SubTableDate { get; set; }

        Task<IEnumerable<CheckInLogEntity>> SearchAsync(long userId, int pageNumber, int pageSize);
    }
}
