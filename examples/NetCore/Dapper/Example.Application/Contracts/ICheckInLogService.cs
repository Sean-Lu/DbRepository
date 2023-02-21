using System.Collections.Generic;
using System.Threading.Tasks;
using Example.Application.Dtos;

namespace Example.Application.Contracts
{
    public interface ICheckInLogService
    {
        Task<bool> AddAsync(CheckInLogDto model);
        Task<bool> AddAsync(IEnumerable<CheckInLogDto> list);
        Task<bool> AddOrUpdateAsync(CheckInLogDto model);
        Task<bool> AddOrUpdateAsync(IEnumerable<CheckInLogDto> list);
        Task<bool> DeleteByIdAsync(long id);
        Task<int> DeleteAllAsync();
        Task<bool> UpdateCheckInTypeAsync(long id, int checkInType);
        Task<CheckInLogDto> GetByIdAsync(long id);
        Task<List<CheckInLogDto>> GetAllAsync();
        Task<List<CheckInLogDto>> SearchAsync(long userId, int pageIndex, int pageSize);
    }
}
