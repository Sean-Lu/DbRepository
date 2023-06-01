using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Example.ADO.NETCore.Application.Dtos;

namespace Example.ADO.NETCore.Application.Contracts
{
    /// <summary>
    /// 测试（使用通用仓储）
    /// </summary>
    public interface ITestSimpleService
    {
        Task<bool> AddAsync(TestDto model);
        Task<bool> AddAsync(IEnumerable<TestDto> list);
        Task<bool> AddOrUpdateAsync(TestDto model);
        Task<bool> AddOrUpdateAsync(IEnumerable<TestDto> list);
        Task<bool> DeleteByIdAsync(long id);
        Task<int> DeleteAllAsync();
        Task<bool> UpdateStatusAsync(long id, int status);
        Task<TestDto> GetByIdAsync(long id);
        Task<List<TestDto>> GetAllAsync();

        Task<bool> TestCRUDAsync(IDbTransaction trans = null);
    }
}