using System;

namespace Sean.Core.DbRepository.CodeFirst;

public interface ISqlGenerator
{
    string GetCreateTableSql<TEntity>(Func<string, string> tableNameFunc = null);
    string GetUpgradeSql<TEntity>(Func<string, string> tableNameFunc = null);
}