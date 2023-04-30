using Example.EF.Core.ConsoleApp.Contracts;
using Example.EF.Core.ConsoleApp.Entities;
using Microsoft.EntityFrameworkCore;

namespace Example.EF.Core.ConsoleApp.Repositories
{
    //public class TestRepository : EFBaseRepository<TestUpperEntity>, ITestRepository
    public class TestRepository : EFBaseRepository<TestEntity>, ITestRepository
    {
        public TestRepository(DbContext dbContext) : base(dbContext)
        {
        }
    }
}
