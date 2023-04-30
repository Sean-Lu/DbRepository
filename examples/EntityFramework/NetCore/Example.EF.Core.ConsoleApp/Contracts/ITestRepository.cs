using Example.EF.Core.ConsoleApp.Entities;

namespace Example.EF.Core.ConsoleApp.Contracts
{
    //public interface ITestRepository : IEFBaseRepository<TestUpperEntity>
    public interface ITestRepository : IEFBaseRepository<TestEntity>
    {
    }
}
