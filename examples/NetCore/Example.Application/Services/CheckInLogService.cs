using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Example.Application.Contracts;
using Example.Application.Dtos;
using Example.Domain.Contracts;

namespace Example.Application.Services
{
    public class CheckInLogService : ICheckInLogService
    {
        private readonly IMapper _mapper;
        private readonly ICheckInLogRepository _checkInLogRepository;

        public CheckInLogService(
            IMapper mapper,
            ICheckInLogRepository checkInLogRepository)
        {
            _mapper = mapper;
            _checkInLogRepository = checkInLogRepository;
            //_checkInLogRepository.SubTableDate = DateTime.Now;// 按时间分表
        }

        public async Task<List<CheckInLogDto>> SearchAsync(long userId, int pageIndex, int pageSize)
        {
            var entities = await _checkInLogRepository.SearchAsync(userId, pageIndex, pageSize);
            return _mapper.Map<List<CheckInLogDto>>(entities);
        }
    }
}
