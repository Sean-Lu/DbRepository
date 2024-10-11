using System.Data;
using System.Threading.Tasks;
using Example.ADO.NETCore.Domain.Entities;
using Sean.Core.DbRepository;

namespace Example.ADO.NETCore.Domain.Contracts;

public interface ITestRepository : IBaseRepository<TestEntity>
{
    Task<bool> TestCRUDAsync(IDbTransaction trans = null);
    Task<bool> TestCRUDWithTransactionAsync();
}