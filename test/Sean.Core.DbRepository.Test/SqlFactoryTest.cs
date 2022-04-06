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

            var sqlFactory = SqlFactory<TestEntity>.Create(DatabaseType.MySql, true)
                .BulkInsert(list)
                .BuildInsertableSql();

            var sql = sqlFactory.InsertSql;
            var param = sqlFactory.Parameter;
        }
    }
}
