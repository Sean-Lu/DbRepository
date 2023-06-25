using Example.Dapper.Core.Domain.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sean.Core.DbRepository.Test
{
    /// <summary>
    /// XXXSqlBuilder Test
    /// </summary>
    [TestClass]
    public class SqlBuilderTest : TestBase
    {
        private readonly SqlFactory _sqlFactory;

        public SqlBuilderTest()
        {
            _sqlFactory = new SqlFactory(DatabaseType.MySql);
        }

        [TestMethod]
        public void ValidateWhere()
        {
            var sqlCommand = _sqlFactory.CreateQueryableBuilder<TestEntity>(false)
                .SelectFields(entity => entity.UserId)
                .Where(entity => entity.Status == 1 && entity.IsVip && entity.Age > 18)
                .Build();
            var sql = sqlCommand.Sql;
            var expectedSql = "SELECT `UserId` FROM `Test` WHERE `Status` = @Status AND `IsVip` = @IsVip AND `Age` > @Age";
            Assert.IsTrue(expectedSql == sql);
        }

        [TestMethod]
        public void ValidateMultiWhere()
        {
            var sqlCommand = _sqlFactory.CreateQueryableBuilder<TestEntity>(false)
                .SelectFields(entity => entity.UserId)
                .Where(entity => entity.Status == 1)
                .Where(entity => entity.IsVip && entity.Age > 18)
                .Build();
            var sql = sqlCommand.Sql;
            var expectedSql = "SELECT `UserId` FROM `Test` WHERE `Status` = @Status AND `IsVip` = @IsVip AND `Age` > @Age";
            Assert.IsTrue(expectedSql == sql);
        }

        [TestMethod]
        public void ValidateWhereIFTrue()
        {
            var condition = true;
            var sqlCommand = _sqlFactory.CreateQueryableBuilder<TestEntity>(false)
                .SelectFields(entity => entity.UserId)
                .Where(entity => entity.Status == 1)
                .WhereIF(condition, entity => entity.IsVip && entity.Age > 18)
                .Build();
            var sql = sqlCommand.Sql;
            var expectedSql = "SELECT `UserId` FROM `Test` WHERE `Status` = @Status AND `IsVip` = @IsVip AND `Age` > @Age";
            Assert.IsTrue(expectedSql == sql);
        }
        [TestMethod]
        public void ValidateWhereIFFalse()
        {
            var condition = false;
            var sqlCommand = _sqlFactory.CreateQueryableBuilder<TestEntity>(false)
                .SelectFields(entity => entity.UserId)
                .Where(entity => entity.Status == 1)
                .WhereIF(condition, entity => entity.IsVip && entity.Age > 18)
                .Build();
            var sql = sqlCommand.Sql;
            var expectedSql = "SELECT `UserId` FROM `Test` WHERE `Status` = @Status";
            Assert.IsTrue(expectedSql == sql);
        }

        [TestMethod]
        public void ValidateMaxField()
        {
            var sqlCommand = _sqlFactory.CreateQueryableBuilder<TestEntity>(false)
                .SelectFields(entity => entity.UserId)
                .MaxField(entity => entity.AccountBalance, "MaxValue")
                .Where(entity => entity.Status == 1)
                .GroupByField(entity => entity.UserId)
                .Build();
            var sql = sqlCommand.Sql;
            var expectedSql = "SELECT `UserId`, MAX(`AccountBalance`) AS MaxValue FROM `Test` WHERE `Status` = @Status GROUP BY `UserId`";
            Assert.IsTrue(expectedSql == sql);
        }

        [TestMethod]
        public void ValidateMinField()
        {
            var sqlCommand = _sqlFactory.CreateQueryableBuilder<TestEntity>(false)
                .SelectFields(entity => entity.UserId)
                .MinField(entity => entity.AccountBalance, "MinValue")
                .Where(entity => entity.Status == 1)
                .GroupByField(entity => entity.UserId)
                .Build();
            var sql = sqlCommand.Sql;
            var expectedSql = "SELECT `UserId`, MIN(`AccountBalance`) AS MinValue FROM `Test` WHERE `Status` = @Status GROUP BY `UserId`";
            Assert.IsTrue(expectedSql == sql);
        }

        [TestMethod]
        public void ValidateSumField()
        {
            var sqlCommand = _sqlFactory.CreateQueryableBuilder<TestEntity>(false)
                .SelectFields(entity => entity.UserId)
                .SumField(entity => entity.AccountBalance, "SumValue")
                .Where(entity => entity.Status == 1)
                .GroupByField(entity => entity.UserId)
                .Build();
            var sql = sqlCommand.Sql;
            var expectedSql = "SELECT `UserId`, SUM(`AccountBalance`) AS SumValue FROM `Test` WHERE `Status` = @Status GROUP BY `UserId`";
            Assert.IsTrue(expectedSql == sql);
        }

        [TestMethod]
        public void ValidateSumField2()
        {
            var sqlCommand = _sqlFactory.CreateQueryableBuilder<TestEntity>(false)
                .SelectFields(entity => entity.UserId)
                .SumField($"`{Table<TestEntity>.Field(entity => entity.AccountBalance)}`-`{Table<TestEntity>.Field(entity => entity.AccountBalance2)}`", "SumValue", true)
                .Where(entity => entity.Status == 1)
                .GroupByField(entity => entity.UserId)
                .Build();
            var sql = sqlCommand.Sql;
            var expectedSql = "SELECT `UserId`, SUM(`AccountBalance`-`AccountBalance2`) AS SumValue FROM `Test` WHERE `Status` = @Status GROUP BY `UserId`";
            Assert.IsTrue(expectedSql == sql);
        }

        [TestMethod]
        public void ValidateAvgField()
        {
            var sqlCommand = _sqlFactory.CreateQueryableBuilder<TestEntity>(false)
                .SelectFields(entity => entity.UserId)
                .AvgField(entity => entity.AccountBalance, "AvgValue")
                .Where(entity => entity.Status == 1)
                .GroupByField(entity => entity.UserId)
                .Build();
            var sql = sqlCommand.Sql;
            var expectedSql = "SELECT `UserId`, AVG(`AccountBalance`) AS AvgValue FROM `Test` WHERE `Status` = @Status GROUP BY `UserId`";
            Assert.IsTrue(expectedSql == sql);
        }

        [TestMethod]
        public void ValidateCountField()
        {
            var sqlCommand = _sqlFactory.CreateQueryableBuilder<TestEntity>(false)
                .SelectFields(entity => entity.UserId)
                .CountField(entity => entity.AccountBalance, "CountValue")
                .Where(entity => entity.Status == 1)
                .GroupByField(entity => entity.UserId)
                .Build();
            var sql = sqlCommand.Sql;
            var expectedSql = "SELECT `UserId`, COUNT(`AccountBalance`) AS CountValue FROM `Test` WHERE `Status` = @Status GROUP BY `UserId`";
            Assert.IsTrue(expectedSql == sql);
        }

        [TestMethod]
        public void ValidateDistinctField()
        {
            var sqlCommand = _sqlFactory.CreateQueryableBuilder<TestEntity>(false)
                .DistinctFields(entity => entity.UserId)
                .Where(entity => entity.Status == 1)
                .Build();
            var sql = sqlCommand.Sql;
            var expectedSql = "SELECT DISTINCT `UserId` FROM `Test` WHERE `Status` = @Status";
            Assert.IsTrue(expectedSql == sql);
        }

        [TestMethod]
        public void ValidateCountDistinctField()
        {
            var sqlCommand = _sqlFactory.CreateQueryableBuilder<TestEntity>(false)
                .CountDistinctField(entity => entity.UserId, "CountDistinctValue")
                .Where(entity => entity.Status == 1)
                .Build();
            var sql = sqlCommand.Sql;
            var expectedSql = "SELECT COUNT(DISTINCT `UserId`) AS CountDistinctValue FROM `Test` WHERE `Status` = @Status";
            Assert.IsTrue(expectedSql == sql);
        }
    }
}
