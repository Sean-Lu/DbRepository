using FreeSql;

namespace Sean.Core.DbRepository.FreeSql;

public interface IFreeSqlBaseRepository<TEntity> : IBaseRepository<TEntity> where TEntity : class
{

}