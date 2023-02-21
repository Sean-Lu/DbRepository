using System;
using Example.Model.Entities;
using Sean.Core.DbRepository;

namespace Example.Domain.Contracts
{
    public interface ICheckInLogRepository : IBaseRepository<CheckInLogEntity>
    {
        /// <summary>
        /// 分表规则：按时间分表
        /// </summary>
        DateTime? SubTableDate { get; set; }
    }
}
