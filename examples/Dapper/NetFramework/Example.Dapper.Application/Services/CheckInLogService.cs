using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Example.Dapper.Application.Contracts;
using Example.Dapper.Domain.Contracts;
using Example.Dapper.Model.Entities;
using Sean.Core.DbRepository;
using Sean.Utility.Contracts;

namespace Example.Dapper.Application.Services
{
    public class CheckInLogService : ICheckInLogService
    {
        private readonly ILogger _logger;
        private readonly ICheckInLogRepository _checkInLogRepository;

        public CheckInLogService(
            ILogger<CheckInLogService> logger,
            ICheckInLogRepository checkInLogRepository)
        {
            _logger = logger;
            _checkInLogRepository = checkInLogRepository;
            //_checkInLogRepository.SubTableDate = DateTime.Now;// 按时间分表
        }

        public async Task<bool> AddAsync(CheckInLogEntity model)
        {
            return await _checkInLogRepository.AddAsync(model);
        }

        public async Task<bool> AddAsync(IEnumerable<CheckInLogEntity> list)
        {
            return await _checkInLogRepository.AddAsync(list);
        }

        public async Task<bool> AddOrUpdateAsync(CheckInLogEntity model)
        {
            return await _checkInLogRepository.AddOrUpdateAsync(model);
        }

        public async Task<bool> AddOrUpdateAsync(IEnumerable<CheckInLogEntity> list)
        {
            return await _checkInLogRepository.AddOrUpdateAsync(list);
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

        public async Task<CheckInLogEntity> GetByIdAsync(long id)
        {
            return await _checkInLogRepository.GetAsync(entity => entity.Id == id);
        }

        public async Task<List<CheckInLogEntity>> GetAllAsync()
        {
            return (await _checkInLogRepository.QueryAsync(entity => true, master: false))?.ToList();// 查询结果来自从库
        }

        public async Task<List<CheckInLogEntity>> SearchAsync(long userId, int pageNumber, int pageSize)
        {
            var orderBy = OrderByConditionBuilder<CheckInLogEntity>.Build(OrderByType.Desc, entity => entity.CreateTime);
            orderBy.Next = OrderByConditionBuilder<CheckInLogEntity>.Build(OrderByType.Desc, entity => entity.Id);
            return (await _checkInLogRepository.QueryAsync(entity => entity.UserId == userId, orderBy, pageNumber, pageSize, master: false))?.ToList();// 查询结果来自从库
        }
    }
}
