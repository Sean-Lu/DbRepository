using System.Linq.Expressions;
using Example.EF.Core.ConsoleApp.Contracts;
using Microsoft.EntityFrameworkCore;
#if NET6_0

#else
using System.Data.Entity;
#endif

namespace Example.EF.Core.ConsoleApp.Repositories
{
    public abstract class EFBaseRepository<TEntity> : IEFBaseRepository<TEntity> where TEntity : class
    {
        public DbSet<TEntity> Entities => _db.Set<TEntity>();

        private readonly DbContext _db;

        protected EFBaseRepository(DbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public int Execute(string sql, params object[] parameters)
        {
            return _db.Database.ExecuteSqlRaw(sql, parameters);
        }

        public bool Add(TEntity entity)
        {
            Entities.Add(entity);
            return _db.SaveChanges() > 0;
        }

        public bool Delete(TEntity entity)
        {
            Entities.Remove(entity);
            return _db.SaveChanges() > 0;
        }

        public bool Update(TEntity entity)
        {
            _db.Entry(entity).State = EntityState.Modified;
            return _db.SaveChanges() > 0;
        }

        public List<TEntity> QueryWithNoTracking(Expression<Func<TEntity, bool>> whereExpression)
        {
            // AsNoTracking: 无跟踪查询。如果查询出来的对象不需要修改并保存到数据库，推荐使用这种方式查询，查询效率更高。
            return Entities.Where(whereExpression).AsNoTracking().ToList();
        }
        public List<TEntity> Query(Expression<Func<TEntity, bool>> whereExpression)
        {
            return Entities.Where(whereExpression).ToList();
        }
        public List<TEntity> QueryAll()
        {
            return Entities.ToList();
        }

        public TEntity? Get(Expression<Func<TEntity, bool>> whereExpression)
        {
            return Entities.FirstOrDefault(whereExpression);
        }
        public TEntity? GetById(params object[] keyValues)
        {
            return Entities.Find(keyValues);
        }

        public int Count()
        {
            return Entities.Count();
        }
        public int Count(Expression<Func<TEntity, bool>> whereExpression)
        {
            return Entities.Count(whereExpression);
        }
    }
}
