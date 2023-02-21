using System.Threading.Tasks;
using Example.Model.Entities;
using Sean.Core.DbRepository;

namespace Example.Domain.Contracts
{
    public interface ITestRepository : IBaseRepository<TestEntity>
    {
        Task TestCRUDWithTransactionAsync();
    }
}