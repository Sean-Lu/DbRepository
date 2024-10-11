using Example.Dapper.Core.Domain.Entities;
using Sean.Core.DbRepository;

namespace Example.Dapper.Core.Domain.Contracts;

public interface IUserRepository : IBaseRepository<UserEntity>
{

}