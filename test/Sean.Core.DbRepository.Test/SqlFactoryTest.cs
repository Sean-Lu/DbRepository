using System.Collections.Generic;
using Example.Domain.Entities;
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
            for (int i = 0; i < 200; i++)
            {
                list.Add(new TestEntity());
            }

            IInsertableSql insertableSql = SqlFactory<TestEntity>.CreateInsertable(DatabaseType.MySql, true)
                .BulkInsert(list)
                .Build();

            var sql = insertableSql.InsertSql;
            var param = insertableSql.Parameter;
        }
    }
}
