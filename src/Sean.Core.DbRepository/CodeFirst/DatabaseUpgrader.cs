using System;

namespace Sean.Core.DbRepository.CodeFirst;

public class DatabaseUpgrader : IDatabaseUpgrader
{
    private readonly DbFactory _db;
    private readonly DatabaseType _dbType;

    public DatabaseUpgrader(string connectionString, DatabaseType dbType)
    {
        _dbType = dbType;
        _db = new DbFactory(new MultiConnectionSettings(ConnectionStringOptions.Create(connectionString, _dbType)));
    }
    public DatabaseUpgrader(DbFactory dbFactory)
    {
        _dbType = dbFactory.DbType;
        _db = dbFactory;
    }

    public void UpgradeFromEntity<TEntity>(Func<string, string> tableNameFunc = null)
    {
        UpgradeFromEntity(typeof(TEntity), tableNameFunc);
    }
    public virtual void UpgradeFromEntity(Type entityType, Func<string, string> tableNameFunc = null)
    {
        ISqlGenerator sqlGenerator = SqlGeneratorFactory.GetSqlGenerator(_dbType);
        sqlGenerator.Initialize(_db);
        var upgradeSqlList = sqlGenerator.GetUpgradeSql(entityType, tableNameFunc);
        upgradeSqlList?.ForEach(sql =>
        {
            if (!string.IsNullOrWhiteSpace(sql))
            {
                _db.ExecuteNonQuery(sql);
            }
        });
    }
}