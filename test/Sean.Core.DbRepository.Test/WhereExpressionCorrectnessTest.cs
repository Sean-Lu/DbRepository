using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Example.Dapper.Core.Domain.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository.Test;

/// <summary>
/// 表达式解析层正确性测试：
/// 缺失 throw、参数匹配、StringComparison 重载、空集合 IN、LIKE 通配符转义。
/// </summary>
[TestClass]
public class WhereExpressionCorrectnessTest : TestBase
{
    private readonly ISqlAdapter _sqlAdapter;

    public WhereExpressionCorrectnessTest()
    {
        _sqlAdapter = new DefaultSqlAdapter(DatabaseType.MySql, null);
    }

    #region ConstantExtractor 缺失 throw
    /// <summary>
    /// 常量位置不支持的二元表达式（例如 Add）应当抛出异常，
    /// 而不是静默把异常对象当作参数值。
    /// </summary>
    [TestMethod]
    public void ValidateUnsupportedBinaryConstantThrows()
    {
        var x = 10;
        var y = 8;
        Expression<Func<TestEntity, bool>> whereExpression = entity => entity.Age > x + y;
        Assert.Throws<NotSupportedException>(() =>
        {
            whereExpression.GetParameterizedWhereClause(_sqlAdapter, out _);
        });
    }

    /// <summary>
    /// 常量位置的数组索引仍然支持。
    /// </summary>
    [TestMethod]
    public void ValidateArrayIndexConstant()
    {
        var ids = new long[] { 10001L, 10002L };
        Expression<Func<TestEntity, bool>> whereExpression = entity => entity.UserId == ids[1];
        var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
        var expectedParameters = new Dictionary<string, object>
        {
            { "UserId", 10002L }
        };
        Assert.AreEqual("`UserId` = @UserId", whereClause);
        AssertSqlParameters(expectedParameters, parameters);
    }
    #endregion

    #region 参数匹配改用 ReferenceEquals
    /// <summary>
    /// 组合表达式（通过 AndAlso 合并表达式）在参数匹配改用 ReferenceEquals 后仍能正常解析。
    /// </summary>
    [TestMethod]
    public void ValidateComposedExpressionParameterMatch()
    {
        Expression<Func<TestEntity, bool>> left = entity => entity.UserId == 10001L;
        Expression<Func<TestEntity, bool>> right = entity => entity.Age >= 18;
        var composed = left.AndAlso(right);
        var whereClause = composed.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
        var expectedParameters = new Dictionary<string, object>
        {
            { "UserId", 10001L },
            { "Age", 18 }
        };
        Assert.AreEqual("`UserId` = @UserId AND `Age` >= @Age", whereClause);
        AssertSqlParameters(expectedParameters, parameters);
    }
    #endregion

    #region 带 StringComparison 的字符串方法重载必须抛出
    /// <summary>
    /// 带 StringComparison 参数的字符串方法重载应当抛出异常，而不是被静默忽略。
    /// </summary>
    [TestMethod]
    public void ValidateStringComparisonOverloadThrows()
    {
        Expression<Func<TestEntity, bool>> whereExpression = entity => entity.Remark.Contains("A", StringComparison.OrdinalIgnoreCase);
        Assert.Throws<NotSupportedException>(() =>
        {
            whereExpression.GetParameterizedWhereClause(_sqlAdapter, out _);
        });
    }

    /// <summary>
    /// string.Equals(string, StringComparison) 重载同样必须抛出异常。
    /// </summary>
    [TestMethod]
    public void ValidateStringComparisonEqualsOverloadThrows()
    {
        Expression<Func<TestEntity, bool>> whereExpression = entity => entity.Remark.Equals("A", StringComparison.OrdinalIgnoreCase);
        Assert.Throws<NotSupportedException>(() =>
        {
            whereExpression.GetParameterizedWhereClause(_sqlAdapter, out _);
        });
    }
    #endregion

    #region 空集合 IN/NOT IN
    /// <summary>
    /// IN 空集合生成 "1=0"（无匹配），而不是生成非法 SQL。
    /// </summary>
    [TestMethod]
    public void ValidateEmptyCollectionIn()
    {
        var ids = new List<long>();
        Expression<Func<TestEntity, bool>> whereExpression = entity => ids.Contains(entity.UserId);
        var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
        Assert.AreEqual("1=0", whereClause);
        Assert.AreEqual(0, parameters.Count);
    }

    /// <summary>
    /// NOT IN 空集合生成 "1=1"（全部匹配）。
    /// </summary>
    [TestMethod]
    public void ValidateEmptyCollectionNotIn()
    {
        var ids = new long[0];
        Expression<Func<TestEntity, bool>> whereExpression = entity => !ids.Contains(entity.UserId);
        var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
        Assert.AreEqual("1=1", whereClause);
        Assert.AreEqual(0, parameters.Count);
    }

    /// <summary>
    /// 空 IN 通过 AndAlso 与其他条件组合。
    /// </summary>
    [TestMethod]
    public void ValidateEmptyCollectionInWithAndAlso()
    {
        var ids = new List<long>();
        Expression<Func<TestEntity, bool>> whereExpression = entity => ids.Contains(entity.UserId) && entity.Age >= 18;
        var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
        var expectedParameters = new Dictionary<string, object>
        {
            { "Age", 18 }
        };
        Assert.AreEqual("1=0 AND `Age` >= @Age", whereClause);
        AssertSqlParameters(expectedParameters, parameters);
    }

    /// <summary>
    /// 非空集合的 IN 保持原有行为。
    /// </summary>
    [TestMethod]
    public void ValidateNonEmptyCollectionIn()
    {
        var ids = new List<long> { 10001L, 10002L };
        Expression<Func<TestEntity, bool>> whereExpression = entity => ids.Contains(entity.UserId);
        var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
        var expectedParameters = new Dictionary<string, object>
        {
            { "UserId", ids }
        };
        Assert.AreEqual("`UserId` IN @UserId", whereClause);
        AssertSqlParameters(expectedParameters, parameters);
    }

    /// <summary>
    /// 惰性枚举的 IN 不应因空集合探测而丢失第一个元素。
    /// </summary>
    [TestMethod]
    public void ValidateLazyEnumerableInPreservesFirstElement()
    {
        IEnumerable<long> ids = GetLazyIds();
        Expression<Func<TestEntity, bool>> whereExpression = entity => ids.Contains(entity.UserId);
        var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);

        Assert.AreEqual("`UserId` IN @UserId", whereClause);
        CollectionAssert.AreEqual(
            new object[] { 10001L, 10002L },
            ((List<object>)parameters["UserId"]).ToArray());
    }

    private static IEnumerable<long> GetLazyIds()
    {
        yield return 10001L;
        yield return 10002L;
    }
    #endregion

    #region LIKE 通配符转义
    /// <summary>
    /// LIKE 值中包含 '%' 时，使用 ESCAPE '/' 进行转义。
    /// </summary>
    [TestMethod]
    public void ValidateLikeEscapePercent()
    {
        Expression<Func<TestEntity, bool>> whereExpression = entity => entity.Remark.Contains("50%");
        var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
        var expectedParameters = new Dictionary<string, object>
        {
            { "Remark", "%50/%%" }
        };
        Assert.AreEqual("`Remark` LIKE @Remark ESCAPE '/'", whereClause);
        AssertSqlParameters(expectedParameters, parameters);
    }

    /// <summary>
    /// LIKE 值中包含 '_' 时，使用 ESCAPE '/' 进行转义。
    /// </summary>
    [TestMethod]
    public void ValidateLikeEscapeUnderscore()
    {
        Expression<Func<TestEntity, bool>> whereExpression = entity => entity.Remark.StartsWith("a_b");
        var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
        var expectedParameters = new Dictionary<string, object>
        {
            { "Remark", "a/_b%" }
        };
        Assert.AreEqual("`Remark` LIKE @Remark ESCAPE '/'", whereClause);
        AssertSqlParameters(expectedParameters, parameters);
    }

    /// <summary>
    /// LIKE 值中不含通配符时，保持原有 SQL 形态（不追加 ESCAPE 子句）。
    /// </summary>
    [TestMethod]
    public void ValidateLikeNoWildcardNoEscape()
    {
        Expression<Func<TestEntity, bool>> whereExpression = entity => entity.Remark.Contains("abc");
        var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
        var expectedParameters = new Dictionary<string, object>
        {
            { "Remark", "%abc%" }
        };
        Assert.AreEqual("`Remark` LIKE @Remark", whereClause);
        AssertSqlParameters(expectedParameters, parameters);
    }

    /// <summary>
    /// SqlServer：'[' 同样是通配符，必须进行转义。
    /// </summary>
    [TestMethod]
    public void ValidateLikeEscapeSqlServerBracket()
    {
        var sqlServerAdapter = new DefaultSqlAdapter(DatabaseType.SqlServer, null);
        Expression<Func<TestEntity, bool>> whereExpression = entity => entity.Remark.Contains("a[b");
        var whereClause = whereExpression.GetParameterizedWhereClause(sqlServerAdapter, out var parameters);
        var expectedParameters = new Dictionary<string, object>
        {
            { "Remark", "%a/[b%" }
        };
        Assert.AreEqual("[Remark] LIKE @Remark ESCAPE '/'", whereClause);
        AssertSqlParameters(expectedParameters, parameters);
    }

    /// <summary>
    /// QuestDB uses an implicit backslash escape character and rejects the ESCAPE clause.
    /// </summary>
    [TestMethod]
    public void ValidateLikeEscapeQuestDbPercent()
    {
        var questDbAdapter = new DefaultSqlAdapter(DatabaseType.QuestDB, null);
        Expression<Func<TestEntity, bool>> whereExpression = entity => entity.Remark.Contains("50%");
        var whereClause = whereExpression.GetParameterizedWhereClause(questDbAdapter, out var parameters);
        var expectedParameters = new Dictionary<string, object>
        {
            { "Remark", "%50\\%%" }
        };
        Assert.AreEqual("\"Remark\" LIKE @Remark", whereClause);
        AssertSqlParameters(expectedParameters, parameters);
    }

    /// <summary>
    /// A literal backslash must be doubled for QuestDB even when no wildcard is present.
    /// </summary>
    [TestMethod]
    public void ValidateLikeEscapeQuestDbBackslash()
    {
        var questDbAdapter = new DefaultSqlAdapter(DatabaseType.QuestDB, null);
        Expression<Func<TestEntity, bool>> whereExpression = entity => entity.Remark.Contains("a\\b");
        var whereClause = whereExpression.GetParameterizedWhereClause(questDbAdapter, out var parameters);
        var expectedParameters = new Dictionary<string, object>
        {
            { "Remark", "%a\\\\b%" }
        };
        Assert.AreEqual("\"Remark\" LIKE @Remark", whereClause);
        AssertSqlParameters(expectedParameters, parameters);
    }

    /// <summary>
    /// 转义字符 '/' 本身仅在值含通配符时才被转义。
    /// </summary>
    [TestMethod]
    public void ValidateLikeEscapeSlashWithWildcard()
    {
        Expression<Func<TestEntity, bool>> whereExpression = entity => entity.Remark.Contains("a/b%");
        var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
        var expectedParameters = new Dictionary<string, object>
        {
            { "Remark", "%a//b/%%" }
        };
        Assert.AreEqual("`Remark` LIKE @Remark ESCAPE '/'", whereClause);
        AssertSqlParameters(expectedParameters, parameters);
    }

    /// <summary>
    /// NOT LIKE 且值含通配符时同样追加 ESCAPE 子句。
    /// </summary>
    [TestMethod]
    public void ValidateNotLikeEscape()
    {
        Expression<Func<TestEntity, bool>> whereExpression = entity => !entity.Remark.EndsWith("100%");
        var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
        var expectedParameters = new Dictionary<string, object>
        {
            { "Remark", "%100/%" }
        };
        Assert.AreEqual("`Remark` NOT LIKE @Remark ESCAPE '/'", whereClause);
        AssertSqlParameters(expectedParameters, parameters);
    }

    /// <summary>
    /// Equals 不是 LIKE 条件：值中包含 '%' 时不进行转义。
    /// </summary>
    [TestMethod]
    public void ValidateEqualsNotEscaped()
    {
        Expression<Func<TestEntity, bool>> whereExpression = entity => entity.Remark.Equals("50%");
        var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
        var expectedParameters = new Dictionary<string, object>
        {
            { "Remark", "50%" }
        };
        Assert.AreEqual("`Remark` = @Remark", whereClause);
        AssertSqlParameters(expectedParameters, parameters);
    }
    #endregion
}
