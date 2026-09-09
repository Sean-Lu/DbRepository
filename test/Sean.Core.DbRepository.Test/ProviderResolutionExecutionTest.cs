using System;
using System.Data.Common;
using System.Data.SQLite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository.Test;

[TestClass]
[DoNotParallelize] // 驱动映射是全局配置，修改期间不能与其他数据库用例并行。
public class ProviderResolutionExecutionTest
{
    [TestMethod]
    [DataRow("instance")]
    [DataRow("static-field")]
    [DataRow("constructor")]
    public void Mapping_ResolvesExecutableFactoryByNameAndDatabaseType(string route)
    {
        var previous = DatabaseType.SQLite.GetDbProviderMap();
        var providerName = $"Sean.Provider.Test.{Guid.NewGuid():N}";
        var map = route switch
        {
            "instance" => new DbProviderMap(providerName, SQLiteFactory.Instance),
            "static-field" => new DbProviderMap(providerName, typeof(SQLiteFactory).AssemblyQualifiedName),
            _ => new DbProviderMap(providerName, typeof(ConstructorFactory).AssemblyQualifiedName)
        };
        try
        {
            DatabaseType.SQLite.SetDbProviderMap(map);
            var named = new DbFactory("Data Source=:memory:;Pooling=False;", providerName);
            Assert.AreEqual(DatabaseType.SQLite, named.DbType);
            Assert.AreEqual(41L, named.ExecuteScalar<long>("SELECT 41"));
            var typed = new DbFactory("Data Source=:memory:;Pooling=False;", DatabaseType.SQLite);
            Assert.AreSame(named.ProviderFactory, typed.ProviderFactory);
            Assert.AreEqual(42L, typed.ExecuteScalar<long>("SELECT 42"));
            if (route != "constructor") Assert.AreSame(SQLiteFactory.Instance, named.ProviderFactory);
            else Assert.IsInstanceOfType<ConstructorFactory>(named.ProviderFactory);
        }
        finally
        {
            DatabaseType.SQLite.SetDbProviderMap(previous);
            DbProviderFactories.UnregisterFactory(providerName);
        }
    }

    [TestMethod]
    public void UnmappedProvider_UsesAdoRegistrationAndInfersSqliteDialect()
    {
        var providerName = $"Sean.RegisteredProvider.Test.{Guid.NewGuid():N}";
        DbProviderFactories.RegisterFactory(providerName, SQLiteFactory.Instance);
        try
        {
            // 没有 ORM 名称映射时，应回退 ADO.NET 注册表，并从真实工厂推断方言。
            var factory = new DbFactory("Data Source=:memory:;Pooling=False;", providerName);
            Assert.AreSame(SQLiteFactory.Instance, factory.ProviderFactory);
            Assert.AreEqual(DatabaseType.SQLite, factory.DbType);
            Assert.AreEqual(43L, factory.ExecuteScalar<long>("SELECT 43"));
        }
        finally
        {
            DbProviderFactories.UnregisterFactory(providerName);
        }
    }

    // 不提供 Instance 字段，用真实 SQLite 命令验证无参构造回退路径。
    public sealed class ConstructorFactory : DbProviderFactory
    {
        public override DbConnection CreateConnection() => SQLiteFactory.Instance.CreateConnection();
        public override DbCommand CreateCommand() => SQLiteFactory.Instance.CreateCommand();
        public override DbParameter CreateParameter() => SQLiteFactory.Instance.CreateParameter();
    }
}
