using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Example.Dapper.Core.Application.Contracts;
using Example.Dapper.Core.Domain.Contracts;
using Example.Dapper.Core.Domain.Entities;
using Sean.Utility.Contracts;

namespace Example.Dapper.Core.Application.Services;

public class UserService(
    ISimpleLogger<UserService> logger,
    IUserRepository userRepository
    ) : IUserService
{
    private readonly ILogger _logger = logger;

    public async Task<bool> AddAsync(UserEntity model)
    {
        return await userRepository.AddAsync(model);
    }

    public async Task<bool> AddAsync(IEnumerable<UserEntity> list)
    {
        return await userRepository.AddAsync(list);
    }

    public async Task<bool> AddOrUpdateAsync(UserEntity model)
    {
        return await userRepository.AddOrUpdateAsync(model);
    }

    public async Task<bool> AddOrUpdateAsync(IEnumerable<UserEntity> list)
    {
        return await userRepository.AddOrUpdateAsync(list);
    }

    public async Task<bool> DeleteByIdAsync(long id)
    {
        return await userRepository.DeleteAsync(entity => entity.Id == id) > 0;
    }

    public async Task<int> DeleteAllAsync()
    {
        return await userRepository.DeleteAllAsync();
    }

    public async Task<bool> UpdateStatusAsync(long id, int status)
    {
        return await userRepository.UpdateAsync(new UserEntity
        {
            Id = id,
            Status = status
        }, entity => new { entity.Status }) > 0;
    }

    public async Task<UserEntity> GetByIdAsync(long id)
    {
        return await userRepository.GetAsync(entity => entity.Id == id);
    }

    public async Task<List<UserEntity>> GetAllAsync()
    {
        return (await userRepository.QueryAsync(entity => true))?.ToList();
    }
}