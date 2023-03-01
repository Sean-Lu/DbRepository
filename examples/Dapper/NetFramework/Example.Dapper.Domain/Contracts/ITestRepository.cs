using System.Data;
using System.Threading.Tasks;
using Example.Dapper.Model.Entities;
using Sean.Core.DbRepository;

namespace Example.Dapper.Domain.Contracts
{
    public interface ITestRepository : IBaseRepository<TestEntity>
    {
        Task<bool> TestCRUDAsync(IDbTransaction trans = null);
        Task<bool> TestCRUDWithTransactionAsync();
    }
}