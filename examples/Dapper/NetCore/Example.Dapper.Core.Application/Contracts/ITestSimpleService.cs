using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Example.Dapper.Core.Domain.Entities;

namespace Example.Dapper.Core.Application.Contracts
{
    /// <summary>
    /// 测试（使用通用仓储）
    /// </summary>
    public interface ITestSimpleService
    {
        Task<bool> AddAsync(TestEntity model);
        Task<bool> AddAsync(IEnumerable<TestEntity> list);
        Task<bool> AddOrUpdateAsync(TestEntity model);
        Task<bool> AddOrUpdateAsync(IEnumerable<TestEntity> list);
        Task<bool> DeleteByIdAsync(long id);
        Task<int> DeleteAllAsync();
        Task<bool> UpdateStatusAsync(long id, int status);
        Task<TestEntity> GetByIdAsync(long id);
        Task<List<TestEntity>> GetAllAsync();

        Task<bool> TestCRUDAsync(IDbTransaction trans = null);
    }
}