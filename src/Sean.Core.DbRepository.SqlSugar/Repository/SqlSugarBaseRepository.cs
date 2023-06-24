using SqlSugar;

namespace Sean.Core.DbRepository.SqlSugar;

public abstract class SqlSugarBaseRepository<TEntity> : SimpleClient<TEntity>, ISqlSugarBaseRepository<TEntity> where TEntity : class, new()
{
    protected SqlSugarBaseRepository(ISqlSugarClient context = null) : base(context)
    {
    }
}