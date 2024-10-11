using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Example.Dapper.Application.Contracts;
using Example.Dapper.Domain.Contracts;
using Example.Dapper.Model.Entities;
using Sean.Utility.Contracts;

namespace Example.Dapper.Application.Services
{
    public class UserService : IUserService
    {
        private readonly ILogger _logger;
        private readonly IUserRepository _userRepository;

        public UserService(
            ISimpleLogger<UserService> logger,
            IUserRepository userRepository)
        {
            _logger = logger;
            _userRepository = userRepository;
        }

        public async Task<bool> AddAsync(UserEntity model)
        {
            return await _userRepository.AddAsync(model);
        }

        public async Task<bool> AddAsync(IEnumerable<UserEntity> list)
        {
            return await _userRepository.AddAsync(list);
        }

        public async Task<bool> AddOrUpdateAsync(UserEntity model)
        {
            return await _userRepository.AddOrUpdateAsync(model);
        }

        public async Task<bool> AddOrUpdateAsync(IEnumerable<UserEntity> list)
        {
            return await _userRepository.AddOrUpdateAsync(list);
        }

        public async Task<bool> DeleteByIdAsync(long id)
        {
            return await _userRepository.DeleteAsync(entity => entity.Id == id) > 0;
        }

        public async Task<int> DeleteAllAsync()
        {
            return await _userRepository.DeleteAllAsync();
        }

        public async Task<bool> UpdateStatusAsync(long id, int status)
        {
            return await _userRepository.UpdateAsync(new UserEntity
            {
                Id = id,
                Status = status
            }, entity => new { entity.Status }) > 0;
        }

        public async Task<UserEntity> GetByIdAsync(long id)
        {
            return await _userRepository.GetAsync(entity => entity.Id == id);
        }

        public async Task<List<UserEntity>> GetAllAsync()
        {
            return (await _userRepository.QueryAsync(entity => true))?.ToList();
        }
    }
}