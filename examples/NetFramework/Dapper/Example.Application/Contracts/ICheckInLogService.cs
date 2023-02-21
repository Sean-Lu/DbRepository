using System.Collections.Generic;
using System.Threading.Tasks;
using Example.Model.Entities;

namespace Example.Application.Contracts
{
    public interface ICheckInLogService
    {
        Task<bool> AddAsync(CheckInLogEntity model);
        Task<bool> AddAsync(IEnumerable<CheckInLogEntity> list);
        Task<bool> AddOrUpdateAsync(CheckInLogEntity model);
        Task<bool> AddOrUpdateAsync(IEnumerable<CheckInLogEntity> list);
        Task<bool> DeleteByIdAsync(long id);
        Task<int> DeleteAllAsync();
        Task<bool> UpdateCheckInTypeAsync(long id, int checkInType);
        Task<CheckInLogEntity> GetByIdAsync(long id);
        Task<List<CheckInLogEntity>> GetAllAsync();
        Task<List<CheckInLogEntity>> SearchAsync(long userId, int pageIndex, int pageSize);
    }
}
