using System.Collections.Generic;
using System.Threading.Tasks;
using Example.Model.Entities;

namespace Example.Application.Contracts
{
    public interface ITestService
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

        Task TestCRUDWithTransactionAsync();
    }
}