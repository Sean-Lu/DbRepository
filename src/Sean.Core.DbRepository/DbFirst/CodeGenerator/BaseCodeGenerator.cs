namespace Sean.Core.DbRepository.DbFirst;

public abstract class BaseCodeGenerator : IBaseCodeGenerator
{
    protected DbFactory _db;
    protected readonly DatabaseType _dbType;

    protected BaseCodeGenerator(DatabaseType dbType)
    {
        _dbType = dbType;
    }

    public virtual void Initialize(string connectionString)
    {
        _db = new DbFactory(new MultiConnectionSettings(ConnectionStringOptions.Create(connectionString, _dbType)));
    }
    public virtual void Initialize(DbFactory dbFactory)
    {
        _db = dbFactory;
    }
}