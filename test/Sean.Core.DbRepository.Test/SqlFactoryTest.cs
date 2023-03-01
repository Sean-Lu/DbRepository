using System;
using System.Collections.Generic;
using Example.Dapper.Core.Domain.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sean.Core.DbRepository.Test
{
    /// <summary>
    /// <see cref="SqlFactory"/> Test
    /// </summary>
    [TestClass]
    public class SqlFactoryTest : TestBase
    {
        [TestMethod]
        public void BulkInsertTest()
        {
            var list = new List<TestEntity>();
            for (int i = 0; i < 10; i++)
            {
                list.Add(new TestEntity
                {
                    Id = i + 1,
                    UserId = 10000 + i,
                    UserName = "TestName",
                    Country = CountryType.China,
                    IsVip = true,
                    AccountBalance = 99.92M,
                    Remark = "Test12'3",
                    CreateTime = DateTime.Now
                });
            }

            //BaseSqlBuilder.SqlIndented = true;
            //BaseSqlBuilder.SqlParameterized = false;

            ISqlCommand sqlCommand = SqlFactory<TestEntity>.CreateInsertableBuilder(DatabaseType.MySql, true)
                .SetParameter(list)
                .Build();

            Console.WriteLine(sqlCommand.Sql);
        }

        [TestMethod]
        public void BulkInsertOrUpdateTest()
        {
            var list = new List<TestEntity>();
            for (int i = 0; i < 10; i++)
            {
                list.Add(new TestEntity
                {
                    Id = i + 1,
                    UserId = 10000 + i,
                    UserName = "TestName",
                    Country = CountryType.China,
                    IsVip = true,
                    AccountBalance = 99.92M,
                    Remark = "Test12'3",
                    CreateTime = DateTime.Now
                });
            }

            //BaseSqlBuilder.SqlIndented = true;
            //BaseSqlBuilder.SqlParameterized = false;

            ISqlCommand sqlCommand = SqlFactory<TestEntity>.CreateReplaceableBuilder(DatabaseType.MySql, true)
                .SetParameter(list)
                .Build();

            Console.WriteLine(sqlCommand.Sql);
        }
    }
}
