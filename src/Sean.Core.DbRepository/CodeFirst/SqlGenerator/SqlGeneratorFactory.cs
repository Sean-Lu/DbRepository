using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Sean.Utility.Extensions;

namespace Sean.Core.DbRepository.CodeFirst;

public static class SqlGeneratorFactory
{
    private static readonly Dictionary<DatabaseType, ISqlGenerator> _sqlGenerators = new();
    private static readonly object _syncRoot = new();
    private static readonly ConditionalWeakTable<ISqlGenerator, object> _generatorLocks = new();

    static SqlGeneratorFactory()
    {
        #region Set default implement for ISqlGenerator.
        SetSqlGenerator(DatabaseType.MySql, new SqlGeneratorForMySql());
        SetSqlGenerator(DatabaseType.MariaDB, new SqlGeneratorForMySql(DatabaseType.MariaDB));
        SetSqlGenerator(DatabaseType.TiDB, new SqlGeneratorForMySql(DatabaseType.TiDB));
        SetSqlGenerator(DatabaseType.OceanBase, new SqlGeneratorForMySql(DatabaseType.OceanBase));
        SetSqlGenerator(DatabaseType.SqlServer, new SqlGeneratorForSqlServer());
        SetSqlGenerator(DatabaseType.Oracle, new SqlGeneratorForOracle());
        SetSqlGenerator(DatabaseType.SQLite, new SqlGeneratorForSQLite());
        SetSqlGenerator(DatabaseType.DuckDB, new SqlGeneratorForDuckDB());
        SetSqlGenerator(DatabaseType.MsAccess, new SqlGeneratorForMsAccess());
        SetSqlGenerator(DatabaseType.Firebird, new SqlGeneratorForFirebird());
        SetSqlGenerator(DatabaseType.PostgreSql, new SqlGeneratorForPostgreSql());
        SetSqlGenerator(DatabaseType.OpenGauss, new SqlGeneratorForOpenGauss());
        SetSqlGenerator(DatabaseType.HighgoDB, new SqlGeneratorForPostgreSql(DatabaseType.HighgoDB));
        SetSqlGenerator(DatabaseType.IvorySQL, new SqlGeneratorForPostgreSql(DatabaseType.IvorySQL));
        SetSqlGenerator(DatabaseType.QuestDB, new SqlGeneratorForQuestDB());
        SetSqlGenerator(DatabaseType.DB2, new SqlGeneratorForDB2());
        SetSqlGenerator(DatabaseType.Informix, new SqlGeneratorForInformix());
        SetSqlGenerator(DatabaseType.ClickHouse, new SqlGeneratorForClickHouse());
        SetSqlGenerator(DatabaseType.Dameng, new SqlGeneratorForDameng());
        SetSqlGenerator(DatabaseType.KingbaseES, new SqlGeneratorForPostgreSql(DatabaseType.KingbaseES));
        SetSqlGenerator(DatabaseType.ShenTong, new SqlGeneratorForShenTong());
        SetSqlGenerator(DatabaseType.Xugu, new SqlGeneratorForXugu());
        #endregion
    }

    public static void SetSqlGenerator(DatabaseType dbType, ISqlGenerator sqlGenerator)
    {
        lock (_syncRoot)
        {
            _sqlGenerators.AddOrUpdate(dbType, sqlGenerator);
        }
    }

    public static ISqlGenerator GetSqlGenerator(DatabaseType dbType)
    {
        lock (_syncRoot)
        {
            _sqlGenerators.TryGetValue(dbType, out var sqlGenerator);
            return sqlGenerator;
        }
    }

    internal static TResult UseSqlGenerator<TResult>(DatabaseType dbType,
        Func<ISqlGenerator, TResult> action)
    {
        var sqlGenerator = GetSqlGenerator(dbType);
        if (sqlGenerator == null)
        {
            return action(null);
        }

        // 生成器由公开工厂按实例注册，必须把初始化和后续生成放在同一临界区内。
        // 使用内部锁对象，避免在调用方可获取的生成器实例上加锁而造成外部锁顺序冲突。
        var generatorLock = _generatorLocks.GetValue(sqlGenerator, _ => new object());
        lock (generatorLock)
        {
            return action(sqlGenerator);
        }
    }
}
