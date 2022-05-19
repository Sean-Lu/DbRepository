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

namespace Example.Application.Services
{
    public class TestService : ITestService
    {
        private readonly IMapper _mapper;
        private readonly ITestRepository _testRepository;

        public TestService(
            IMapper mapper,
            ITestRepository testRepository)
        {
            _mapper = mapper;
            _testRepository = testRepository;
        }

        public async Task<bool> AddAsync(TestDto checkInLogDto)
        {
            return await _testRepository.AddAsync(_mapper.Map<TestEntity>(checkInLogDto));
        }

        public async Task<TestDto> GetByIdAsync(long id)
        {
            var entity = await _testRepository.GetAsync(entity => entity.Id == id);
            return _mapper.Map<TestDto>(entity);
        }
    }
}
