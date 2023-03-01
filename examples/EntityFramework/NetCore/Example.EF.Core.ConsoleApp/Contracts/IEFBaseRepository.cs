using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Example.EF.Core.ConsoleApp.Contracts
{
    public interface IEFBaseRepository<TEntity> where TEntity : class
    {
        DbSet<TEntity> Entities { get; }

        int Execute(string sql, params object[] parameters);

        bool Add(TEntity entity);

        bool Delete(TEntity entity);

        bool Update(TEntity entity);

        List<TEntity> QueryWithNoTracking(Expression<Func<TEntity, bool>> whereExpression);
        List<TEntity> Query(Expression<Func<TEntity, bool>> whereExpression);
        List<TEntity> QueryAll();

        TEntity? Get(Expression<Func<TEntity, bool>> whereExpression);
        TEntity? GetById(params object[] keyValues);

        int Count();
        int Count(Expression<Func<TEntity, bool>> whereExpression);
    }
}
