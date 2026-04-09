using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Example.ADO.NETCore.Application.Contracts;
using Example.ADO.NETCore.Domain.Entities;
using Example.ADO.NETCore.Domain.Repositories;
using Newtonsoft.Json;
using Sean.Core.DbRepository;
using Sean.Core.DbRepository.Extensions;
using Sean.Utility.Contracts;

namespace Example.ADO.NETCore.Application.Services;

public class TestSimpleService(
    ISimpleLogger<TestSimpleService> logger,
    CommonRepository<TestEntity> testRepository
    ) : ITestSimpleService
{
    private readonly ILogger _logger = logger;

    public async Task<bool> AddAsync(TestEntity model)
    {
        return await testRepository.AddAsync(model);
    }

    public async Task<bool> AddAsync(IEnumerable<TestEntity> list)
    {
        return await testRepository.AddAsync(list);
    }

    public async Task<bool> AddOrUpdateAsync(TestEntity model)
    {
        return await testRepository.AddOrUpdateAsync(model);
    }

    public async Task<bool> AddOrUpdateAsync(IEnumerable<TestEntity> list)
    {
        return await testRepository.AddOrUpdateAsync(list);
    }

    public async Task<bool> DeleteByIdAsync(long id)
    {
        return await testRepository.DeleteAsync(entity => entity.Id == id) > 0;
    }

    public async Task<int> DeleteAllAsync()
    {
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

    public async Task<bool> TestCRUDAsync(IDbTransaction trans = null)
    {
        _logger.LogDebug($"######Current database type: {testRepository.DbType}");

        var testModel = new TestEntity
        {
            //Id = 1,
            UserId = 10001,
            UserName = "Test01",
            Age = 18,
            IsVip = true,
            AccountBalance = 99.95M,
            Remark = "Test",
            CreateTime = DateTime.Now,
            UpdateTime = DateTime.Now
        };
        var addResult = await testRepository.AddAsync(testModel, true, transaction: trans);
        _logger.LogDebug($"######Add result: {addResult}");
        if (!addResult)
        {
            return false;
        }

        testModel.AccountBalance = 1000M;
        testModel.Age = 20;
        var updateResult = await testRepository.UpdateAsync(testModel, entity => new { entity.AccountBalance, entity.Age }, transaction: trans) > 0;
        _logger.LogDebug($"######Update result: {updateResult}");
        if (!updateResult)
        {
            return false;
        }

        testModel.AccountBalance++;
        testModel.Age++;
        var addOrUpdateResult = await testRepository.AddOrUpdateAsync(testModel, transaction: trans);
        _logger.LogDebug($"######AddOrUpdate result: {addOrUpdateResult}");
        if (!addOrUpdateResult)
        {
            return false;
        }

        var incrResult = await testRepository.IncrementAsync(1, entity => entity.AccountBalance, entity => entity.Id == testModel.Id, transaction: trans);
        _logger.LogDebug($"######Increment result: {incrResult}");
        if (!incrResult)
        {
            return false;
        }

        var orderBy = OrderByConditionBuilder<TestEntity>.Build(OrderByType.Asc, entity => entity.UserId);
        orderBy.Next = OrderByConditionBuilder<TestEntity>.Build(OrderByType.Asc, entity => entity.Id);
        var queryResult = (await testRepository.QueryAsync(entity => true, orderBy, 1, 3))?.ToList();
        _logger.LogDebug($"######Query result: {JsonConvert.SerializeObject(queryResult, Formatting.Indented)}");

        var getResult = await testRepository.GetAsync(entity => entity.UserId == 10001);
        _logger.LogDebug($"######Get result: {JsonConvert.SerializeObject(getResult, Formatting.Indented)}");

        var sqlCommand = testRepository.CreateQueryableBuilder()
            .Where(entity => entity.Age >= 18 && entity.IsVip)
            .OrderBy(OrderByType.Desc, entity => entity.CreateTime)
            .Page(1, 3)
            .Build();
        var executeDataTableResult = await testRepository.ExecuteDataTableAsync(sqlCommand);
        _logger.LogDebug($"######ExecuteDataTable result: {JsonConvert.SerializeObject(executeDataTableResult, Formatting.Indented)}");

        var deleteResult = await testRepository.DeleteAsync(testModel, transaction: trans);
        _logger.LogDebug($"######Delete result: {deleteResult}");
        if (!deleteResult)
        {
            return false;
        }

        return true;
    }
}