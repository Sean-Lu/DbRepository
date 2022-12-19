using Microsoft.EntityFrameworkCore;

namespace Example.EntityFramework.Contracts
{
    public interface IEFBaseRepository<TEntity> where TEntity : class
    {
        DbSet<TEntity> Entities { get; }
        int Execute(string sql, params object[] parameters);
    }
}
