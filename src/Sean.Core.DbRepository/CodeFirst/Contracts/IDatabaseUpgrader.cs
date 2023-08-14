using System;

namespace Sean.Core.DbRepository.CodeFirst;

public interface IDatabaseUpgrader
{
    void UpgradeFromEntity<TEntity>(Func<string, string> tableNameFunc = null);
    void UpgradeFromEntity(Type entityType, Func<string, string> tableNameFunc = null);
}