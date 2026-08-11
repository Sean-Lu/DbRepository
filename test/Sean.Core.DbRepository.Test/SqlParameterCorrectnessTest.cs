using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sean.Core.DbRepository.Util;

namespace Sean.Core.DbRepository.Test;

/// <summary>
/// SQL 参数解析与替换的正确性测试。
/// </summary>
[TestClass]
public class SqlParameterCorrectnessTest
{
    private const string SqlWithPseudoParameters = @"SELECT @real, values[1:@upper], ':literal', 'it''s @escaped', ""@quoted"", `@backtick`, value::text,
       $tag$ @dollar_quoted $tag$, q'[oracle@quoted]'
FROM Sample
WHERE Id = :id -- @line_comment
  AND Name = $name /* :block_comment */";

    [TestMethod]
    public void ParseSqlParameters_IgnoresLiteralsCommentsCastsAndQuotedIdentifiers()
    {
        var parameters = SqlParameterUtil.ParseSqlParameters(SqlWithPseudoParameters)
            .OrderBy(parameter => parameter.Value)
            .Select(parameter => parameter.Key)
            .ToArray();

        CollectionAssert.AreEqual(new[] { "real", "upper", "id", "name" }, parameters);
    }

    [TestMethod]
    public void FilterAndRemoveUnusedParameters_UseOnlyExecutableSql()
    {
        var filtered = SqlParameterUtil.FilterParameters(
                new[] { "real", "upper", "literal", "escaped", "text", "id", "line_comment", "name" },
                SqlWithPseudoParameters)
            .ToArray();
        CollectionAssert.AreEqual(new[] { "real", "upper", "id", "name" }, filtered);

        var parameters = new Dictionary<string, object>
        {
            { "real", 1 },
            { "upper", 2 },
            { "literal", 3 },
            { "escaped", 4 },
            { "text", 5 },
            { "id", 6 },
            { "line_comment", 7 },
            { "name", 8 }
        };
        SqlParameterUtil.RemoveUnusedParameters(parameters, SqlWithPseudoParameters);

        CollectionAssert.AreEquivalent(new[] { "real", "upper", "id", "name" }, parameters.Keys.ToArray());
    }

    [TestMethod]
    public void ConvertParameterToDictionaryByPosition_SkipsPseudoParameters()
    {
        var sqlCommand = new DefaultSqlCommand(DatabaseType.Informix)
        {
            Sql = "SELECT @Second, '@Ignored', @First::int /* @Comment */",
            Parameter = new Dictionary<string, object>
            {
                { "First", 1 },
                { "Second", 2 },
                { "Ignored", 3 },
                { "Comment", 4 }
            }
        };

        sqlCommand.ConvertParameterToDictionaryByPosition(true);

        Assert.AreEqual("SELECT ?, '@Ignored', ?::int /* @Comment */", sqlCommand.Sql);
        var parameters = (Dictionary<string, object>)sqlCommand.Parameter;
        CollectionAssert.AreEqual(new[] { "1", "2" }, parameters.Keys.ToArray());
        CollectionAssert.AreEqual(new object[] { 2, 1 }, parameters.Values.ToArray());
    }

    [TestMethod]
    public void ConvertParameterToDictionaryByPosition_PreservesRepeatedParameters()
    {
        var sqlCommand = new DefaultSqlCommand(DatabaseType.Informix)
        {
            Sql = "SELECT @Value + @Value",
            Parameter = new Dictionary<string, object> { { "Value", 2 } }
        };

        sqlCommand.ConvertParameterToDictionaryByPosition(true);

        Assert.AreEqual("SELECT ? + ?", sqlCommand.Sql);
        var parameters = (Dictionary<string, object>)sqlCommand.Parameter;
        CollectionAssert.AreEqual(new[] { "1", "2" }, parameters.Keys.ToArray());
        CollectionAssert.AreEqual(new object[] { 2, 2 }, parameters.Values.ToArray());

        sqlCommand.ConvertSqlToNonParameter();
        Assert.AreEqual("SELECT 2 + 2", sqlCommand.Sql);
    }

    [TestMethod]
    public void ConvertSqlToNonParameter_ReplacesOnlyExecutableNamedParameters()
    {
        var sqlCommand = new DefaultSqlCommand(DatabaseType.PostgreSql)
        {
            Sql = "SELECT @Value, '@Value', @Value::text -- @Value",
            Parameter = new Dictionary<string, object>
            {
                { "Value", "O'Brien" }
            }
        };

        sqlCommand.ConvertSqlToNonParameter();

        Assert.AreEqual("SELECT 'O''Brien', '@Value', 'O''Brien'::text -- @Value", sqlCommand.Sql);
    }

    [TestMethod]
    public void ReplaceParameter_SkipsLiteralAndReplacesOnlyFirstExecutableOccurrence()
    {
        var sql = SqlParameterUtil.ReplaceParameter(
            "SELECT '@Id' AS LiteralValue, @Id AS FirstValue, @Id AS SecondValue",
            "Id",
            "1");

        Assert.AreEqual(
            "SELECT '@Id' AS LiteralValue, 1 AS FirstValue, @Id AS SecondValue",
            sql);
    }

    [TestMethod]
    public void ConvertSqlToNonParameter_UsesNumericPositionInsteadOfDictionaryOrder()
    {
        var sqlCommand = new DefaultSqlCommand(DatabaseType.Informix)
        {
            Sql = "SELECT ? AS FirstValue, '?' AS LiteralValue, ? AS SecondValue -- ?",
            Parameter = new Dictionary<string, object>
            {
                { "2", "second" },
                { "1", "first" }
            }
        };
        sqlCommand.ConvertSqlToUseQuestionMarkParameter();

        sqlCommand.ConvertSqlToNonParameter();

        Assert.AreEqual(
            "SELECT 'first' AS FirstValue, '?' AS LiteralValue, 'second' AS SecondValue -- ?",
            sqlCommand.Sql);
    }

    [TestMethod]
    public void ParseSqlParameters_HandlesPostgreSqlEscapeString()
    {
        const string sql = @"SELECT E'it\'s @fake' AS text_value FROM sample WHERE id = @real";

        var parameters = SqlParameterUtil.ParseSqlParameters(sql)
            .OrderBy(parameter => parameter.Value)
            .Select(parameter => parameter.Key)
            .ToArray();

        CollectionAssert.AreEqual(new[] { "real" }, parameters);

        var parameterValues = new Dictionary<string, object> { { "real", 1 } };
        SqlParameterUtil.RemoveUnusedParameters(parameterValues, sql);
        CollectionAssert.AreEqual(new[] { "real" }, parameterValues.Keys.ToArray());
    }

    [TestMethod]
    public void ConvertSqlToNonParameter_HandlesMySqlBackslashEscapedString()
    {
        var sqlCommand = new DefaultSqlCommand(DatabaseType.MySql)
        {
            Sql = @"SELECT 'it\'s @fake' AS text_value FROM sample WHERE id = @real",
            Parameter = new Dictionary<string, object> { { "real", 1 } }
        };

        sqlCommand.ConvertSqlToNonParameter();

        Assert.AreEqual(
            @"SELECT 'it\'s @fake' AS text_value FROM sample WHERE id = 1",
            sqlCommand.Sql);
    }

    [TestMethod]
    public void PublicParameterUtilities_DoNotRemoveRealParameterWhenDialectIsUnknown()
    {
        const string sql = @"SELECT 'it\'s @possible' AS text_value FROM sample WHERE id = @real";
        var parameters = new Dictionary<string, object> { { "real", 1 } };

        SqlParameterUtil.RemoveUnusedParameters(parameters, sql);

        // 未提供数据库类型时无法消除全部歧义，但必须保证字符串之后的真实参数不会被误删。
        CollectionAssert.AreEqual(new[] { "real" }, parameters.Keys.ToArray());
        Assert.AreEqual(
            @"SELECT 'it\'s @possible' AS text_value FROM sample WHERE id = 1",
            SqlParameterUtil.ReplaceParameter(sql, "real", "1"));
    }

    [TestMethod]
    public void ConvertSqlToNonParameter_KeepsAnsiBackslashBehaviorForSqlServer()
    {
        var sqlCommand = new DefaultSqlCommand(DatabaseType.SqlServer)
        {
            Sql = @"SELECT 'C:\' AS path_value, @real AS real_value",
            Parameter = new Dictionary<string, object> { { "real", 1 } }
        };

        sqlCommand.ConvertSqlToNonParameter();

        Assert.AreEqual(
            @"SELECT 'C:\' AS path_value, 1 AS real_value",
            sqlCommand.Sql);
    }

    [TestMethod]
    public void ParseSqlParameters_IgnoresPrefixesEmbeddedInIdentifiers()
    {
        var parameters = SqlParameterUtil.ParseSqlParameters(
                "SELECT FOO$BAR, value@part, :real FROM sample")
            .OrderBy(parameter => parameter.Value)
            .Select(parameter => parameter.Key)
            .ToArray();

        CollectionAssert.AreEqual(new[] { "real" }, parameters);
    }

    [TestMethod]
    public void ConvertSqlToNonParameter_HandlesOracleNonNestedBlockComment()
    {
        var sqlCommand = new DefaultSqlCommand(DatabaseType.Oracle)
        {
            Sql = "SELECT /* outer /* inner */ :real AS real_value FROM dual",
            Parameter = new Dictionary<string, object> { { "real", 1 } }
        };

        sqlCommand.ConvertSqlToNonParameter();

        Assert.AreEqual(
            "SELECT /* outer /* inner */ 1 AS real_value FROM dual",
            sqlCommand.Sql);
    }

    [TestMethod]
    public void ConvertSqlToNonParameter_HandlesPostgreSqlNestedBlockComment()
    {
        var sqlCommand = new DefaultSqlCommand(DatabaseType.PostgreSql)
        {
            Sql = "SELECT /* outer /* @inner */ @outer */ @real AS real_value",
            Parameter = new Dictionary<string, object> { { "real", 1 } }
        };

        sqlCommand.ConvertSqlToNonParameter();

        Assert.AreEqual(
            "SELECT /* outer /* @inner */ @outer */ 1 AS real_value",
            sqlCommand.Sql);
    }

    [TestMethod]
    public void ConvertSqlToNonParameter_HandlesMySqlCommentRules()
    {
        var sqlCommand = new DefaultSqlCommand(DatabaseType.MySql)
        {
            Sql = "SELECT 1--1 + @real # @fake\n, @second",
            Parameter = new Dictionary<string, object>
            {
                { "real", 2 },
                { "second", 3 }
            }
        };

        sqlCommand.ConvertSqlToNonParameter();

        Assert.AreEqual(
            "SELECT 1--1 + 2 # @fake\n, 3",
            sqlCommand.Sql);
    }

    [TestMethod]
    public void ConvertSqlToNonParameter_SkipsSqlServerBracketIdentifier()
    {
        var sqlCommand = new DefaultSqlCommand(DatabaseType.SqlServer)
        {
            Sql = "SELECT [Column@Fake], @real AS real_value",
            Parameter = new Dictionary<string, object> { { "real", 1 } }
        };

        sqlCommand.ConvertSqlToNonParameter();

        Assert.AreEqual(
            "SELECT [Column@Fake], 1 AS real_value",
            sqlCommand.Sql);
    }
}
