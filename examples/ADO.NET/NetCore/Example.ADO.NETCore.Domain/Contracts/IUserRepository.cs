using Example.ADO.NETCore.Domain.Entities;
using Sean.Core.DbRepository;

namespace Example.ADO.NETCore.Domain.Contracts;

public interface IUserRepository : IBaseRepository<UserEntity>
{

}