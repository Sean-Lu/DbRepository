namespace Sean.Core.DbRepository.DbFirst;

public abstract class BaseCodeGenerator
{
    protected readonly DatabaseType _dbType;

    protected BaseCodeGenerator(DatabaseType dbType)
    {
        _dbType = dbType;
    }
}