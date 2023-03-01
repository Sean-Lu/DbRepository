using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Example.Dapper.Core.Application.Contracts;
using Example.Dapper.Core.Application.Dtos;
using Example.Dapper.Core.Domain.Contracts;
using Example.Dapper.Core.Domain.Entities;
using Sean.Utility.Contracts;

namespace Example.Dapper.Core.Application.Services
{
    public class CheckInLogService : ICheckInLogService
    {
        private readonly ILogger _logger;
        private readonly IMapper _mapper;
        private readonly ICheckInLogRepository _checkInLogRepository;

        public CheckInLogService(
            ISimpleLogger<CheckInLogService> logger,
            IMapper mapper,
            ICheckInLogRepository checkInLogRepository)
        {
            _logger = logger;
            _mapper = mapper;
            _checkInLogRepository = checkInLogRepository;
            //_checkInLogRepository.SubTableDate = DateTime.Now;// 按时间分表
        }

        public async Task<bool> AddAsync(CheckInLogDto model)
        {
            return await _checkInLogRepository.AddAsync(_mapper.Map<CheckInLogEntity>(model));
        }

        public async Task<bool> AddAsync(IEnumerable<CheckInLogDto> list)
        {
            return await _checkInLogRepository.AddAsync(_mapper.Map<List<CheckInLogEntity>>(list));
            //return await list.PagingExecuteAsync(200, async (pageIndex, models) => await _checkInLogRepository.AddAsync(_mapper.Map<List<CheckInLogEntity>>(models)));
        }

        public async Task<bool> AddOrUpdateAsync(CheckInLogDto model)
        {
            return await _checkInLogRepository.AddOrUpdateAsync(_mapper.Map<CheckInLogEntity>(model));
        }

        public async Task<bool> AddOrUpdateAsync(IEnumerable<CheckInLogDto> list)
        {
            return await _checkInLogRepository.AddOrUpdateAsync(_mapper.Map<List<CheckInLogEntity>>(list));
            //return await list.PagingExecuteAsync(200, async (pageIndex, models) => await _checkInLogRepository.AddOrUpdateAsync(_mapper.Map<List<CheckInLogEntity>>(models)));
        }

        public async Task<bool> DeleteByIdAsync(long id)
        {
            return await _checkInLogRepository.DeleteAsync(entity => entity.Id == id) > 0;
        }

        public async Task<int> DeleteAllAsync()
        {
            //return await _checkInLogRepository.DeleteAsync(entity => true);
            return await _checkInLogRepository.DeleteAllAsync();
        }

        public async Task<bool> UpdateCheckInTypeAsync(long id, int checkInType)
        {
            return await _checkInLogRepository.UpdateAsync(new CheckInLogEntity
            {
                CheckInType = checkInType
            }, entity => new { entity.CheckInType }, entity => entity.Id == id) > 0;
        }

        public async Task<CheckInLogDto> GetByIdAsync(long id)
        {
            var entity = await _checkInLogRepository.GetAsync(entity => entity.Id == id);
            return _mapper.Map<CheckInLogDto>(entity);
        }

        public async Task<List<CheckInLogDto>> GetAllAsync()
        {
            var entities = await _checkInLogRepository.QueryAsync(entity => true, master: false);// 查询结果来自从库
            return _mapper.Map<List<CheckInLogDto>>(entities);
        }

        public async Task<List<CheckInLogDto>> SearchAsync(long userId, int pageIndex, int pageSize)
        {
            var entities = await _checkInLogRepository.SearchAsync(userId, pageIndex, pageSize);
            return _mapper.Map<List<CheckInLogDto>>(entities);
        }
    }
}
