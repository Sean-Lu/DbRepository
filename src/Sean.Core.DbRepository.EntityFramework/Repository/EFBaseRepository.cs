using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Sean.Core.DbRepository.EntityFramework;

public abstract class EFBaseRepository<TEntity> : IEFBaseRepository<TEntity> where TEntity : class
{
    public virtual DbSet<TEntity> Entities => _db.Set<TEntity>();

    protected readonly DbContext _db;

    protected EFBaseRepository(DbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    #region Synchronous method
    public virtual int Execute(string sql, params object[] parameters)
    {
        return _db.Database.ExecuteSqlCommand(sql, parameters);
    }
    public virtual List<T> Query<T>(string sql, params object[] parameters)
    {
        return _db.Database.SqlQuery<T>(sql, parameters).ToList();
    }

    public virtual bool Add(TEntity entity)
    {
        Entities.Add(entity);
        return _db.SaveChanges() > 0;
    }

    public virtual bool Add(IEnumerable<TEntity> entities)
    {
        Entities.AddRange(entities);
        return _db.SaveChanges() > 0;
    }

    public virtual bool Delete(TEntity entity)
    {
        Entities.Remove(entity);
        return _db.SaveChanges() > 0;
    }

    public virtual bool Delete(IEnumerable<TEntity> entities)
    {
        Entities.RemoveRange(entities);
        return _db.SaveChanges() > 0;
    }

    public virtual bool Update(TEntity entity)
    {
        _db.Entry(entity).State = EntityState.Modified;
        return _db.SaveChanges() > 0;
    }

    public virtual bool Update(IEnumerable<TEntity> entities)
    {
        foreach (var entity in entities)
        {
            _db.Entry(entity).State = EntityState.Modified;
        }
        return _db.SaveChanges() > 0;
    }

    public virtual List<TEntity> Query(Expression<Func<TEntity, bool>> whereExpression)
    {
        return Entities.Where(whereExpression).ToList();
    }
    public virtual List<TEntity> QueryWithNoTracking(Expression<Func<TEntity, bool>> whereExpression)
    {
        // AsNoTracking: 无跟踪查询。如果查询出来的对象不需要修改并保存到数据库，推荐使用这种方式查询，查询效率更高。
        return Entities.Where(whereExpression).AsNoTracking().ToList();
    }
    public virtual List<TEntity> QueryAll()
    {
        return Entities.ToList();
    }

    public virtual TEntity Get(Expression<Func<TEntity, bool>> whereExpression)
    {
        return Entities.FirstOrDefault(whereExpression);
    }
    public virtual TEntity GetById(params object[] keyValues)
    {
        return Entities.Find(keyValues);
    }

    public virtual int Count()
    {
        return Entities.Count();
    }
    public virtual int Count(Expression<Func<TEntity, bool>> whereExpression)
    {
        return Entities.Count(whereExpression);
    }
    #endregion

    #region Asynchronous method
    public virtual async Task<int> ExecuteAsync(string sql, params object[] parameters)
    {
        return await _db.Database.ExecuteSqlCommandAsync(sql, parameters);
    }
    public virtual async Task<List<T>> QueryAsync<T>(string sql, params object[] parameters)
    {
        return await _db.Database.SqlQuery<T>(sql, parameters).ToListAsync();
    }

    public virtual async Task<bool> AddAsync(TEntity entity)
    {
        Entities.Add(entity);
        return await _db.SaveChangesAsync() > 0;
    }

    public virtual async Task<bool> AddAsync(IEnumerable<TEntity> entities)
    {
        Entities.AddRange(entities);
        return await _db.SaveChangesAsync() > 0;
    }

    public virtual async Task<bool> DeleteAsync(TEntity entity)
    {
        Entities.Remove(entity);
        return await _db.SaveChangesAsync() > 0;
    }

    public virtual async Task<bool> DeleteAsync(IEnumerable<TEntity> entities)
    {
        Entities.RemoveRange(entities);
        return await _db.SaveChangesAsync() > 0;
    }

    public virtual async Task<bool> UpdateAsync(TEntity entity)
    {
        _db.Entry(entity).State = EntityState.Modified;
        return await _db.SaveChangesAsync() > 0;
    }

    public virtual async Task<bool> UpdateAsync(IEnumerable<TEntity> entities)
    {
        foreach (var entity in entities)
        {
            _db.Entry(entity).State = EntityState.Modified;
        }
        return await _db.SaveChangesAsync() > 0;
    }

    public virtual async Task<List<TEntity>> QueryAsync(Expression<Func<TEntity, bool>> whereExpression)
    {
        return await Entities.Where(whereExpression).ToListAsync();
    }
    public virtual async Task<List<TEntity>> QueryWithNoTrackingAsync(Expression<Func<TEntity, bool>> whereExpression)
    {
        // AsNoTracking: 无跟踪查询。如果查询出来的对象不需要修改并保存到数据库，推荐使用这种方式查询，查询效率更高。
        return await Entities.Where(whereExpression).AsNoTracking().ToListAsync();
    }
    public virtual async Task<List<TEntity>> QueryAllAsync()
    {
        return await Entities.ToListAsync();
    }

    public virtual async Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> whereExpression)
    {
        return await Entities.FirstOrDefaultAsync(whereExpression);
    }
    public virtual async Task<TEntity> GetByIdAsync(params object[] keyValues)
    {
        return await Entities.FindAsync(keyValues);
    }

    public virtual async Task<int> CountAsync()
    {
        return await Entities.CountAsync();
    }
    public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>> whereExpression)
    {
        return await Entities.CountAsync(whereExpression);
    }
    #endregion
}