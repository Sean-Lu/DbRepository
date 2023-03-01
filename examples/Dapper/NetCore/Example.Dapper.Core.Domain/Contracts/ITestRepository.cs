using System.Data;
using System.Threading.Tasks;
using Example.Dapper.Core.Domain.Entities;
using Sean.Core.DbRepository;

namespace Example.Dapper.Core.Domain.Contracts
{
    public interface ITestRepository : IBaseRepository<TestEntity>
    {
        Task<bool> TestCRUDAsync(IDbTransaction trans = null);
        Task<bool> TestCRUDWithTransactionAsync();
    }
}