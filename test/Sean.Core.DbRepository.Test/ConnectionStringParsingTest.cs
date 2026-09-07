using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sean.Core.DbRepository.Test;

/// <summary>
/// 连接串按标准 ADO.NET 引号、转义和重复键规则处理，ORM 扩展不能污染驱动输入。
/// </summary>
[TestClass]
public class ConnectionStringParsingTest
{
    [TestMethod]
    [DataRow("Password=\"a;b=c\"", "a;b=c")]
    [DataRow("Password='a;b=c'", "a;b=c")]
    [DataRow("Password=\"a\"\"b;c\"", "a\"b;c")]
    [DataRow("Password='a''b;c'", "a'b;c")]
    [DataRow("Password=\"  a=b; c  \"", "  a=b; c  ")]
    [DataRow("Password=a=b=c", "a=b=c")]
    [DataRow("Password=  plain  ", "plain")]
    [DataRow("Password=\"\"", "")]
    public void Dictionary_DecodesQuotedValuesWithoutSplittingCredentials(string pair, string expected)
    {
        var values = ConnectionStringOptions.GetConnectionDictionary("Server=localhost;" + pair + ";DatabaseType=SQLite");
        Assert.AreEqual(expected, values["Password"]);
        Assert.AreEqual("localhost", values["Server"]);
        Assert.AreEqual(3, values.Count);
    }

    [TestMethod]
    public void Dictionary_UsesCaseInsensitiveLastValueAndStandardEmptyValueRules()
    {
        var values = ConnectionStringOptions.GetConnectionDictionary("Key=first;KEY=second;kEy=last;Empty=;QuotedEmpty=\"\";A==B=value");
        Assert.AreEqual("last", values["key"]);
        Assert.AreEqual("value", values["A=B"]);
        Assert.IsFalse(values.ContainsKey("Empty"));
        Assert.AreEqual("", values["QuotedEmpty"]);
        Assert.AreEqual(3, values.Count);
    }

    [TestMethod]
    public void DictionarySerialization_QuotesValuesAndEscapesKeys()
    {
        var values = new Dictionary<string, string>
        {
            { "Password", " p;'q\"=value;DatabaseType=Oracle " },
            { "A=B", "x;y=z" },
            { "Server", "localhost" }
        };
        var connectionString = ConnectionStringOptions.GetConnectionString(values);
        var parsed = new DbConnectionStringBuilder { ConnectionString = connectionString };
        Assert.AreEqual(values["Password"], parsed["Password"]);
        Assert.AreEqual(values["A=B"], parsed["A=B"]);
        Assert.AreEqual(3, parsed.Count);
        Assert.IsFalse(parsed.ContainsKey("DatabaseType"), "密码中的文本不能变为新的连接参数。");
        var roundTrip = ConnectionStringOptions.GetConnectionDictionary(connectionString);
        Assert.AreEqual(values["Password"], roundTrip["password"]);
        Assert.AreEqual(values["A=B"], roundTrip["a=b"]);
    }

    [TestMethod]
    [DataRow("DatabaseType=SQLite;ProviderName=Custom.Provider")]
    [DataRow("ProviderName='Custom.Provider';DatabaseType=\"SQLite\"")]
    [DataRow("databasetype=sqlite;PROVIDERNAME=Custom.Provider")]
    [DataRow("DatabaseType=MySql;databasetype=SQLite;ProviderName=Old;providername=Custom.Provider")]
    [DataRow("DatabaseType=;DatabaseType=SQLite;ProviderName=;ProviderName=Custom.Provider")]
    public void Parse_ExtractsAndRemovesBothExtensions(string extensions)
    {
        var source = "Server=localhost;Password=\"a;b=DatabaseType=Oracle\";" + extensions;
        Assert.IsTrue(ConnectionStringOptions.ParseConnectionString(source, out var cleaned, out var type, out var provider));
        Assert.AreEqual(DatabaseType.SQLite, type);
        Assert.AreEqual("Custom.Provider", provider);
        var builder = new DbConnectionStringBuilder { ConnectionString = cleaned };
        Assert.AreEqual(2, builder.Count);
        Assert.AreEqual("a;b=DatabaseType=Oracle", builder["Password"]);
        Assert.IsFalse(builder.ContainsKey("DatabaseType"));
        Assert.IsFalse(builder.ContainsKey("ProviderName"));
        // 二次解析没有扩展，应返回 false 且不再次改写连接串。
        Assert.IsFalse(ConnectionStringOptions.ParseConnectionString(cleaned, out var unchanged, out type, out provider));
        Assert.AreEqual(cleaned, unchanged);
        Assert.AreEqual(DatabaseType.Unknown, type);
        Assert.IsNull(provider);
    }

    [TestMethod]
    public void Parse_PreservesDefinedNumericTypesUnknownAndCustomProviderNames()
    {
        foreach (var type in new[] { DatabaseType.SQLite, DatabaseType.Unknown })
        {
            var source = "Server=localhost;DatabaseType=" + (int)type + ";ProviderName=My.Custom.Factory";
            Assert.IsTrue(ConnectionStringOptions.ParseConnectionString(source, out _, out var parsedType, out var provider));
            Assert.AreEqual(type, parsedType);
            Assert.AreEqual("My.Custom.Factory", provider);
        }
        Assert.IsTrue(ConnectionStringOptions.ParseConnectionString("Server=localhost;ProviderName=My.Custom.Factory", out _, out var unknown, out _));
        Assert.AreEqual(DatabaseType.Unknown, unknown);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("  ")]
    [DataRow("Server = localhost ; Password='a;b';")]
    [DataRow("Password='DatabaseType=Oracle;ProviderName=Custom'")]
    [DataRow("Application Name=DatabaseType;OtherProviderName=value")]
    [DataRow("Server=host;Broken;DatabaseType=SQLite")]
    [DataRow("provider-specific-format")]
    [DataRow("Driver={Some;Driver};Password={a;b=c}")]
    public void Parse_WithoutOrmExtensionsPreservesOriginalText(string source)
    {
        Assert.IsFalse(ConnectionStringOptions.ParseConnectionString(source, out var unchanged, out var type, out var provider));
        Assert.AreEqual(source, unchanged);
        Assert.AreEqual(DatabaseType.Unknown, type);
        Assert.IsNull(provider);
    }

    [TestMethod]
    [DataRow("DatabaseType=NotADatabase")]
    [DataRow("DatabaseType=99999")]
    [DataRow("DatabaseType=-1")]
    [DataRow("DatabaseType=\"MySql, MariaDB\"")]
    [DataRow("DatabaseType=\"SQLite, Unknown\"")]
    [DataRow("DatabaseType=")]
    [DataRow("DatabaseType=\"\"")]
    [DataRow("DatabaseType=' '")]
    [DataRow("ProviderName=")]
    [DataRow("ProviderName=\"\"")]
    [DataRow("ProviderName=' '")]
    [DataRow("DatabaseType=SQLite;databasetype=")]
    [DataRow("ProviderName=Custom.Provider;providername=")]
    [DataRow("DatabaseType=invalid;ProviderName=Custom.Provider")]
    [DataRow("DatabaseType=SQLite;ProviderName=")]
    public void Parse_InvalidExtensionsAreRejectedWithoutLeakingCredentials(string extensions)
    {
        const string secret = "synthetic-secret-value";
        var exception = Assert.Throws<ArgumentException>(() => ConnectionStringOptions.ParseConnectionString(
            "Server=localhost;Password=" + secret + ";" + extensions, out _, out _, out _));
        Assert.AreEqual("connectionString", exception.ParamName);
        Assert.IsFalse(exception.Message.Contains(secret));
    }

    [TestMethod]
    [DataRow("Server=host;DatabaseType=SQLite;Broken")]
    [DataRow("Server=host;Password='not closed;DatabaseType=SQLite")]
    [DataRow("Server=host;Password=\"not closed;DatabaseType=SQLite")]
    [DataRow("Password='quoted'trailing;DatabaseType=SQLite")]
    [DataRow("=value;DatabaseType=SQLite")]
    public void MalformedStandardStringsThrowArgumentException(string source)
    {
        Assert.Throws<ArgumentException>(() => ConnectionStringOptions.GetConnectionDictionary(source));
        Assert.Throws<ArgumentException>(() => ConnectionStringOptions.ParseConnectionString(source, out _, out _, out _));
    }

    [TestMethod]
    public void EmptyDictionaryAndNullInputsHaveExplicitBehavior()
    {
        Assert.AreEqual(0, ConnectionStringOptions.GetConnectionDictionary(null).Count);
        Assert.AreEqual(0, ConnectionStringOptions.GetConnectionDictionary(" \t ").Count);
        Assert.AreEqual("", ConnectionStringOptions.GetConnectionString(new Dictionary<string, string>()));
        Assert.Throws<ArgumentNullException>(() => ConnectionStringOptions.GetConnectionString(null));
        Assert.Throws<ArgumentException>(() => ConnectionStringOptions.CreateFromConnectionString(" "));
    }

    [TestMethod]
    public void SQLite_ExecutesWithBothExtensionsRemoved()
    {
        var options = ConnectionStringOptions.CreateFromConnectionString(
            "Data Source=:memory:;Version=3;Pooling=False;databasetype=sqlite;ProviderName=System.Data.SQLite", false);
        Assert.IsTrue(options.IsValid);
        Assert.IsFalse(options.Master);
        Assert.AreEqual(DatabaseType.SQLite, options.DbType);
        Assert.AreEqual("System.Data.SQLite", options.ProviderName);
        // 让真实驱动接收清理后的连接串，残留 ORM 扩展不能只靠字典断言掩盖。
        using var connection = new SQLiteConnection(options.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 7";
        Assert.AreEqual(7L, command.ExecuteScalar());
    }
}
