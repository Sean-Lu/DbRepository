using Example.Dapper.Core.Domain.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sean.Core.DbRepository.Test
{
    [TestClass]
    public class OrderByClauseSqlBuilderTest : TestBase
    {
        private readonly ISqlAdapter _sqlAdapter;

        public OrderByClauseSqlBuilderTest()
        {
            _sqlAdapter = new DefaultSqlAdapter<TestEntity>(DatabaseType.MySql);
        }

        [TestMethod]
        public void ValidateSingleTable()
        {
            var sqlCommand = OrderByClauseSqlBuilder<TestEntity>.Create(_sqlAdapter)
                .OrderBy(OrderByType.Desc, entity => entity.CreateTime)
                .OrderBy(OrderByType.Desc, entity => entity.Id)
                .Build();
            var whereClause = sqlCommand.Sql;
            Assert.AreEqual("`CreateTime` DESC, `Id` DESC", whereClause);
        }

        [TestMethod]
        public void ValidateSingleTable2()
        {
            var sqlCommand = OrderByClauseSqlBuilder<TestEntity>.Create(_sqlAdapter)
                .OrderBy(OrderByType.Desc, entity => new { entity.CreateTime, entity.UserId })
                .OrderBy(OrderByType.Desc, entity => entity.Id)
                .Build();
            var whereClause = sqlCommand.Sql;
            Assert.AreEqual("`CreateTime`, `UserId` DESC, `Id` DESC", whereClause);
        }

        [TestMethod]
        public void ValidateMultiTable()
        {
            var sqlCommand = OrderByClauseSqlBuilder<TestEntity>.Create(_sqlAdapter)
                .OrderBy(OrderByType.Desc, entity => entity.CreateTime)
                .OrderBy(OrderByType.Desc, entity => entity.Id)
                .IsMultiTable(true)
                .Build();
            var whereClause = sqlCommand.Sql;
            Assert.AreEqual("`Test`.`CreateTime` DESC, `Test`.`Id` DESC", whereClause);
        }

        [TestMethod]
        public void ValidateMultiTable2()
        {
            var sqlCommand = OrderByClauseSqlBuilder<TestEntity>.Create(_sqlAdapter)
                .OrderBy(OrderByType.Desc, entity => entity.CreateTime)
                .OrderBy<CheckInLogEntity>(OrderByType.Desc, entity => entity.Id)
                .Build();
            var whereClause = sqlCommand.Sql;
            Assert.AreEqual("`Test`.`CreateTime` DESC, `CheckInLog`.`Id` DESC", whereClause);
        }
    }
}
