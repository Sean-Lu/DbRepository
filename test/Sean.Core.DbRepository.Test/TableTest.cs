using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sean.Core.DbRepository.Test;

/// <summary>
/// Test for <see cref="Table{TEntity}"/>.
/// </summary>
[TestClass]
public class TableTest : TestBase
{
    [TestMethod]
    public void ValidateTableName()
    {
        var tableName = Table<TestEntity>.TableName();
        Assert.AreEqual("Test", tableName);
    }

    [TestMethod]
    public void ValidateTableName2()
    {
        var tableName = Table<TestEntity>.Create(DatabaseType.MySql).GetTableName();
        Assert.AreEqual("`Test`", tableName);
    }

    [TestMethod]
    public void ValidateField()
    {
        var fieldName = Table<TestEntity>.Field(entity => entity.UserName);
        Assert.AreEqual("UserName", fieldName);

        var fieldName2 = Table<TestEntity>.Field(entity => entity.AccountBalance);
        Assert.AreEqual("AccountBalance", fieldName2);
    }

    [TestMethod]
    public void ValidateField2()
    {
        var fieldName = Table<TestEntity>.Create(DatabaseType.MySql).GetField(entity => entity.UserName);
        Assert.AreEqual("`UserName`", fieldName);

        var fieldName2 = Table<TestEntity>.Create(DatabaseType.MySql).GetField(entity => entity.AccountBalance);
        Assert.AreEqual("`AccountBalance`", fieldName2);
    }

    [TestMethod]
    public void ValidateFieldWithTableName()
    {
        var fieldName = Table<TestEntity>.FieldWithTableName(entity => entity.UserName);
        Assert.AreEqual("Test.UserName", fieldName);

        var fieldName2 = Table<TestEntity>.FieldWithTableName(entity => entity.AccountBalance);
        Assert.AreEqual("Test.AccountBalance", fieldName2);
    }

    [TestMethod]
    public void ValidateFieldWithTableName2()
    {
        var fieldName = Table<TestEntity>.Create(DatabaseType.MySql).GetFieldWithTableName(entity => entity.UserName);
        Assert.AreEqual("`Test`.`UserName`", fieldName);

        var fieldName2 = Table<TestEntity>.Create(DatabaseType.MySql).GetFieldWithTableName(entity => entity.AccountBalance);
        Assert.AreEqual("`Test`.`AccountBalance`", fieldName2);
    }

    [TestMethod]
    public void ValidateSqlWhereClause()
    {
        var sqlWhereClause = Table<TestEntity>.Create(DatabaseType.MySql).GetParameterizedWhereClause(entity => entity.IsVip && entity.Age > 18, out var parameters);
        Assert.AreEqual("`IsVip` = @IsVip AND `Age` > @Age", sqlWhereClause);
        var expectedParameters = new Dictionary<string, object>
        {
            { "IsVip", true },
            { "Age", 18 }
        };
        AssertSqlParameters(expectedParameters, parameters);
    }
}
