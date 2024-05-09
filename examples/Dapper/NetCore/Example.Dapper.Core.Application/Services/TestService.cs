using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Example.Dapper.Core.Application.Contracts;
using Example.Dapper.Core.Domain.Contracts;
using Example.Dapper.Core.Domain.Entities;
using Sean.Utility.Contracts;
using Sean.Utility.Extensions;

namespace Example.Dapper.Core.Application.Services
{
    public class TestService : ITestService
    {
        private readonly ILogger _logger;
        private readonly ITestRepository _testRepository;

        public TestService(
            ISimpleLogger<TestService> logger,
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
            //return await list.PagingExecuteAsync(200, async (pageNumber, models) => await _testRepository.AddAsync(models));
        }

        public async Task<bool> AddOrUpdateAsync(TestEntity model)
        {
            return await _testRepository.AddOrUpdateAsync(model);
        }

        public async Task<bool> AddOrUpdateAsync(IEnumerable<TestEntity> list)
        {
            return await _testRepository.AddOrUpdateAsync(list);
            //return await list.PagingExecuteAsync(200, async (pageNumber, models) => await _testRepository.AddOrUpdateAsync(models));
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

        public async Task<bool> TestCRUDAsync()
        {
            return await _testRepository.TestCRUDAsync();
        }

        public async Task<bool> TestCRUDWithTransactionAsync()
        {
            return await _testRepository.TestCRUDWithTransactionAsync();
        }

        public async Task<bool> ExecuteAutoTransactionTest()
        {
            try
            {
                return await _testRepository.ExecuteAutoTransactionAsync(async trans =>
                {
                    var testEntity = new TestEntity
                    {
                        Id = 124,
                        AccountBalance = 100,
                        IsVip = true
                    };

                    if (!await _testRepository.AddAsync(testEntity, transaction: trans))
                    {
                        return false;
                    }

                    testEntity.AccountBalance = 999;
                    if (await _testRepository.UpdateAsync(testEntity, entity => entity.AccountBalance, transaction: trans) < 1)
                    {
                        return false;
                    }

                    return true;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("ExecuteAutoTransactionTest 执行异常", ex);
                return false;
            }
        }
    }
}
