using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Example.Application.Contracts;
using Example.Application.Dtos;
using Example.Domain.Contracts;
using Example.Domain.Entities;
using Sean.Core.DbRepository;
using Sean.Core.DbRepository.Extensions;
using Sean.Utility.Contracts;

namespace Example.Application.Services
{
    public class TestService : ITestService
    {
        private readonly ILogger _logger;
        private readonly IMapper _mapper;
        private readonly ITestRepository _testRepository;

        public TestService(
            ISimpleLogger<TestService> logger,
            IMapper mapper,
            ITestRepository testRepository)
        {
            _logger = logger;
            _mapper = mapper;
            _testRepository = testRepository;
        }

        public async Task<bool> AddAsync(TestDto model)
        {
            return await _testRepository.AddAsync(_mapper.Map<TestEntity>(model));
        }

        public async Task<bool> AddAsync(IEnumerable<TestDto> list)
        {
            //return await _testRepository.AddAsync(_mapper.Map<List<TestEntity>>(list));

            return await list.PagingExecuteAsync(200, async (pageIndex, models) => await _testRepository.AddAsync(_mapper.Map<List<TestEntity>>(models)));
        }

        public async Task<bool> AddOrUpdateAsync(TestDto model)
        {
            return await _testRepository.AddOrUpdateAsync(_mapper.Map<TestEntity>(model));
        }

        public async Task<bool> AddOrUpdateAsync(IEnumerable<TestDto> list)
        {
            //return await _testRepository.AddOrUpdateAsync(_mapper.Map<List<TestEntity>>(list));

            return await list.PagingExecuteAsync(200, async (pageIndex, models) => await _testRepository.AddOrUpdateAsync(_mapper.Map<List<TestEntity>>(models)));
        }

        public async Task<bool> DeleteByIdAsync(long id)
        {
            return await _testRepository.DeleteAsync(entity => entity.Id == id) > 0;
        }

        public async Task<int> DeleteAllAsync()
        {
            return await _testRepository.DeleteAsync(entity => true);
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
            var entity = await _testRepository.GetAsync(entity => entity.Id == id);
            return _mapper.Map<TestDto>(entity);
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
