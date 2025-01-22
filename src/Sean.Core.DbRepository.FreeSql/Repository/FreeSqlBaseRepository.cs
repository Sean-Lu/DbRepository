using FreeSql;

namespace Sean.Core.DbRepository.FreeSql;

public abstract class FreeSqlBaseRepository<TEntity> : BaseRepository<TEntity>, IFreeSqlBaseRepository<TEntity> where TEntity : class
{
    protected FreeSqlBaseRepository(IFreeSql fsql) : base(fsql)
    {
    }
}