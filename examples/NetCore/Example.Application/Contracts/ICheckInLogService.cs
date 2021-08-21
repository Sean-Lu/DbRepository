using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Example.Application.Dtos;

namespace Example.Application.Contracts
{
    public interface ICheckInLogService
    {
        Task<bool> AddAsync(CheckInLogDto checkInLogDto);

        Task<List<CheckInLogDto>> SearchAsync(long userId, int pageIndex, int pageSize);
    }
}
