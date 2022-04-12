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

            IInsertableSql insertableSql = SqlFactory<TestEntity>.CreateInsertableBuilder(DatabaseType.MySql, true)
                .SetParameter(list)// BulkInsert
                .Build();

            var sql = insertableSql.Sql;
            var param = insertableSql.Parameter;
        }

        [TestMethod]
        public void ReplaceTest()
        {
            var list = new List<TestEntity>();
            for (int i = 0; i < 5; i++)
            {
                list.Add(new TestEntity());
            }

            IReplaceableSql replaceableSql = SqlFactory<TestEntity>.CreateReplaceableBuilder(DatabaseType.MySql, true)
                .SetParameter(list)
                .Build();

            var sql = replaceableSql.Sql;
            var param = replaceableSql.Parameter;
        }
    }
}
