using System;
using System.Linq;
using System.Reflection;

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

    public void Upgrade<TEntity>(Func<string, string> tableNameFunc = null)
    {
        Upgrade(typeof(TEntity), tableNameFunc);
    }
    public virtual void Upgrade(Type entityType, Func<string, string> tableNameFunc = null)
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

    public virtual void Upgrade(params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes().Where(c => c.GetCustomAttributes<CodeFirstAttribute>(false).Any()).ToList();
            types.ForEach(type =>
            {
                Upgrade(type);
            });
        }
    }
}