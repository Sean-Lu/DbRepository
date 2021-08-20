using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Example.Domain.Entities;
using Sean.Core.DbRepository.Contracts;

namespace Example.Domain.Contracts
{
    public interface ICheckInLogRepository : IBaseRepository<CheckInLogEntity>
    {
        /// <summary>
        /// 按时间分表
        /// </summary>
        DateTime SubTableDate { get; set; }

        Task<IEnumerable<CheckInLogEntity>> SearchAsync(long userId, int pageIndex, int pageSize);
    }
}
