using System;
using System.Collections.Generic;
using System.Text;
using Example.Domain.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sean.Core.DbRepository.Test
{
    [TestClass]
    public class TableTest
    {
        [TestMethod]
        public void ValidateField()
        {
            var fieldName = Table<TestEntity>.Field(entity => entity.UserName);
            Assert.AreEqual(fieldName, "UserName");

            var fieldName2 = Table<TestEntity>.Field(entity => entity.AccountBalance);
            Assert.AreEqual(fieldName2, "AccountBalance");
        }

        [TestMethod]
        public void ValidateTableName()
        {
            var tableName = Table<TestEntity>.TableName();
            Assert.AreEqual(tableName, "Test");
        }

        [TestMethod]
        public void ValidateSqlWhereClause()
        {
            var sqlWhereClause = Table<TestEntity>.SqlWhereClause(DatabaseType.MySql, entity => entity.IsVip);
            Assert.AreEqual(sqlWhereClause, "`IsVip` = @IsVip");
        }
    }
}
