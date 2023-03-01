using System.Collections.Generic;
using Example.Dapper.Core.Domain.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sean.Core.DbRepository.Test
{
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
            Assert.AreEqual(tableName, "Test");
        }

        [TestMethod]
        public void ValidateTableName2()
        {
            var tableName = Table<TestEntity>.Create(DatabaseType.MySql).GetTableName();
            Assert.AreEqual(tableName, "`Test`");
        }

        [TestMethod]
        public void ValidateField()
        {
            var fieldName = Table<TestEntity>.Field(entity => entity.UserName);
            Assert.AreEqual(fieldName, "UserName");

            var fieldName2 = Table<TestEntity>.Field(entity => entity.AccountBalance);
            Assert.AreEqual(fieldName2, "AccountBalance");
        }

        [TestMethod]
        public void ValidateField2()
        {
            var fieldName = Table<TestEntity>.Create(DatabaseType.MySql).GetField(entity => entity.UserName);
            Assert.AreEqual(fieldName, "`UserName`");

            var fieldName2 = Table<TestEntity>.Create(DatabaseType.MySql).GetField(entity => entity.AccountBalance);
            Assert.AreEqual(fieldName2, "`AccountBalance`");
        }

        [TestMethod]
        public void ValidateFieldWithTableName()
        {
            var fieldName = Table<TestEntity>.FieldWithTableName(entity => entity.UserName);
            Assert.AreEqual(fieldName, "Test.UserName");

            var fieldName2 = Table<TestEntity>.FieldWithTableName(entity => entity.AccountBalance);
            Assert.AreEqual(fieldName2, "Test.AccountBalance");
        }

        [TestMethod]
        public void ValidateFieldWithTableName2()
        {
            var fieldName = Table<TestEntity>.Create(DatabaseType.MySql).GetFieldWithTableName(entity => entity.UserName);
            Assert.AreEqual(fieldName, "`Test`.`UserName`");

            var fieldName2 = Table<TestEntity>.Create(DatabaseType.MySql).GetFieldWithTableName(entity => entity.AccountBalance);
            Assert.AreEqual(fieldName2, "`Test`.`AccountBalance`");
        }

        [TestMethod]
        public void ValidateSqlWhereClause()
        {
            var sqlWhereClause = Table<TestEntity>.Create(DatabaseType.MySql).GetParameterizedWhereClause(entity => entity.IsVip && entity.Age > 18, out var parameters);
            Assert.AreEqual(sqlWhereClause, "`IsVip` = @IsVip AND `Age` > @Age");
            var expectedParameters = new Dictionary<string, object>
            {
                { "IsVip", true },
                { "Age", 18 }
            };
            AssertSqlParameters(expectedParameters, parameters);
        }
    }
}
