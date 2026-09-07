using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sean.Core.DbRepository.Test;

/// <summary>
/// 配置入口的扩展字段解析及已有连接选择规则，不建立实际数据库连接。
/// </summary>
[TestClass]
public class ConnectionStringConfigurationTest
{
    [TestMethod]
    [DataRow("Driver={Example;ODBC};Server=db;PWD={a;b=c};DatabaseType=SQLite;ProviderName=custom", false)]
    [DataRow("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\\data\\sample.accdb;Extended Properties=\"Excel 12.0;HDR=YES\";", true)]
    [DataRow("Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=db)(PORT=1521)));Password=a=b;", false)]
    public void ExplicitConstructorsAndFactories_PreserveOriginalProviderSyntax(string connectionString, bool master)
    {
        // 显式指定提供程序的入口必须透传原串，不能用通用解析器重写厂商语法。
        var providerOptions = new[]
        {
            new ConnectionStringOptions(connectionString, "Example.Provider", master),
            ConnectionStringOptions.Create(connectionString, "Example.Provider", master)
        };
        var typeOptions = new[]
        {
            new ConnectionStringOptions(connectionString, DatabaseType.SQLite, master),
            ConnectionStringOptions.Create(connectionString, DatabaseType.SQLite, master)
        };
        var factoryOptions = new[]
        {
            new ConnectionStringOptions(connectionString, SQLiteFactory.Instance, master),
            ConnectionStringOptions.Create(connectionString, SQLiteFactory.Instance, master)
        };

        foreach (var options in providerOptions.Concat(typeOptions).Concat(factoryOptions))
        {
            Assert.AreEqual(connectionString, options.ConnectionString);
            Assert.AreEqual(master, options.Master);
            Assert.IsTrue(options.IsValid);
            Assert.IsNull(options.ConnectionName);
        }
        foreach (var options in providerOptions)
        {
            Assert.AreEqual("Example.Provider", options.ProviderName);
            Assert.AreEqual(DatabaseType.Unknown, options.DbType);
        }
        foreach (var options in typeOptions)
        {
            Assert.AreEqual(DatabaseType.SQLite, options.DbType);
            Assert.IsNull(options.ProviderName);
        }
        foreach (var options in factoryOptions)
        {
            Assert.AreSame(SQLiteFactory.Instance, options.ProviderFactory);
        }
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    public void IsValid_RequiresANonEmptyConnectionString(string connectionString)
    {
        Assert.IsFalse(ConnectionStringOptions.Create(connectionString, DatabaseType.SQLite).IsValid);
        Assert.IsFalse(ConnectionStringOptions.Create(connectionString, "Example.Provider").IsValid);
        Assert.IsFalse(ConnectionStringOptions.Create(connectionString, SQLiteFactory.Instance).IsValid);
    }

    [TestMethod]
    public void IsValid_RequiresProviderMetadataAndMasterDefaultsToTrue()
    {
        Assert.IsFalse(ConnectionStringOptions.Create("Data Source=db", DatabaseType.Unknown).IsValid);
        Assert.IsFalse(ConnectionStringOptions.Create("Data Source=db", (string)null).IsValid);
        Assert.IsFalse(ConnectionStringOptions.Create("Data Source=db", " ").IsValid);
        Assert.IsFalse(ConnectionStringOptions.Create("Data Source=db", (DbProviderFactory)null).IsValid);
        Assert.IsTrue(ConnectionStringOptions.Create("Data Source=db", DatabaseType.SQLite).Master);
        Assert.IsTrue(ConnectionStringOptions.Create("Data Source=db", "Example.Provider").Master);
        Assert.IsTrue(ConnectionStringOptions.Create("Data Source=db", SQLiteFactory.Instance).Master);
    }

    [TestMethod]
    public void CreateFromConnectionName_CleansBothExtensionsAndPreservesQuotedValues()
    {
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            ["ConnectionStrings:orders"] = "Data Source=\"db;name=value\";Password=\"a;b=c\";dAtAbAsEtYpE=SQLite;pRoViDeRnAmE=Example.Provider",
            ["DatabaseSettings:DatabaseType"] = "MySql",
            ["DatabaseSettings:ProviderName"] = "Global.Provider"
        });

        var options = ConnectionStringOptions.CreateFromConnectionName(configuration, "orders", false);

        Assert.AreEqual("orders", options.ConnectionName);
        Assert.IsFalse(options.Master);
        AssertEmbeddedMetadata(options, "db;name=value");
        Assert.AreEqual("a;b=c", ParseProviderValues(options)["Password"]);
    }

    [TestMethod]
    [DataRow(0, DatabaseType.SQLite, null)]
    [DataRow(1, DatabaseType.Unknown, "Global.Provider")]
    [DataRow(2, DatabaseType.MySql, null)]
    [DataRow(3, DatabaseType.Unknown, "Named.Provider")]
    public void CreateFromConnectionName_PreservesGlobalAndNamedMetadataPriority(int firstAvailable, DatabaseType expectedType, string expectedProvider)
    {
        var values = new Dictionary<string, string> { ["ConnectionStrings:orders"] = "Data Source=db" };
        var candidates = new[]
        {
            new KeyValuePair<string, string>("DatabaseSettings:DatabaseType", "SQLite"),
            new KeyValuePair<string, string>("DatabaseSettings:ProviderName", "Global.Provider"),
            new KeyValuePair<string, string>("DatabaseSettings:DatabaseTypes:orders", "MySql"),
            new KeyValuePair<string, string>("DatabaseSettings:ProviderNames:orders", "Named.Provider")
        };
        foreach (var candidate in candidates.Skip(firstAvailable))
        {
            values.Add(candidate.Key, candidate.Value);
        }

        var options = ConnectionStringOptions.CreateFromConnectionName(CreateConfiguration(values), "orders");

        Assert.AreEqual(expectedType, options.DbType);
        Assert.AreEqual(expectedProvider, options.ProviderName);
        Assert.AreEqual("db", ParseProviderValues(options)["Data Source"]);
        Assert.IsTrue(options.Master);
        Assert.IsTrue(options.IsValid);
    }

    [TestMethod]
    public void EmbeddedProviderName_TakesPriorityOverGlobalDatabaseType()
    {
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            ["ConnectionStrings:orders"] = "Data Source=db;pRoViDeRnAmE=Embedded.Provider",
            ["DatabaseSettings:DatabaseType"] = "MySql"
        });

        var options = ConnectionStringOptions.CreateFromConnectionName(configuration, "orders");

        Assert.AreEqual(DatabaseType.Unknown, options.DbType);
        Assert.AreEqual("Embedded.Provider", options.ProviderName);
        Assert.AreEqual("db", ParseProviderValues(options)["Data Source"]);
    }

    [TestMethod]
    public void ReloadFromConnectionName_ReplacesParsedValuesWithoutChangingConnectionRole()
    {
        var options = new ConnectionStringOptions("Data Source=old", "Old.Provider", false)
        {
            ConnectionName = "orders"
        };
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            ["ConnectionStrings:orders"] = EmbeddedConnection("new;db=value")
        });

        options.ReloadFromConnectionName(configuration);

        AssertEmbeddedMetadata(options, "new;db=value");
        Assert.AreEqual("orders", options.ConnectionName);
        Assert.IsFalse(options.Master);
    }

    [TestMethod]
    [DataRow("Data Source=new;DatabaseType=NotADatabase;ProviderName=New.Provider")]
    [DataRow("Data Source=\"unterminated;DatabaseType=SQLite;ProviderName=New.Provider")]
    public void ReloadFromConnectionName_WhenParsingFails_DoesNotPartiallyUpdateOptions(string invalidConnectionString)
    {
        var options = new ConnectionStringOptions("Data Source=old", DatabaseType.SQLite, false)
        {
            ConnectionName = "orders",
            ProviderName = "Old.Provider",
            ProviderFactory = SQLiteFactory.Instance
        };
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            ["ConnectionStrings:orders"] = invalidConnectionString
        });

        Assert.Throws<ArgumentException>(() => options.ReloadFromConnectionName(configuration));

        Assert.AreEqual("Data Source=old", options.ConnectionString);
        Assert.AreEqual(DatabaseType.SQLite, options.DbType);
        Assert.AreEqual("Old.Provider", options.ProviderName);
        Assert.AreSame(SQLiteFactory.Instance, options.ProviderFactory);
        Assert.AreEqual("orders", options.ConnectionName);
        Assert.IsFalse(options.Master);
    }

    [TestMethod]
    public void ReloadFromConnectionName_WhenSwitchingToProviderName_ClearsPreviousDatabaseType()
    {
        var options = ConnectionStringOptions.CreateFromConnectionString("Data Source=old;DatabaseType=SQLite", false);
        options.ConnectionName = "orders";
        options.ProviderFactory = SQLiteFactory.Instance;
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            ["ConnectionStrings:orders"] = "Data Source=new",
            ["DatabaseSettings:ProviderName"] = "New.Provider"
        });

        options.ReloadFromConnectionName(configuration);

        Assert.AreEqual("new", ParseProviderValues(options)["Data Source"]);
        Assert.AreEqual(DatabaseType.Unknown, options.DbType);
        Assert.AreEqual("New.Provider", options.ProviderName);
        Assert.AreSame(SQLiteFactory.Instance, options.ProviderFactory);
        Assert.AreEqual("orders", options.ConnectionName);
        Assert.IsFalse(options.Master);
    }

    [TestMethod]
    public void ReloadFromConnectionName_WhenSwitchingToDatabaseType_ClearsPreviousProviderName()
    {
        var options = new ConnectionStringOptions("Data Source=old", "Old.Provider", false)
        {
            ConnectionName = "orders",
            ProviderFactory = SQLiteFactory.Instance
        };
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            ["ConnectionStrings:orders"] = "Data Source=new",
            ["DatabaseSettings:DatabaseType"] = "MySql"
        });

        options.ReloadFromConnectionName(configuration);

        Assert.AreEqual("new", ParseProviderValues(options)["Data Source"]);
        Assert.AreEqual(DatabaseType.MySql, options.DbType);
        Assert.IsNull(options.ProviderName);
        Assert.AreSame(SQLiteFactory.Instance, options.ProviderFactory);
        Assert.AreEqual("orders", options.ConnectionName);
        Assert.IsFalse(options.Master);
    }

    [TestMethod]
    public void ReloadFromConnectionName_WhenConfiguredDatabaseTypeIsInvalid_PreservesExistingOptions()
    {
        var options = new ConnectionStringOptions("Data Source=old", DatabaseType.SQLite, false)
        {
            ConnectionName = "orders",
            ProviderName = "Old.Provider",
            ProviderFactory = SQLiteFactory.Instance
        };
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            ["ConnectionStrings:orders"] = "Data Source=new",
            ["DatabaseSettings:DatabaseType"] = "NotADatabase"
        });

        // 保留 ConfigurationBinder 对无效配置值的异常类型，失败前不能发布新连接串。
        Assert.Throws<InvalidOperationException>(() => options.ReloadFromConnectionName(configuration));

        Assert.AreEqual("Data Source=old", options.ConnectionString);
        Assert.AreEqual(DatabaseType.SQLite, options.DbType);
        Assert.AreEqual("Old.Provider", options.ProviderName);
        Assert.AreSame(SQLiteFactory.Instance, options.ProviderFactory);
        Assert.AreEqual("orders", options.ConnectionName);
        Assert.IsFalse(options.Master);
    }

    [TestMethod]
    public void CreateMultiFromConnectionName_DirectNamedConnectionTakesPriorityOverCluster()
    {
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            ["ConnectionStrings:orders"] = EmbeddedConnection("direct"),
            ["ConnectionStrings:orders.master"] = EmbeddedConnection("cluster-master"),
            ["ConnectionStrings:orders.secondary"] = EmbeddedConnection("cluster-secondary")
        });

        var options = ConnectionStringOptions.CreateMultiFromConnectionName(configuration, "orders");

        Assert.AreEqual(1, options.Count);
        Assert.AreEqual("orders", options[0].ConnectionName);
        Assert.IsTrue(options[0].Master);
        AssertEmbeddedMetadata(options[0], "direct");
    }

    [TestMethod]
    public void CreateMultiFromConnectionName_UnnumberedMasterAndSecondaryTakePriority()
    {
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            ["ConnectionStrings:master"] = EmbeddedConnection("primary"),
            ["ConnectionStrings:master1"] = EmbeddedConnection("ignored-primary"),
            ["ConnectionStrings:secondary"] = EmbeddedConnection("replica"),
            ["ConnectionStrings:secondary1"] = EmbeddedConnection("ignored-replica")
        });

        var options = ConnectionStringOptions.CreateMultiFromConnectionName(configuration, "master");

        CollectionAssert.AreEqual(new[] { "master", "secondary" }, options.Select(option => option.ConnectionName).ToArray());
        Assert.IsTrue(options[0].Master);
        Assert.IsFalse(options[1].Master);
        AssertEmbeddedMetadata(options[0], "primary");
        AssertEmbeddedMetadata(options[1], "replica");
    }

    [TestMethod]
    public void CreateMultiFromConnectionName_NumberedNamedClusterStopsAtFirstMissingEntry()
    {
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            ["ConnectionStrings:orders.master1"] = EmbeddedConnection("primary1"),
            ["ConnectionStrings:orders.master2"] = EmbeddedConnection("primary2"),
            ["ConnectionStrings:orders.master4"] = EmbeddedConnection("ignored-primary4"),
            ["ConnectionStrings:orders.secondary1"] = EmbeddedConnection("replica1"),
            ["ConnectionStrings:orders.secondary3"] = EmbeddedConnection("ignored-replica3")
        });

        var options = ConnectionStringOptions.CreateMultiFromConnectionName(configuration, "orders");

        CollectionAssert.AreEqual(new[] { "orders.master1", "orders.master2", "orders.secondary1" },
            options.Select(option => option.ConnectionName).ToArray());
        CollectionAssert.AreEqual(new[] { true, true, false }, options.Select(option => option.Master).ToArray());
        AssertEmbeddedMetadata(options[0], "primary1");
        AssertEmbeddedMetadata(options[1], "primary2");
        AssertEmbeddedMetadata(options[2], "replica1");
    }

    [TestMethod]
    [DataRow("master", "secondary")]
    [DataRow("orders", "orders.secondary")]
    public void CreateMultiFromConnectionName_WithoutMasterDoesNotReturnSecondary(string connectionName, string secondaryName)
    {
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            [$"ConnectionStrings:{secondaryName}"] = EmbeddedConnection("replica")
        });

        Assert.AreEqual(0, ConnectionStringOptions.CreateMultiFromConnectionName(configuration, connectionName).Count);
    }

    [TestMethod]
    public void MissingConnection_PreservesInvalidSingleAndEmptyMultiResults()
    {
        var configuration = CreateConfiguration(new Dictionary<string, string>());

        var single = ConnectionStringOptions.CreateFromConnectionName(configuration, "missing");

        Assert.IsNull(single.ConnectionString);
        Assert.IsFalse(single.IsValid);
        Assert.AreEqual(0, ConnectionStringOptions.CreateMultiFromConnectionName(configuration, "missing").Count);
    }

    private static IConfigurationRoot CreateConfiguration(Dictionary<string, string> values)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static string EmbeddedConnection(string dataSource)
    {
        return $"Data Source=\"{dataSource}\";dAtAbAsEtYpE=SQLite;pRoViDeRnAmE=Example.Provider";
    }

    private static void AssertEmbeddedMetadata(ConnectionStringOptions options, string expectedDataSource)
    {
        // 两项元数据都保留在选项中，由现有 DbFactory 决定提供程序优先级。
        Assert.AreEqual(DatabaseType.SQLite, options.DbType);
        Assert.AreEqual("Example.Provider", options.ProviderName);
        Assert.IsTrue(options.IsValid);
        Assert.AreEqual(expectedDataSource, ParseProviderValues(options)["Data Source"]);
    }

    private static DbConnectionStringBuilder ParseProviderValues(ConnectionStringOptions options)
    {
        var builder = new DbConnectionStringBuilder { ConnectionString = options.ConnectionString };
        Assert.IsFalse(builder.ContainsKey("DatabaseType"));
        Assert.IsFalse(builder.ContainsKey("ProviderName"));
        return builder;
    }
}
