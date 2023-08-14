using System;
using System.Collections.Generic;

namespace Sean.Core.DbRepository.CodeFirst;

public interface ISqlGenerator : IBaseSqlGenerator
{
    List<string> GetCreateTableSql<TEntity>(Func<string, string> tableNameFunc = null);
    List<string> GetCreateTableSql(Type entityType, Func<string, string> tableNameFunc = null);

    List<string> GetUpgradeSql<TEntity>(Func<string, string> tableNameFunc = null);
    List<string> GetUpgradeSql(Type entityType, Func<string, string> tableNameFunc = null);
}

public interface IBaseSqlGenerator
{
    void Initialize(string connectionString);
    void Initialize(DbFactory dbFactory);
}