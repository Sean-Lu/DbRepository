using System;
using System.Collections.Generic;

namespace Sean.Core.DbRepository.CodeFirst;

public interface ISqlGenerator
{
    void Initialize(string connectionString);
    void Initialize(DbFactory dbFactory);

    List<string> GetCreateTableSql<TEntity>(bool ignoreIfExists = false, Func<string, string> tableNameFunc = null);
    List<string> GetCreateTableSql(Type entityType, bool ignoreIfExists = false, Func<string, string> tableNameFunc = null);

    List<string> GetUpgradeSql<TEntity>(Func<string, string> tableNameFunc = null);
    List<string> GetUpgradeSql(Type entityType, Func<string, string> tableNameFunc = null);
}