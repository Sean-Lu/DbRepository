using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Sean.Core.DbRepository.EntityFramework;

public interface IEFBaseRepository<TEntity> where TEntity : class
{
    DbSet<TEntity> Entities { get; }

    #region Synchronous method
    int Execute(string sql, params object[] parameters);
    List<T> Query<T>(string sql, params object[] parameters);

    bool Add(TEntity entity);
    bool Add(IEnumerable<TEntity> entities);

    bool Delete(TEntity entity);
    bool Delete(IEnumerable<TEntity> entities);

    bool Update(TEntity entity);
    bool Update(IEnumerable<TEntity> entities);

    List<TEntity> Query(Expression<Func<TEntity, bool>> whereExpression);
    List<TEntity> QueryWithNoTracking(Expression<Func<TEntity, bool>> whereExpression);
    List<TEntity> QueryAll();

    TEntity Get(Expression<Func<TEntity, bool>> whereExpression);
    TEntity GetById(params object[] keyValues);

    int Count();
    int Count(Expression<Func<TEntity, bool>> whereExpression);
    #endregion

    #region Asynchronous method
    Task<int> ExecuteAsync(string sql, params object[] parameters);
    Task<List<T>> QueryAsync<T>(string sql, params object[] parameters);

    Task<bool> AddAsync(TEntity entity);
    Task<bool> AddAsync(IEnumerable<TEntity> entities);

    Task<bool> DeleteAsync(TEntity entity);
    Task<bool> DeleteAsync(IEnumerable<TEntity> entities);

    Task<bool> UpdateAsync(TEntity entity);
    Task<bool> UpdateAsync(IEnumerable<TEntity> entities);

    Task<List<TEntity>> QueryAsync(Expression<Func<TEntity, bool>> whereExpression);
    Task<List<TEntity>> QueryWithNoTrackingAsync(Expression<Func<TEntity, bool>> whereExpression);
    Task<List<TEntity>> QueryAllAsync();

    Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> whereExpression);
    Task<TEntity> GetByIdAsync(params object[] keyValues);

    Task<int> CountAsync();
    Task<int> CountAsync(Expression<Func<TEntity, bool>> whereExpression);
    #endregion
}