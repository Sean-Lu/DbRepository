using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Sean.Utility.Extensions;

namespace Sean.Core.DbRepository.DbFirst;

public static class CodeGeneratorFactory
{
    private static readonly Dictionary<DatabaseType, ICodeGenerator> _codeGenerators = new();
    private static readonly object _syncRoot = new();
    private static readonly ConditionalWeakTable<ICodeGenerator, object> _generatorLocks = new();

    static CodeGeneratorFactory()
    {
        #region Set default implement for ICodeGenerator.
        SetCodeGenerator(DatabaseType.MySql, new CodeGeneratorForMySql());
        SetCodeGenerator(DatabaseType.MariaDB, new CodeGeneratorForMySql(DatabaseType.MariaDB));
        SetCodeGenerator(DatabaseType.TiDB, new CodeGeneratorForMySql(DatabaseType.TiDB));
        SetCodeGenerator(DatabaseType.OceanBase, new CodeGeneratorForMySql(DatabaseType.OceanBase));
        SetCodeGenerator(DatabaseType.SqlServer, new CodeGeneratorForSqlServer());
        SetCodeGenerator(DatabaseType.Oracle, new CodeGeneratorForOracle());
        SetCodeGenerator(DatabaseType.SQLite, new CodeGeneratorForSQLite());
        SetCodeGenerator(DatabaseType.DuckDB, new CodeGeneratorForSQLite(DatabaseType.DuckDB));
        SetCodeGenerator(DatabaseType.MsAccess, new CodeGeneratorForMsAccess());
        SetCodeGenerator(DatabaseType.Firebird, new CodeGeneratorForFirebird());
        SetCodeGenerator(DatabaseType.PostgreSql, new CodeGeneratorForPostgreSql());
        SetCodeGenerator(DatabaseType.OpenGauss, new CodeGeneratorForPostgreSql(DatabaseType.OpenGauss));
        SetCodeGenerator(DatabaseType.HighgoDB, new CodeGeneratorForPostgreSql(DatabaseType.HighgoDB));
        SetCodeGenerator(DatabaseType.IvorySQL, new CodeGeneratorForPostgreSql(DatabaseType.IvorySQL));
        SetCodeGenerator(DatabaseType.QuestDB, null);
        SetCodeGenerator(DatabaseType.DB2, null);
        SetCodeGenerator(DatabaseType.Informix, null);
        SetCodeGenerator(DatabaseType.ClickHouse, null);
        SetCodeGenerator(DatabaseType.Dameng, new CodeGeneratorForDameng());
        SetCodeGenerator(DatabaseType.KingbaseES, null);
        SetCodeGenerator(DatabaseType.ShenTong, null);
        SetCodeGenerator(DatabaseType.Xugu, null);
        #endregion
    }

    public static void SetCodeGenerator(DatabaseType dbType, ICodeGenerator codeGenerator)
    {
        lock (_syncRoot)
        {
            _codeGenerators.AddOrUpdate(dbType, codeGenerator);
        }
    }

    public static ICodeGenerator GetCodeGenerator(DatabaseType dbType)
    {
        lock (_syncRoot)
        {
            _codeGenerators.TryGetValue(dbType, out var codeGenerator);
            return codeGenerator;
        }
    }

    internal static TResult UseCodeGenerator<TResult>(DatabaseType dbType,
        Func<ICodeGenerator, TResult> action)
    {
        var codeGenerator = GetCodeGenerator(dbType);
        if (codeGenerator == null)
        {
            return action(null);
        }

        // DbFirst 生成器同样保存初始化状态，读取元数据前不能被其他连接重新初始化。
        var generatorLock = _generatorLocks.GetValue(codeGenerator, _ => new object());
        lock (generatorLock)
        {
            return action(codeGenerator);
        }
    }
}
