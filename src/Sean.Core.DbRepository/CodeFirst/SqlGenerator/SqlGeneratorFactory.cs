using System.Collections.Generic;
using Sean.Utility.Extensions;

namespace Sean.Core.DbRepository.CodeFirst;

public static class SqlGeneratorFactory
{
    private static readonly Dictionary<DatabaseType, ISqlGenerator> _sqlGenerators = new();

    static SqlGeneratorFactory()
    {
        #region Set default implement for ISqlGenerator.
        SetSqlGenerator(DatabaseType.MySql, new SqlGeneratorForMySql());
        SetSqlGenerator(DatabaseType.MariaDB, new SqlGeneratorForMySql());
        SetSqlGenerator(DatabaseType.TiDB, new SqlGeneratorForMySql());
        SetSqlGenerator(DatabaseType.OceanBase, new SqlGeneratorForMySql());
        SetSqlGenerator(DatabaseType.SqlServer, new SqlGeneratorForSqlServer());
        SetSqlGenerator(DatabaseType.Oracle, new SqlGeneratorForOracle());
        SetSqlGenerator(DatabaseType.SQLite, new SqlGeneratorForSQLite());
        SetSqlGenerator(DatabaseType.DuckDB, new SqlGeneratorForDuckDB());
        SetSqlGenerator(DatabaseType.MsAccess, new SqlGeneratorForMsAccess());
        SetSqlGenerator(DatabaseType.Firebird, new SqlGeneratorForFirebird());
        SetSqlGenerator(DatabaseType.PostgreSql, new SqlGeneratorForPostgreSql());
        SetSqlGenerator(DatabaseType.OpenGauss, new SqlGeneratorForOpenGauss());
        SetSqlGenerator(DatabaseType.HighgoDB, new SqlGeneratorForPostgreSql());
        SetSqlGenerator(DatabaseType.IvorySQL, new SqlGeneratorForPostgreSql());
        SetSqlGenerator(DatabaseType.QuestDB, new SqlGeneratorForQuestDB());
        SetSqlGenerator(DatabaseType.DB2, new SqlGeneratorForDB2());
        SetSqlGenerator(DatabaseType.Informix, new SqlGeneratorForInformix());
        SetSqlGenerator(DatabaseType.ClickHouse, new SqlGeneratorForClickHouse());
        SetSqlGenerator(DatabaseType.Dameng, new SqlGeneratorForDameng());
        SetSqlGenerator(DatabaseType.KingbaseES, new SqlGeneratorForPostgreSql());
        SetSqlGenerator(DatabaseType.ShenTong, new SqlGeneratorForShenTong());
        SetSqlGenerator(DatabaseType.Xugu, new SqlGeneratorForXugu());
        #endregion
    }

    public static void SetSqlGenerator(DatabaseType dbType, ISqlGenerator sqlGenerator)
    {
        _sqlGenerators.AddOrUpdate(dbType, sqlGenerator);
    }

    public static ISqlGenerator GetSqlGenerator(DatabaseType dbType)
    {
        _sqlGenerators.TryGetValue(dbType, out var sqlGenerator);
        return sqlGenerator;
    }
}