using Example.EntityFramework.Contracts;
using Example.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;

namespace Example.EntityFramework.Repositories
{
    public class TestRepository : EFBaseRepository<TestEntity>, ITestRepository
    {
        public TestRepository(DbContext dbContext) : base(dbContext)
        {
        }
    }
}
