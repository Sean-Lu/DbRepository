using Example.Dapper.Model.Entities;
using Sean.Core.DbRepository;

namespace Example.Dapper.Domain.Contracts;

public interface IUserRepository : IBaseRepository<UserEntity>
{

}