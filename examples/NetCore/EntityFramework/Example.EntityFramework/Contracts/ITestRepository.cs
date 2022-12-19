using Example.EntityFramework.Entities;

namespace Example.EntityFramework.Contracts
{
    public interface ITestRepository : IEFBaseRepository<TestEntity>
    {
    }
}
