using System.Data.Common;

namespace Sean.Core.DbRepository;

public interface ITypeHandler
{
    void Set(DbParameter dbParameter, object value, DatabaseType databaseType);
}