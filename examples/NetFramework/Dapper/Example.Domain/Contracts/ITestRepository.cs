using System.Data;
using System.Threading.Tasks;
using Example.Model.Entities;
using Sean.Core.DbRepository;

namespace Example.Domain.Contracts
{
    public interface ITestRepository : IBaseRepository<TestEntity>
    {
        Task<bool> TestCRUDAsync(IDbTransaction trans = null);
        Task<bool> TestCRUDWithTransactionAsync();
    }
}