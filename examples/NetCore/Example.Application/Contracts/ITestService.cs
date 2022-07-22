using System.Collections.Generic;
using System.Threading.Tasks;
using Example.Application.Dtos;

namespace Example.Application.Contracts
{
    public interface ITestService
    {
        Task<bool> AddAsync(TestDto model);
        Task<bool> AddAsync(IEnumerable<TestDto> list);
        Task<bool> AddOrUpdateAsync(TestDto model);
        Task<bool> AddOrUpdateAsync(IEnumerable<TestDto> list);
        Task<bool> DeleteByIdAsync(long id);
        Task<int> DeleteAllAsync();
        Task<bool> UpdateStatusAsync(long id, int status);
        Task<TestDto> GetByIdAsync(long id);
    }
}