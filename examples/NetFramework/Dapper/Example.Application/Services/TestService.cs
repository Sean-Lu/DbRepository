using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Example.Application.Contracts;
using Example.Domain.Contracts;
using Example.Model.Entities;
using Sean.Utility.Contracts;

namespace Example.Application.Services
{
    public class TestService : ITestService
    {
        private readonly ILogger _logger;
        private readonly ITestRepository _testRepository;

        public TestService(
            ILogger<TestService> logger,
            ITestRepository testRepository)
        {
            _logger = logger;
            _testRepository = testRepository;
        }

        public async Task<bool> AddAsync(TestEntity model)
        {
            return await _testRepository.AddAsync(model);
        }

        public async Task<bool> AddAsync(IEnumerable<TestEntity> list)
        {
            return await _testRepository.AddAsync(list);
        }

        public async Task<bool> AddOrUpdateAsync(TestEntity model)
        {
            return await _testRepository.AddOrUpdateAsync(model);
        }

        public async Task<bool> AddOrUpdateAsync(IEnumerable<TestEntity> list)
        {
            return await _testRepository.AddOrUpdateAsync(list);
        }

        public async Task<bool> DeleteByIdAsync(long id)
        {
            return await _testRepository.DeleteAsync(entity => entity.Id == id) > 0;
        }

        public async Task<int> DeleteAllAsync()
        {
            //return await _testRepository.DeleteAsync(entity => true);
            return await _testRepository.DeleteAllAsync();
        }

        public async Task<bool> UpdateStatusAsync(long id, int status)
        {
            return await _testRepository.UpdateAsync(new TestEntity
            {
                Id = id,
                Status = status
            }, entity => new { entity.Status }) > 0;
        }

        public async Task<TestEntity> GetByIdAsync(long id)
        {
            return await _testRepository.GetAsync(entity => entity.Id == id);
        }

        public async Task<List<TestEntity>> GetAllAsync()
        {
            return (await _testRepository.QueryAsync(entity => true))?.ToList();
        }

        public async Task TestCRUDWithTransactionAsync()
        {
            await _testRepository.TestCRUDWithTransactionAsync();
        }
    }
}
