using System.Collections.Generic;
using System.Threading.Tasks;
using Example.Dapper.Core.Domain.Entities;

namespace Example.Dapper.Core.Application.Contracts
{
    public interface IUserService
    {
        Task<bool> AddAsync(UserEntity model);
        Task<bool> AddAsync(IEnumerable<UserEntity> list);
        Task<bool> AddOrUpdateAsync(UserEntity model);
        Task<bool> AddOrUpdateAsync(IEnumerable<UserEntity> list);
        Task<bool> DeleteByIdAsync(long id);
        Task<int> DeleteAllAsync();
        Task<bool> UpdateStatusAsync(long id, int status);
        Task<UserEntity> GetByIdAsync(long id);
        Task<List<UserEntity>> GetAllAsync();
    }
}