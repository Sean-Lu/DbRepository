using System;
using System.Collections.Generic;
using Example.Dapper.Core.Domain.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sean.Core.DbRepository.Test
{
    /// <summary>
    /// CRUD SQL test.
    /// </summary>
    [TestClass]
    public class SqlBuilderTest : TestBase
    {
        private readonly SqlFactory _sqlFactory;

        public SqlBuilderTest()
        {
            DbContextConfiguration.Options.SqlIndented = true;
            //DbContextConfiguration.Options.SqlParameterized = false;

            _sqlFactory = new SqlFactory(DatabaseType.MySql);
        }

        #region INSERT
        [TestMethod]
        public void TestInsert()
        {
            var testEntity = new TestEntity();
            ISqlCommand sqlCommand = _sqlFactory.CreateInsertableBuilder<TestEntity>(true)
                .SetParameter(testEntity)
                .Build();
            Assert.AreEqual(sqlCommand.Sql, @"INSERT INTO `Test`(`UserId`, `UserName`, `Age`, `Sex`, `PhoneNumber`, `Email`, `IsVip`, `IsBlack`, `Country`, `AccountBalance`, `AccountBalance2`, `Status`, `Remark`, `CreateTime`, `UpdateTime`) 
VALUES(@UserId, @UserName, @Age, @Sex, @PhoneNumber, @Email, @IsVip, @IsBlack, @Country, @AccountBalance, @AccountBalance2, @Status, @Remark, @CreateTime, @UpdateTime)");
            Assert.AreEqual(testEntity, sqlCommand.Parameter);
        }

        [TestMethod]
        public void TestInsertIgnoreFields()
        {
            var testEntity = new TestEntity();
            ISqlCommand sqlCommand = _sqlFactory.CreateInsertableBuilder<TestEntity>(true)
                .IgnoreFields(entity => new { entity.CreateTime, entity.UpdateTime })
                .SetParameter(testEntity)
                .Build();
            Assert.AreEqual(sqlCommand.Sql, @"INSERT INTO `Test`(`UserId`, `UserName`, `Age`, `Sex`, `PhoneNumber`, `Email`, `IsVip`, `IsBlack`, `Country`, `AccountBalance`, `AccountBalance2`, `Status`, `Remark`) 
VALUES(@UserId, @UserName, @Age, @Sex, @PhoneNumber, @Email, @IsVip, @IsBlack, @Country, @AccountBalance, @AccountBalance2, @Status, @Remark)");
            Assert.AreEqual(testEntity, sqlCommand.Parameter);
        }

        [TestMethod]
        public void TestBulkInsert()
        {
            var testEntities = new List<TestEntity>
            {
                new TestEntity(),
                new TestEntity()
            };
            ISqlCommand sqlCommand = _sqlFactory.CreateInsertableBuilder<TestEntity>(true)
                .SetParameter(testEntities)
                .Build();
            Assert.AreEqual(sqlCommand.Sql, @"INSERT INTO `Test`(`UserId`, `UserName`, `Age`, `Sex`, `PhoneNumber`, `Email`, `IsVip`, `IsBlack`, `Country`, `AccountBalance`, `AccountBalance2`, `Status`, `Remark`, `CreateTime`, `UpdateTime`) 
VALUES(@UserId_1, @UserName_1, @Age_1, @Sex_1, @PhoneNumber_1, @Email_1, @IsVip_1, @IsBlack_1, @Country_1, @AccountBalance_1, @AccountBalance2_1, @Status_1, @Remark_1, @CreateTime_1, @UpdateTime_1), 
(@UserId_2, @UserName_2, @Age_2, @Sex_2, @PhoneNumber_2, @Email_2, @IsVip_2, @IsBlack_2, @Country_2, @AccountBalance_2, @AccountBalance2_2, @Status_2, @Remark_2, @CreateTime_2, @UpdateTime_2)");
            AssertSqlParameters(new Dictionary<string, object>
            {
                {"UserId_1",0L},
                {"UserName_1",null},
                {"Age_1",0},
                {"Sex_1",SexType.Unknown},
                {"PhoneNumber_1",null},
                {"Email_1",null},
                {"IsVip_1",false},
                {"IsBlack_1",false},
                {"Country_1",CountryType.Unknown},
                {"AccountBalance_1",0M},
                {"AccountBalance2_1",0M},
                {"Status_1",0},
                {"Remark_1",null},
                {"CreateTime_1",default(DateTime)},
                {"UpdateTime_1",null},
                {"UserId_2",0L},
                {"UserName_2",null},
                {"Age_2",0},
                {"Sex_2",SexType.Unknown},
                {"PhoneNumber_2",null},
                {"Email_2",null},
                {"IsVip_2",false},
                {"IsBlack_2",false},
                {"Country_2",CountryType.Unknown},
                {"AccountBalance_2",0M},
                {"AccountBalance2_2",0M},
                {"Status_2",0},
                {"Remark_2",null},
                {"CreateTime_2",default(DateTime)},
                {"UpdateTime_2",null},
            }, sqlCommand.Parameter as Dictionary<string, object>);
        }
        #endregion

        #region DELETE
        [TestMethod]
        public void TestDeleteWhere()
        {
            ISqlCommand sqlCommand = _sqlFactory.CreateDeleteableBuilder<TestEntity>()
                .Where(entity => entity.Status == 1 && entity.IsVip && entity.Age > 18)
                .Build();
            Assert.AreEqual(sqlCommand.Sql, "DELETE FROM `Test` WHERE `Status` = @Status AND `IsVip` = @IsVip AND `Age` > @Age");
            AssertSqlParameters(new Dictionary<string, object>
            {
                {"Status",1},
                {"IsVip",true},
                {"Age",18},
            }, sqlCommand.Parameter as Dictionary<string, object>);
        }

        [TestMethod]
        public void TestDeleteById()
        {
            var testEntity = new TestEntity { Id = 1 };
            ISqlCommand sqlCommand = _sqlFactory.CreateDeleteableBuilder<TestEntity>()
                .SetParameter(testEntity)
                .Build();
            Assert.AreEqual(sqlCommand.Sql, "DELETE FROM `Test` WHERE 1=1 AND `Id` = @Id");
            Assert.AreEqual(testEntity, sqlCommand.Parameter);
        }

        [TestMethod]
        public void TestDeleteAll()
        {
            ISqlCommand sqlCommand = _sqlFactory.CreateDeleteableBuilder<TestEntity>()
                .AllowEmptyWhereClause()
                .Build();
            Assert.AreEqual(sqlCommand.Sql, "DELETE FROM `Test`");
            Assert.IsNull(sqlCommand.Parameter);
        }
        #endregion

        #region UPDATE
        [TestMethod]
        public void TestUpdateWhere()
        {
            var testEntity = new TestEntity { Id = 1 };
            ISqlCommand sqlCommand = _sqlFactory.CreateUpdateableBuilder<TestEntity>(true)
                .Where(entity => entity.Status == 1 && entity.IsVip && entity.Age > 18)
                .SetParameter(testEntity)
                .Build();
            Assert.AreEqual(sqlCommand.Sql, "UPDATE `Test` SET `UserId`=@UserId, `UserName`=@UserName, `Age`=@Age, `Sex`=@Sex, `PhoneNumber`=@PhoneNumber, `Email`=@Email, `IsVip`=@IsVip, `IsBlack`=@IsBlack, `Country`=@Country, `AccountBalance`=@AccountBalance, `AccountBalance2`=@AccountBalance2, `Status`=@Status, `Remark`=@Remark, `CreateTime`=@CreateTime, `UpdateTime`=@UpdateTime WHERE `Status` = @Status_2 AND `IsVip` = @IsVip_2 AND `Age` > @Age_2");
            AssertSqlParameters(new Dictionary<string, object>
            {
                {"Id",1L},
                {"UserId",0L},
                {"UserName",null},
                {"Age",0},
                {"Sex",SexType.Unknown},
                {"PhoneNumber",null},
                {"Email",null},
                {"IsVip",false},
                {"IsBlack",false},
                {"Country",CountryType.Unknown},
                {"AccountBalance",0M},
                {"AccountBalance2",0M},
                {"Status",0},
                {"Remark",null},
                {"CreateTime",default(DateTime)},
                {"UpdateTime",null},
                {"Age_2",18},
                {"IsVip_2",true},
                {"Status_2",1},
            }, sqlCommand.Parameter as Dictionary<string, object>);

            ISqlCommand sqlCommand2 = _sqlFactory.CreateUpdateableBuilder<TestEntity>(false)
                .UpdateFields(entity => new { entity.AccountBalance, entity.Remark })
                .Where(entity => entity.Status == 1 && entity.IsVip && entity.Age > 18)
                .SetParameter(testEntity)
                .Build();
            Assert.AreEqual(sqlCommand2.Sql, "UPDATE `Test` SET `AccountBalance`=@AccountBalance, `Remark`=@Remark WHERE `Status` = @Status_2 AND `IsVip` = @IsVip_2 AND `Age` > @Age_2");
            AssertSqlParameters(new Dictionary<string, object>
            {
                {"Id",1L},
                {"UserId",0L},
                {"UserName",null},
                {"Age",0},
                {"Sex",SexType.Unknown},
                {"PhoneNumber",null},
                {"Email",null},
                {"IsVip",false},
                {"IsBlack",false},
                {"Country",CountryType.Unknown},
                {"AccountBalance",0M},
                {"AccountBalance2",0M},
                {"Status",0},
                {"Remark",null},
                {"CreateTime",default(DateTime)},
                {"UpdateTime",null},
                {"Age_2",18},
                {"IsVip_2",true},
                {"Status_2",1},
            }, sqlCommand.Parameter as Dictionary<string, object>);
        }

        [TestMethod]
        public void TestUpdateById()
        {
            var testEntity = new TestEntity { Id = 1 };
            ISqlCommand sqlCommand = _sqlFactory.CreateUpdateableBuilder<TestEntity>(true)
                .SetParameter(testEntity)
                .Build();
            Assert.AreEqual(sqlCommand.Sql, "UPDATE `Test` SET `UserId`=@UserId, `UserName`=@UserName, `Age`=@Age, `Sex`=@Sex, `PhoneNumber`=@PhoneNumber, `Email`=@Email, `IsVip`=@IsVip, `IsBlack`=@IsBlack, `Country`=@Country, `AccountBalance`=@AccountBalance, `AccountBalance2`=@AccountBalance2, `Status`=@Status, `Remark`=@Remark, `CreateTime`=@CreateTime, `UpdateTime`=@UpdateTime WHERE 1=1 AND `Id` = @Id");
            Assert.AreEqual(testEntity, sqlCommand.Parameter);

            ISqlCommand sqlCommand2 = _sqlFactory.CreateUpdateableBuilder<TestEntity>(false)
                .UpdateFields(entity => new { entity.AccountBalance, entity.Remark })
                .SetParameter(testEntity)
                .Build();
            Assert.AreEqual(sqlCommand2.Sql, "UPDATE `Test` SET `AccountBalance`=@AccountBalance, `Remark`=@Remark WHERE 1=1 AND `Id` = @Id");
            Assert.AreEqual(testEntity, sqlCommand2.Parameter);
        }

        [TestMethod]
        public void TestBulkUpdate()
        {
            var testEntities = new List<TestEntity>
            {
                new TestEntity(),
                new TestEntity()
            };
            ISqlCommand sqlCommand = _sqlFactory.CreateUpdateableBuilder<TestEntity>(true)
                .SetParameter(testEntities)
                .Build();
            Assert.AreEqual(sqlCommand.Sql, "UPDATE `Test` SET `UserId`=@UserId, `UserName`=@UserName, `Age`=@Age, `Sex`=@Sex, `PhoneNumber`=@PhoneNumber, `Email`=@Email, `IsVip`=@IsVip, `IsBlack`=@IsBlack, `Country`=@Country, `AccountBalance`=@AccountBalance, `AccountBalance2`=@AccountBalance2, `Status`=@Status, `Remark`=@Remark, `CreateTime`=@CreateTime, `UpdateTime`=@UpdateTime WHERE 1=1 AND `Id` = @Id");
            Assert.AreEqual(testEntities, sqlCommand.Parameter);

            ISqlCommand sqlCommand2 = _sqlFactory.CreateUpdateableBuilder<TestEntity>(false)
                .UpdateFields(entity => new { entity.AccountBalance, entity.Remark })
                .SetParameter(testEntities)
                .Build();
            Assert.AreEqual(sqlCommand2.Sql, "UPDATE `Test` SET `AccountBalance`=@AccountBalance, `Remark`=@Remark WHERE 1=1 AND `Id` = @Id");
            Assert.AreEqual(testEntities, sqlCommand2.Parameter);
        }
        #endregion

        #region SELECT
        [TestMethod]
        public void TestSelectWhere()
        {
            ISqlCommand sqlCommand = _sqlFactory.CreateQueryableBuilder<TestEntity>(false)
                .SelectFields(entity => entity.UserId)
                .Where(entity => entity.Status == 1 && entity.IsVip && entity.Age > 18)
                .Build();
            Assert.AreEqual(sqlCommand.Sql, "SELECT `UserId` FROM `Test` WHERE `Status` = @Status AND `IsVip` = @IsVip AND `Age` > @Age");
            AssertSqlParameters(new Dictionary<string, object>
            {
                {"Status",1},
                {"IsVip",true},
                {"Age",18},
            }, sqlCommand.Parameter as Dictionary<string, object>);
        }

        [TestMethod]
        public void TestSelectMultiWhere()
        {
            ISqlCommand sqlCommand = _sqlFactory.CreateQueryableBuilder<TestEntity>(false)
                .SelectFields(entity => entity.UserId)
                .Where(entity => entity.Status == 1)
                .Where(entity => entity.IsVip && entity.Age > 18)
                .Build();
            Assert.AreEqual(sqlCommand.Sql, "SELECT `UserId` FROM `Test` WHERE `Status` = @Status AND `IsVip` = @IsVip AND `Age` > @Age");
            AssertSqlParameters(new Dictionary<string, object>
            {
                {"Status",1},
                {"IsVip",true},
                {"Age",18},
            }, sqlCommand.Parameter as Dictionary<string, object>);
        }

        [TestMethod]
        public void TestSelectWhereIF()
        {
            ISqlCommand sqlCommand = _sqlFactory.CreateQueryableBuilder<TestEntity>(false)
                .SelectFields(entity => entity.UserId)
                .Where(entity => entity.Status == 1)
                .WhereIF(true, entity => entity.IsVip && entity.Age > 18)
                .Build();
            Assert.AreEqual(sqlCommand.Sql, "SELECT `UserId` FROM `Test` WHERE `Status` = @Status AND `IsVip` = @IsVip AND `Age` > @Age");
            AssertSqlParameters(new Dictionary<string, object>
            {
                {"Status",1},
                {"IsVip",true},
                {"Age",18},
            }, sqlCommand.Parameter as Dictionary<string, object>);

            ISqlCommand sqlCommand2 = _sqlFactory.CreateQueryableBuilder<TestEntity>(false)
                .SelectFields(entity => entity.UserId)
                .Where(entity => entity.Status == 1)
                .WhereIF(false, entity => entity.IsVip && entity.Age > 18)
                .Build();
            Assert.AreEqual(sqlCommand2.Sql, "SELECT `UserId` FROM `Test` WHERE `Status` = @Status");
            AssertSqlParameters(new Dictionary<string, object>
            {
                {"Status",1},
            }, sqlCommand2.Parameter as Dictionary<string, object>);
        }

        [TestMethod]
        public void TestSelectMaxField()
        {
            ISqlCommand sqlCommand = _sqlFactory.CreateQueryableBuilder<TestEntity>(false)
                .SelectFields(entity => entity.UserId)
                .MaxField(entity => entity.AccountBalance, "MaxValue")
                .Where(entity => entity.Status == 1)
                .GroupBy(entity => entity.UserId)
                .Build();
            Assert.AreEqual(sqlCommand.Sql, "SELECT `UserId`, MAX(`AccountBalance`) AS MaxValue FROM `Test` WHERE `Status` = @Status GROUP BY `UserId`");
            AssertSqlParameters(new Dictionary<string, object>
            {
                {"Status",1},
            }, sqlCommand.Parameter as Dictionary<string, object>);
        }

        [TestMethod]
        public void TestSelectMinField()
        {
            ISqlCommand sqlCommand = _sqlFactory.CreateQueryableBuilder<TestEntity>(false)
                .SelectFields(entity => entity.UserId)
                .MinField(entity => entity.AccountBalance, "MinValue")
                .Where(entity => entity.Status == 1)
                .GroupBy(entity => entity.UserId)
                .Build();
            Assert.AreEqual(sqlCommand.Sql, "SELECT `UserId`, MIN(`AccountBalance`) AS MinValue FROM `Test` WHERE `Status` = @Status GROUP BY `UserId`");
            AssertSqlParameters(new Dictionary<string, object>
            {
                {"Status",1},
            }, sqlCommand.Parameter as Dictionary<string, object>);
        }

        [TestMethod]
        public void TestSelectSumField()
        {
            ISqlCommand sqlCommand = _sqlFactory.CreateQueryableBuilder<TestEntity>(false)
                .SelectFields(entity => entity.UserId)
                .SumField(entity => entity.AccountBalance, "SumValue")
                .Where(entity => entity.Status == 1)
                .GroupBy(entity => entity.UserId)
                .Build();
            Assert.AreEqual(sqlCommand.Sql, "SELECT `UserId`, SUM(`AccountBalance`) AS SumValue FROM `Test` WHERE `Status` = @Status GROUP BY `UserId`");
            AssertSqlParameters(new Dictionary<string, object>
            {
                {"Status",1},
            }, sqlCommand.Parameter as Dictionary<string, object>);
        }

        [TestMethod]
        public void TestSelectSumField2()
        {
            ISqlCommand sqlCommand = _sqlFactory.CreateQueryableBuilder<TestEntity>(false)
                .SelectFields(entity => entity.UserId)
                .SumField($"`{Table<TestEntity>.Field(entity => entity.AccountBalance)}`-`{Table<TestEntity>.Field(entity => entity.AccountBalance2)}`", "SumValue", true)
                .Where(entity => entity.Status == 1)
                .GroupBy(entity => entity.UserId)
                .Build();
            Assert.AreEqual(sqlCommand.Sql, "SELECT `UserId`, SUM(`AccountBalance`-`AccountBalance2`) AS SumValue FROM `Test` WHERE `Status` = @Status GROUP BY `UserId`");
            AssertSqlParameters(new Dictionary<string, object>
            {
                {"Status",1},
            }, sqlCommand.Parameter as Dictionary<string, object>);
        }

        [TestMethod]
        public void TestSelectAvgField()
        {
            ISqlCommand sqlCommand = _sqlFactory.CreateQueryableBuilder<TestEntity>(false)
                .SelectFields(entity => entity.UserId)
                .AvgField(entity => entity.AccountBalance, "AvgValue")
                .Where(entity => entity.Status == 1)
                .GroupBy(entity => entity.UserId)
                .Build();
            Assert.AreEqual(sqlCommand.Sql, "SELECT `UserId`, AVG(`AccountBalance`) AS AvgValue FROM `Test` WHERE `Status` = @Status GROUP BY `UserId`");
            AssertSqlParameters(new Dictionary<string, object>
            {
                {"Status",1},
            }, sqlCommand.Parameter as Dictionary<string, object>);
        }

        [TestMethod]
        public void TestSelectCountField()
        {
            ISqlCommand sqlCommand = _sqlFactory.CreateQueryableBuilder<TestEntity>(false)
                .SelectFields(entity => entity.UserId)
                .CountField(entity => entity.AccountBalance, "CountValue")
                .Where(entity => entity.Status == 1)
                .GroupBy(entity => entity.UserId)
                .Build();
            Assert.AreEqual(sqlCommand.Sql, "SELECT `UserId`, COUNT(`AccountBalance`) AS CountValue FROM `Test` WHERE `Status` = @Status GROUP BY `UserId`");
            AssertSqlParameters(new Dictionary<string, object>
            {
                {"Status",1},
            }, sqlCommand.Parameter as Dictionary<string, object>);
        }

        [TestMethod]
        public void TestSelectDistinctField()
        {
            ISqlCommand sqlCommand = _sqlFactory.CreateQueryableBuilder<TestEntity>(false)
                .DistinctFields(entity => entity.UserId)
                .Where(entity => entity.Status == 1)
                .Build();
            Assert.AreEqual(sqlCommand.Sql, "SELECT DISTINCT `UserId` FROM `Test` WHERE `Status` = @Status");
            AssertSqlParameters(new Dictionary<string, object>
            {
                {"Status",1},
            }, sqlCommand.Parameter as Dictionary<string, object>);
        }

        [TestMethod]
        public void TestSelectCountDistinctField()
        {
            ISqlCommand sqlCommand = _sqlFactory.CreateQueryableBuilder<TestEntity>(false)
                .CountDistinctField(entity => entity.UserId, "CountDistinctValue")
                .Where(entity => entity.Status == 1)
                .Build();
            Assert.AreEqual(sqlCommand.Sql, "SELECT COUNT(DISTINCT `UserId`) AS CountDistinctValue FROM `Test` WHERE `Status` = @Status");
            AssertSqlParameters(new Dictionary<string, object>
            {
                {"Status",1},
            }, sqlCommand.Parameter as Dictionary<string, object>);
        }
        #endregion
    }
}
