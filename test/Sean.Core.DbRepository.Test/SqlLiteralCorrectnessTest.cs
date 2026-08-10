using System;
using System.Globalization;
using Example.Dapper.Core.Domain.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository.Test;

/// <summary>
/// SQL 字面量格式化与文化相关正确性测试（通过公开 API 间接验证 ConvertToSqlString 的行为）。
/// </summary>
[TestClass]
public class SqlLiteralCorrectnessTest : TestBase
{
    /// <summary>
    /// 在当前文化为阿拉伯语时，非参数化 SQL 中的 DateTime 字面量仍使用西方数字与固定格式。
    /// </summary>
    [TestMethod]
    public void NonParameterizedSql_DateTime_ArabicCulture()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("ar-SA");
            var dt = new DateTime(2026, 7, 28, 14, 30, 45);
            var sqlCommand = SqlFactory.CreateWhereClauseBuilder<TestEntity>(DatabaseType.MySql)
                .Where(entity => entity.CreateTime > dt)
                .SetSqlParameterized(false)
                .Build();
            Assert.IsTrue(sqlCommand.Sql.Contains("'2026-07-28 14:30:45'"), $"SQL 应包含西方数字 DateTime：{sqlCommand.Sql}");
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    /// <summary>
    /// 在当前文化为法语（逗号作小数分隔符）时，非参数化 SQL 中的 decimal 字面量仍使用 '.' 小数点。
    /// </summary>
    [TestMethod]
    public void NonParameterizedSql_Decimal_FrenchCulture()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            var dec = 1234.56m;
            var sqlCommand = SqlFactory.CreateWhereClauseBuilder<TestEntity>(DatabaseType.MySql)
                .Where(entity => entity.AccountBalance > dec)
                .SetSqlParameterized(false)
                .Build();
            Assert.IsFalse(sqlCommand.Sql.Contains(','), $"SQL 中 decimal 字面量不应包含逗号：{sqlCommand.Sql}");
            Assert.IsTrue(sqlCommand.Sql.Contains("1234.56"), $"SQL 应包含 1234.56：{sqlCommand.Sql}");
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    /// <summary>
    /// 非参数化 SQL 中字符串的单引号必须使用 ANSI SQL 标准（双单引号）转义，而不是反斜杠。
    /// </summary>
    [TestMethod]
    public void NonParameterizedSql_StringQuotesEscapedByDoubling()
    {
        var databaseTypes = new[]
        {
            DatabaseType.MySql,
            DatabaseType.SqlServer,
            DatabaseType.Oracle,
            DatabaseType.PostgreSql,
            DatabaseType.SQLite
        };
        foreach (var dbType in databaseTypes)
        {
            var sqlCommand = SqlFactory.CreateWhereClauseBuilder<TestEntity>(dbType)
                .Where(entity => entity.Remark == "O'Brien")
                .SetSqlParameterized(false)
                .Build();
            Assert.IsTrue(sqlCommand.Sql.Contains("'O''Brien'"), $"{dbType} SQL 应包含双单引号转义：{sqlCommand.Sql}");
            Assert.IsFalse(sqlCommand.Sql.Contains("\\'"), $"{dbType} SQL 不应包含反斜杠转义：{sqlCommand.Sql}");
        }
    }

    /// <summary>
    /// MySQL 字符串中反斜杠紧邻单引号时，不得让反斜杠出现在引号字面量内并破坏边界。
    /// </summary>
    [TestMethod]
    public void NonParameterizedSql_MySqlBackslashBeforeQuoteCannotEscapeLiteral()
    {
        var databaseTypes = new[]
        {
            DatabaseType.MySql,
            DatabaseType.MariaDB,
            DatabaseType.TiDB,
            DatabaseType.OceanBase
        };
        foreach (var dbType in databaseTypes)
        {
            var sqlCommand = SqlFactory.CreateWhereClauseBuilder<TestEntity>(dbType)
                .Where(entity => entity.Remark == "x\\'; DROP TABLE users; --")
                .SetSqlParameterized(false)
                .Build();
            Assert.IsTrue(sqlCommand.Sql.Contains("CONCAT('x',CHAR(92),'''; DROP TABLE users; --')"),
                $"{dbType} 应使用 CHAR(92) 将反斜杠移出引号字面量：{sqlCommand.Sql}");
            Assert.IsFalse(sqlCommand.Sql.Contains("\\"), $"{dbType} SQL 引号字面量不应包含原始反斜杠：{sqlCommand.Sql}");
        }
    }

    /// <summary>
    /// 非参数化 SQL 中 int 枚举被正确转换为整数（回归验证）。
    /// </summary>
    [TestMethod]
    public void NonParameterizedSql_IntEnumToNumber()
    {
        var sqlCommand = SqlFactory.CreateWhereClauseBuilder<TestEntity>(DatabaseType.MySql)
            .Where(entity => entity.Country == CountryType.China)
            .SetSqlParameterized(false)
            .Build();
        Assert.IsTrue(sqlCommand.Sql.Contains("= 1") || sqlCommand.Sql.Contains("=1"),
            $"SQL 应将 CountryType.China 转为 1：{sqlCommand.Sql}");
    }

    /// <summary>
    /// long 基类型的负值枚举应使用 InvariantCulture 输出标准负号。
    /// </summary>
    [TestMethod]
    public void NonParameterizedSql_LongEnumUsesInvariantCulture()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            var customCulture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            customCulture.NumberFormat.NegativeSign = "NEG";
            CultureInfo.CurrentCulture = customCulture;
            var value = LongEnumType.Negative;
            var sqlCommand = SqlFactory.CreateWhereClauseBuilder<LongEnumEntity>(DatabaseType.MySql)
                .Where(entity => entity.Value == value)
                .SetSqlParameterized(false)
                .Build();
            Assert.IsTrue(sqlCommand.Sql.Contains("-1"), $"SQL 应包含标准负数 -1：{sqlCommand.Sql}");
            Assert.IsFalse(sqlCommand.Sql.Contains("NEG"), $"SQL 不应使用当前文化的负号：{sqlCommand.Sql}");
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    /// <summary>
    /// 在当前文化为法语时，UpdateableBuilder 中 IncrementFields 生成的 SQL 数值字面量仍使用 InvariantCulture。
    /// </summary>
    [TestMethod]
    public void IncrementFields_FrenchCulture_NumericLiteral()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            var testEntity = new TestEntity { Id = 1L };
            var sqlCommand = SqlFactory.CreateUpdateableBuilder<TestEntity>(DatabaseType.MySql)
                .IncrementFields(entity => entity.AccountBalance, 1.5m)
                .IgnoreFields(entity => new { entity.UserId, entity.UserName, entity.Age, entity.Sex, entity.PhoneNumber, entity.Email, entity.IsVip, entity.IsBlack, entity.Country, entity.AccountBalance2, entity.Status, entity.Remark, entity.CreateTime, entity.UpdateTime })
                .SetParameter(testEntity)
                .Build();
            Assert.IsTrue(sqlCommand.Sql.Contains("+ 1.5"), $"数值字面量应为 1.5：{sqlCommand.Sql}");
            Assert.IsFalse(sqlCommand.Sql.Contains(",5") || sqlCommand.Sql.Contains(", 5"),
                $"数值字面量不应包含逗号小数：{sqlCommand.Sql}");
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    /// <summary>
    /// 在当前文化为法语时，UpdateableBuilder 中 DecrementFields 生成的 SQL 数值字面量仍使用 InvariantCulture。
    /// </summary>
    [TestMethod]
    public void DecrementFields_FrenchCulture_NumericLiteral()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            var testEntity = new TestEntity { Id = 1L };
            var sqlCommand = SqlFactory.CreateUpdateableBuilder<TestEntity>(DatabaseType.MySql)
                .DecrementFields(entity => entity.AccountBalance, 0.25m)
                .IgnoreFields(entity => new { entity.UserId, entity.UserName, entity.Age, entity.Sex, entity.PhoneNumber, entity.Email, entity.IsVip, entity.IsBlack, entity.Country, entity.AccountBalance2, entity.Status, entity.Remark, entity.CreateTime, entity.UpdateTime })
                .SetParameter(testEntity)
                .Build();
            Assert.IsTrue(sqlCommand.Sql.Contains("- 0.25"), $"数值字面量应为 0.25：{sqlCommand.Sql}");
            Assert.IsFalse(sqlCommand.Sql.Contains(",25") || sqlCommand.Sql.Contains(", 25"),
                $"数值字面量不应包含逗号小数：{sqlCommand.Sql}");
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    public class LongEnumEntity
    {
        public LongEnumType Value { get; set; }
    }

    public enum LongEnumType : long
    {
        Negative = -1
    }
}
