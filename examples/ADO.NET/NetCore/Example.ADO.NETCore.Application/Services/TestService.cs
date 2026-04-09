using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Example.ADO.NETCore.Application.Contracts;
using Example.ADO.NETCore.Domain.Contracts;
using Example.ADO.NETCore.Domain.Entities;
using Sean.Utility.Contracts;

namespace Example.ADO.NETCore.Application.Services;

public class TestService(
    ISimpleLogger<TestService> logger,
    ITestRepository testRepository
    ) : ITestService
{
    private readonly ILogger _logger = logger;

    public async Task<bool> AddAsync(TestEntity model)
    {
        return await testRepository.AddAsync(model);
    }

    public async Task<bool> AddAsync(IEnumerable<TestEntity> list)
    {
        return await testRepository.AddAsync(list);
        //return await list.PagingExecuteAsync(200, async (pageNumber, models) => await testRepository.AddAsync(mapper.Map<List<TestEntity>>(models)));
    }

    public async Task<bool> AddOrUpdateAsync(TestEntity model)
    {
        return await testRepository.AddOrUpdateAsync(model);
    }

    public async Task<bool> AddOrUpdateAsync(IEnumerable<TestEntity> list)
    {
        return await testRepository.AddOrUpdateAsync(list);
        //return await list.PagingExecuteAsync(200, async (pageNumber, models) => await testRepository.AddOrUpdateAsync(mapper.Map<List<TestEntity>>(models)));
    }

    public async Task<bool> DeleteByIdAsync(long id)
    {
        return await testRepository.DeleteAsync(entity => entity.Id == id) > 0;
    }

    public async Task<int> DeleteAllAsync()
    {
        //return await testRepository.DeleteAsync(entity => true);
        return await testRepository.DeleteAllAsync();
    }

    public async Task<bool> UpdateStatusAsync(long id, int status)
    {
        return await testRepository.UpdateAsync(new TestEntity
        {
            Id = id,
            Status = status
        }, entity => new { entity.Status }) > 0;
    }

    public async Task<TestEntity> GetByIdAsync(long id)
    {
        return await testRepository.GetAsync(entity => entity.Id == id);
    }

    public async Task<List<TestEntity>> GetAllAsync()
    {
        return (await testRepository.QueryAsync(entity => true))?.ToList();
    }

    public async Task<bool> TestCRUDAsync()
    {
        return await testRepository.TestCRUDAsync();
    }

    public async Task<bool> TestCRUDWithTransactionAsync()
    {
        return await testRepository.TestCRUDWithTransactionAsync();
    }

    public async Task<bool> ExecuteAutoTransactionTest()
    {
        try
        {
            return await testRepository.ExecuteAutoTransactionAsync(async trans =>
            {
                var testEntity = new TestEntity
                {
                    Id = 124,
                    AccountBalance = 100,
                    IsVip = true
                };

                if (!await testRepository.AddAsync(testEntity, transaction: trans))
                {
                    return false;
                }

                testEntity.AccountBalance = 999;
                if (await testRepository.UpdateAsync(testEntity, entity => entity.AccountBalance, transaction: trans) < 1)
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