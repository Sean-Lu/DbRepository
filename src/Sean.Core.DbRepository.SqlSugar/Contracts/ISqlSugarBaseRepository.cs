using SqlSugar;

namespace Sean.Core.DbRepository.SqlSugar;

public interface ISqlSugarBaseRepository<TEntity> : ISugarRepository, ISimpleClient<TEntity> where TEntity : class, new()
{

}