using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Example.Dapper.Core.Application.Contracts;
using Example.Dapper.Core.Application.Dtos;
using Example.Dapper.Core.Domain.Entities;
using Example.Dapper.Core.Domain.Repositories;
using Newtonsoft.Json;
using Sean.Core.DbRepository;
using Sean.Core.DbRepository.Extensions;
using Sean.Utility.Contracts;

namespace Example.Dapper.Core.Application.Services
{
    public class TestSimpleService : ITestSimpleService
    {
        private readonly ILogger _logger;
        private readonly IMapper _mapper;
        private readonly CommonRepository<TestEntity> _testRepository;
        private readonly CommonRepository<CheckInLogEntity> _checkInLogRepository;

        public TestSimpleService(
            ISimpleLogger<TestSimpleService> logger,
            IMapper mapper,
            CommonRepository<TestEntity> testRepository,
            CommonRepository<CheckInLogEntity> checkInLogRepository)
        {
            _logger = logger;
            _mapper = mapper;
            _testRepository = testRepository;
            _checkInLogRepository = checkInLogRepository;
        }

        public async Task<bool> AddAsync(TestDto model)
        {
            return await _testRepository.AddAsync(_mapper.Map<TestEntity>(model));
        }

        public async Task<bool> AddAsync(IEnumerable<TestDto> list)
        {
            return await _testRepository.AddAsync(_mapper.Map<List<TestEntity>>(list));
        }

        public async Task<bool> AddOrUpdateAsync(TestDto model)
        {
            return await _testRepository.AddOrUpdateAsync(_mapper.Map<TestEntity>(model));
        }

        public async Task<bool> AddOrUpdateAsync(IEnumerable<TestDto> list)
        {
            return await _testRepository.AddOrUpdateAsync(_mapper.Map<List<TestEntity>>(list));
        }

        public async Task<bool> DeleteByIdAsync(long id)
        {
            return await _testRepository.DeleteAsync(entity => entity.Id == id) > 0;
        }

        public async Task<int> DeleteAllAsync()
        {
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

        public async Task<TestDto> GetByIdAsync(long id)
        {
            var entities = await _testRepository.GetAsync(entity => entity.Id == id);
            return _mapper.Map<TestDto>(entities);
        }

        public async Task<List<TestDto>> GetAllAsync()
        {
            var entity = await _testRepository.QueryAsync(entity => true);
            return _mapper.Map<List<TestDto>>(entity);
        }

        public async Task<bool> TestCRUDAsync(IDbTransaction trans = null)
        {
            _logger.LogDebug($"######Current database type: {_testRepository.DbType}");

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
            var addResult = await _testRepository.AddAsync(testModel, true, transaction: trans);
            _logger.LogDebug($"######Add result: {addResult}");
            if (!addResult)
            {
                return false;
            }

            testModel.AccountBalance = 1000M;
            testModel.Age = 20;
            var updateResult = await _testRepository.UpdateAsync(testModel, entity => new { entity.AccountBalance, entity.Age }, transaction: trans) > 0;
            _logger.LogDebug($"######Update result: {updateResult}");
            if (!updateResult)
            {
                return false;
            }

            var incrResult = await _testRepository.IncrementAsync(1, entity => entity.AccountBalance, entity => entity.Id == testModel.Id, transaction: trans);
            _logger.LogDebug($"######Increment result: {incrResult}");
            if (!incrResult)
            {
                return false;
            }

            var orderBy = OrderByConditionBuilder<TestEntity>.Build(OrderByType.Asc, entity => entity.UserId);
            orderBy.Next = OrderByConditionBuilder<TestEntity>.Build(OrderByType.Asc, entity => entity.Id);
            var queryResult = (await _testRepository.QueryAsync(entity => true, orderBy, 1, 3))?.ToList();
            _logger.LogDebug($"######Query result: {JsonConvert.SerializeObject(queryResult, Formatting.Indented)}");

            var getResult = await _testRepository.GetAsync(entity => entity.UserId == 10001);
            _logger.LogDebug($"######Get result: {JsonConvert.SerializeObject(getResult, Formatting.Indented)}");

            var sqlCommand = _testRepository.CreateQueryableBuilder(true)
                .Where(entity => entity.Age >= 18 && entity.IsVip)
                .OrderByField(OrderByType.Desc, entity => entity.CreateTime)
                .Page(1, 3)
                .Build();
            var executeDataTableResult = await _testRepository.ExecuteDataTableAsync(sqlCommand);
            _logger.LogDebug($"######ExecuteDataTable result: {JsonConvert.SerializeObject(executeDataTableResult, Formatting.Indented)}");

            var deleteResult = await _testRepository.DeleteAsync(testModel, transaction: trans);
            _logger.LogDebug($"######Delete result: {deleteResult}");
            if (!deleteResult)
            {
                return false;
            }

            return true;
        }
    }
}
