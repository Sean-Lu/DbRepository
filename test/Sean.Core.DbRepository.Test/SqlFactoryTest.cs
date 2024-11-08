using System;
using System.Collections.Generic;
using Example.Dapper.Core.Domain.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sean.Core.DbRepository.Util;

namespace Sean.Core.DbRepository.Test
{
    /// <summary>
    /// <see cref="SqlFactory"/> Test
    /// </summary>
    [TestClass]
    public class SqlFactoryTest : TestBase
    {
        #region INSERT
        [TestMethod]
        public void TestInsert()
        {
            var testEntity = new TestEntity();
            ISqlCommand sqlCommand = SqlFactory.CreateInsertableBuilder<TestEntity>(DatabaseType.MySql)
                .SetParameter(testEntity)
                .SetSqlIndented(true)
                .Build();
            Assert.AreEqual(sqlCommand.Sql, @"INSERT INTO `Test`(`UserId`, `UserName`, `Age`, `Sex`, `PhoneNumber`, `Email`, `IsVip`, `IsBlack`, `Country`, `AccountBalance`, `AccountBalance2`, `Status`, `Remark`, `CreateTime`, `UpdateTime`) 
VALUES(@UserId, @UserName, @Age, @Sex, @PhoneNumber, @Email, @IsVip, @IsBlack, @Country, @AccountBalance, @AccountBalance2, @Status, @Remark, @CreateTime, @UpdateTime)");
            Assert.AreEqual(testEntity, sqlCommand.Parameter);

            testEntity.Id = 10001L;

            ISqlCommand sqlCommand2 = SqlFactory.CreateInsertableBuilder<TestEntity>(DatabaseType.MySql)
                .SetParameter(testEntity)
                .SetSqlIndented(true)
                .Build();
            Assert.AreEqual(sqlCommand2.Sql, @"INSERT INTO `Test`(`Id`, `UserId`, `UserName`, `Age`, `Sex`, `PhoneNumber`, `Email`, `IsVip`, `IsBlack`, `Country`, `AccountBalance`, `AccountBalance2`, `Status`, `Remark`, `CreateTime`, `UpdateTime`) 
VALUES(@Id, @UserId, @UserName, @Age, @Sex, @PhoneNumber, @Email, @IsVip, @IsBlack, @Country, @AccountBalance, @AccountBalance2, @Status, @Remark, @CreateTime, @UpdateTime)");
            Assert.AreEqual(testEntity, sqlCommand2.Parameter);
        }

        [TestMethod]
        public void TestInsertIgnoreFields()
        {
            var testEntity = new TestEntity();
            ISqlCommand sqlCommand = SqlFactory.CreateInsertableBuilder<TestEntity>(DatabaseType.MySql)
                .IgnoreFields(entity => new { entity.CreateTime, entity.UpdateTime })
                .SetParameter(testEntity)
                .SetSqlIndented(true)
                .Build();
            Assert.AreEqual(sqlCommand.Sql, @"INSERT INTO `Test`(`UserId`, `UserName`, `Age`, `Sex`, `PhoneNumber`, `Email`, `IsVip`, `IsBlack`, `Country`, `AccountBalance`, `AccountBalance2`, `Status`, `Remark`) 
VALUES(@UserId, @UserName, @Age, @Sex, @PhoneNumber, @Email, @IsVip, @IsBlack, @Country, @AccountBalance, @AccountBalance2, @Status, @Remark)");
            Assert.AreEqual(testEntity, sqlCommand.Parameter);
            
            testEntity.Id = 10001L;

            ISqlCommand sqlCommand2 = SqlFactory.CreateInsertableBuilder<TestEntity>(DatabaseType.MySql)
                .IgnoreFields(entity => new { entity.CreateTime, entity.UpdateTime })
                .SetParameter(testEntity)
                .SetSqlIndented(true)
                .Build();
            Assert.AreEqual(sqlCommand2.Sql, @"INSERT INTO `Test`(`Id`, `UserId`, `UserName`, `Age`, `Sex`, `PhoneNumber`, `Email`, `IsVip`, `IsBlack`, `Country`, `AccountBalance`, `AccountBalance2`, `Status`, `Remark`) 
VALUES(@Id, @UserId, @UserName, @Age, @Sex, @PhoneNumber, @Email, @IsVip, @IsBlack, @Country, @AccountBalance, @AccountBalance2, @Status, @Remark)");
            Assert.AreEqual(testEntity, sqlCommand2.Parameter);
        }

        [TestMethod]
        public void TestBulkInsert()
        {
            var testEntities = new List<TestEntity>
            {
                new TestEntity(),
                new TestEntity()
            };
            ISqlCommand sqlCommand = SqlFactory.CreateInsertableBuilder<TestEntity>(DatabaseType.MySql)
                .SetParameter(testEntities)
                .SetSqlIndented(true)
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
            ISqlCommand sqlCommand = SqlFactory.CreateDeleteableBuilder<TestEntity>(DatabaseType.MySql)
                .Where(entity => entity.Status == 1 && entity.IsVip && entity.Age > 18)
                .SetSqlIndented(true)
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
            ISqlCommand sqlCommand = SqlFactory.CreateDeleteableBuilder<TestEntity>(DatabaseType.MySql)
                .SetParameter(testEntity)
                .SetSqlIndented(true)
                .Build();
            Assert.AreEqual(sqlCommand.Sql, "DELETE FROM `Test` WHERE 1=1 AND `Id` = @Id");
            Assert.AreEqual(testEntity, sqlCommand.Parameter);
        }

        [TestMethod]
        public void TestDeleteAll()
        {
            ISqlCommand sqlCommand = SqlFactory.CreateDeleteableBuilder<TestEntity>(DatabaseType.MySql)
                .AllowEmptyWhereClause()
                .SetSqlIndented(true)
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
            ISqlCommand sqlCommand = SqlFactory.CreateUpdateableBuilder<TestEntity>(DatabaseType.MySql)
                .Where(entity => entity.Status == 1 && entity.IsVip && entity.Age > 18)
                .SetParameter(testEntity)
                .SetSqlIndented(true)
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

            ISqlCommand sqlCommand2 = SqlFactory.CreateUpdateableBuilder<TestEntity>(DatabaseType.MySql)
                .UpdateFields(entity => new { entity.AccountBalance, entity.Remark })
                .Where(entity => entity.Status == 1 && entity.IsVip && entity.Age > 18)
                .SetParameter(testEntity)
                .SetSqlIndented(true)
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
            ISqlCommand sqlCommand = SqlFactory.CreateUpdateableBuilder<TestEntity>(DatabaseType.MySql)
                .SetParameter(testEntity)
                .SetSqlIndented(true)
                .Build();
            Assert.AreEqual(sqlCommand.Sql, "UPDATE `Test` SET `UserId`=@UserId, `UserName`=@UserName, `Age`=@Age, `Sex`=@Sex, `PhoneNumber`=@PhoneNumber, `Email`=@Email, `IsVip`=@IsVip, `IsBlack`=@IsBlack, `Country`=@Country, `AccountBalance`=@AccountBalance, `AccountBalance2`=@AccountBalance2, `Status`=@Status, `Remark`=@Remark, `CreateTime`=@CreateTime, `UpdateTime`=@UpdateTime WHERE 1=1 AND `Id` = @Id");
            Assert.AreEqual(testEntity, sqlCommand.Parameter);

            ISqlCommand sqlCommand2 = SqlFactory.CreateUpdateableBuilder<TestEntity>(DatabaseType.MySql)
                .UpdateFields(entity => new { entity.AccountBalance, entity.Remark })
                .SetParameter(testEntity)
                .SetSqlIndented(true)
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
            ISqlCommand sqlCommand = SqlFactory.CreateUpdateableBuilder<TestEntity>(DatabaseType.MySql)
                .SetParameter(testEntities)
                .SetSqlIndented(true)
                .Build();
            Assert.AreEqual(sqlCommand.Sql, "UPDATE `Test` SET `UserId`=@UserId, `UserName`=@UserName, `Age`=@Age, `Sex`=@Sex, `PhoneNumber`=@PhoneNumber, `Email`=@Email, `IsVip`=@IsVip, `IsBlack`=@IsBlack, `Country`=@Country, `AccountBalance`=@AccountBalance, `AccountBalance2`=@AccountBalance2, `Status`=@Status, `Remark`=@Remark, `CreateTime`=@CreateTime, `UpdateTime`=@UpdateTime WHERE 1=1 AND `Id` = @Id");
            Assert.AreEqual(testEntities, sqlCommand.Parameter);

            ISqlCommand sqlCommand2 = SqlFactory.CreateUpdateableBuilder<TestEntity>(DatabaseType.MySql)
                .UpdateFields(entity => new { entity.AccountBalance, entity.Remark })
                .SetParameter(testEntities)
                .SetSqlIndented(true)
                .Build();
            Assert.AreEqual(sqlCommand2.Sql, "UPDATE `Test` SET `AccountBalance`=@AccountBalance, `Remark`=@Remark WHERE 1=1 AND `Id` = @Id");
            Assert.AreEqual(testEntities, sqlCommand2.Parameter);
        }
        #endregion

        #region SELECT
        [TestMethod]
        public void TestSelectWhere()
        {
            ISqlCommand sqlCommand = SqlFactory.CreateQueryableBuilder<TestEntity>(DatabaseType.MySql)
                .SelectFields(entity => entity.UserId)
                .Where(entity => entity.Status == 1 && entity.IsVip && entity.Age > 18)
                .SetSqlIndented(true)
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
            ISqlCommand sqlCommand = SqlFactory.CreateQueryableBuilder<TestEntity>(DatabaseType.MySql)
                .SelectFields(entity => entity.UserId)
                .Where(entity => entity.Status == 1)
                .Where(entity => entity.IsVip && entity.Age > 18)
                .SetSqlIndented(true)
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
            ISqlCommand sqlCommand = SqlFactory.CreateQueryableBuilder<TestEntity>(DatabaseType.MySql)
                .SelectFields(entity => entity.UserId)
                .Where(entity => entity.Status == 1)
                .WhereIF(true, entity => entity.IsVip && entity.Age > 18)
                .SetSqlIndented(true)
                .Build();
            Assert.AreEqual(sqlCommand.Sql, "SELECT `UserId` FROM `Test` WHERE `Status` = @Status AND `IsVip` = @IsVip AND `Age` > @Age");
            AssertSqlParameters(new Dictionary<string, object>
            {
                {"Status",1},
                {"IsVip",true},
                {"Age",18},
            }, sqlCommand.Parameter as Dictionary<string, object>);

            ISqlCommand sqlCommand2 = SqlFactory.CreateQueryableBuilder<TestEntity>(DatabaseType.MySql)
                .SelectFields(entity => entity.UserId)
                .Where(entity => entity.Status == 1)
                .WhereIF(false, entity => entity.IsVip && entity.Age > 18)
                .SetSqlIndented(true)
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
            ISqlCommand sqlCommand = SqlFactory.CreateQueryableBuilder<TestEntity>(DatabaseType.MySql)
                .SelectFields(entity => entity.UserId)
                .MaxField(entity => entity.AccountBalance, "MaxValue")
                .Where(entity => entity.Status == 1)
                .GroupBy(entity => entity.UserId)
                .SetSqlIndented(true)
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
            ISqlCommand sqlCommand = SqlFactory.CreateQueryableBuilder<TestEntity>(DatabaseType.MySql)
                .SelectFields(entity => entity.UserId)
                .MinField(entity => entity.AccountBalance, "MinValue")
                .Where(entity => entity.Status == 1)
                .GroupBy(entity => entity.UserId)
                .SetSqlIndented(true)
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
            ISqlCommand sqlCommand = SqlFactory.CreateQueryableBuilder<TestEntity>(DatabaseType.MySql)
                .SelectFields(entity => entity.UserId)
                .SumField(entity => entity.AccountBalance, "SumValue")
                .Where(entity => entity.Status == 1)
                .GroupBy(entity => entity.UserId)
                .SetSqlIndented(true)
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
            ISqlCommand sqlCommand = SqlFactory.CreateQueryableBuilder<TestEntity>(DatabaseType.MySql)
                .SelectFields(entity => entity.UserId)
                .SumField($"`{Table<TestEntity>.Field(entity => entity.AccountBalance)}`-`{Table<TestEntity>.Field(entity => entity.AccountBalance2)}`", "SumValue", true)
                .Where(entity => entity.Status == 1)
                .GroupBy(entity => entity.UserId)
                .SetSqlIndented(true)
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
            ISqlCommand sqlCommand = SqlFactory.CreateQueryableBuilder<TestEntity>(DatabaseType.MySql)
                .SelectFields(entity => entity.UserId)
                .AvgField(entity => entity.AccountBalance, "AvgValue")
                .Where(entity => entity.Status == 1)
                .GroupBy(entity => entity.UserId)
                .SetSqlIndented(true)
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
            ISqlCommand sqlCommand = SqlFactory.CreateQueryableBuilder<TestEntity>(DatabaseType.MySql)
                .SelectFields(entity => entity.UserId)
                .CountField(entity => entity.AccountBalance, "CountValue")
                .Where(entity => entity.Status == 1)
                .GroupBy(entity => entity.UserId)
                .SetSqlIndented(true)
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
            ISqlCommand sqlCommand = SqlFactory.CreateQueryableBuilder<TestEntity>(DatabaseType.MySql)
                .DistinctFields(entity => entity.UserId)
                .Where(entity => entity.Status == 1)
                .SetSqlIndented(true)
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
            ISqlCommand sqlCommand = SqlFactory.CreateQueryableBuilder<TestEntity>(DatabaseType.MySql)
                .CountDistinctField(entity => entity.UserId, "CountDistinctValue")
                .Where(entity => entity.Status == 1)
                .SetSqlIndented(true)
                .Build();
            Assert.AreEqual(sqlCommand.Sql, "SELECT COUNT(DISTINCT `UserId`) AS CountDistinctValue FROM `Test` WHERE `Status` = @Status");
            AssertSqlParameters(new Dictionary<string, object>
            {
                {"Status",1},
            }, sqlCommand.Parameter as Dictionary<string, object>);
        }
        #endregion



        #region TABLE NAME Clause
        [TestMethod]
        public void TestSingleTableName()
        {
            var sqlCommand = SqlFactory.CreateTableNameClauseBuilder<TestEntity>()
                .Build();
            var tableNameClause = sqlCommand.Sql;
            Assert.AreEqual("Test", tableNameClause);

            var sqlCommand2 = SqlFactory.CreateTableNameClauseBuilder<TestEntity>(DatabaseType.MySql)
                .Build();
            var tableNameClause2 = sqlCommand2.Sql;
            Assert.AreEqual("`Test`", tableNameClause2);
        }

        [TestMethod]
        public void TestInnerJoinTableName()
        {
            var sqlCommand = SqlFactory.CreateTableNameClauseBuilder<TestEntity>()
                .InnerJoin<UserEntity>(entity => entity.UserId, entity2 => entity2.Id)
                .Build();
            var tableNameClause = sqlCommand.Sql;
            Assert.AreEqual("Test INNER JOIN User ON Test.UserId=User.Id", tableNameClause);

            var sqlCommand2 = SqlFactory.CreateTableNameClauseBuilder<TestEntity>(DatabaseType.MySql)
                .InnerJoin<UserEntity>(entity => entity.UserId, entity2 => entity2.Id)
                .Build();
            var tableNameClause2 = sqlCommand2.Sql;
            Assert.AreEqual("`Test` INNER JOIN `User` ON `Test`.`UserId`=`User`.`Id`", tableNameClause2);

            var sqlCommand3 = SqlFactory.CreateTableNameClauseBuilder<TestEntity>(DatabaseType.MySql)
                .InnerJoin<UserEntity>(entity => entity.UserId, entity2 => entity2.Id, "a")
                .Build();
            var tableNameClause3 = sqlCommand3.Sql;
            Assert.AreEqual("`Test` INNER JOIN `User` a ON `Test`.`UserId`=a.`Id`", tableNameClause3);

            var sqlCommand4 = SqlFactory.CreateTableNameClauseBuilder<TestEntity>(DatabaseType.MySql)
                .InnerJoin<UserEntity>(entity => entity.UserId, entity2 => entity2.Id, "a")
                .InnerJoin<UserEntity, CheckInLogEntity>(entity => entity.Id, entity2 => entity2.UserId, "a", "b")
                .Build();
            var tableNameClause4 = sqlCommand4.Sql;
            Assert.AreEqual("`Test` INNER JOIN `User` a ON `Test`.`UserId`=a.`Id` INNER JOIN `CheckInLog` b ON a.`Id`=b.`UserId`", tableNameClause4);
        }

        [TestMethod]
        public void TestLeftJoinTableName()
        {
            var sqlCommand = SqlFactory.CreateTableNameClauseBuilder<TestEntity>()
                .LeftJoin<UserEntity>(entity => entity.UserId, entity2 => entity2.Id)
                .Build();
            var tableNameClause = sqlCommand.Sql;
            Assert.AreEqual("Test LEFT JOIN User ON Test.UserId=User.Id", tableNameClause);

            var sqlCommand2 = SqlFactory.CreateTableNameClauseBuilder<TestEntity>(DatabaseType.MySql)
                .LeftJoin<UserEntity>(entity => entity.UserId, entity2 => entity2.Id)
                .Build();
            var tableNameClause2 = sqlCommand2.Sql;
            Assert.AreEqual("`Test` LEFT JOIN `User` ON `Test`.`UserId`=`User`.`Id`", tableNameClause2);

            var sqlCommand3 = SqlFactory.CreateTableNameClauseBuilder<TestEntity>(DatabaseType.MySql)
                .LeftJoin<UserEntity>(entity => entity.UserId, entity2 => entity2.Id, "a")
                .Build();
            var tableNameClause3 = sqlCommand3.Sql;
            Assert.AreEqual("`Test` LEFT JOIN `User` a ON `Test`.`UserId`=a.`Id`", tableNameClause3);

            var sqlCommand4 = SqlFactory.CreateTableNameClauseBuilder<TestEntity>(DatabaseType.MySql)
                .LeftJoin<UserEntity>(entity => entity.UserId, entity2 => entity2.Id, "a")
                .LeftJoin<UserEntity, CheckInLogEntity>(entity => entity.Id, entity2 => entity2.UserId, "a", "b")
                .Build();
            var tableNameClause4 = sqlCommand4.Sql;
            Assert.AreEqual("`Test` LEFT JOIN `User` a ON `Test`.`UserId`=a.`Id` LEFT JOIN `CheckInLog` b ON a.`Id`=b.`UserId`", tableNameClause4);
        }

        [TestMethod]
        public void TestRightJoinTableName()
        {
            var sqlCommand = SqlFactory.CreateTableNameClauseBuilder<TestEntity>()
                .RightJoin<UserEntity>(entity => entity.UserId, entity2 => entity2.Id)
                .Build();
            var tableNameClause = sqlCommand.Sql;
            Assert.AreEqual("Test RIGHT JOIN User ON Test.UserId=User.Id", tableNameClause);

            var sqlCommand2 = SqlFactory.CreateTableNameClauseBuilder<TestEntity>(DatabaseType.MySql)
                .RightJoin<UserEntity>(entity => entity.UserId, entity2 => entity2.Id)
                .Build();
            var tableNameClause2 = sqlCommand2.Sql;
            Assert.AreEqual("`Test` RIGHT JOIN `User` ON `Test`.`UserId`=`User`.`Id`", tableNameClause2);

            var sqlCommand3 = SqlFactory.CreateTableNameClauseBuilder<TestEntity>(DatabaseType.MySql)
                .RightJoin<UserEntity>(entity => entity.UserId, entity2 => entity2.Id, "a")
                .Build();
            var tableNameClause3 = sqlCommand3.Sql;
            Assert.AreEqual("`Test` RIGHT JOIN `User` a ON `Test`.`UserId`=a.`Id`", tableNameClause3);

            var sqlCommand4 = SqlFactory.CreateTableNameClauseBuilder<TestEntity>(DatabaseType.MySql)
                .RightJoin<UserEntity>(entity => entity.UserId, entity2 => entity2.Id, "a")
                .RightJoin<UserEntity, CheckInLogEntity>(entity => entity.Id, entity2 => entity2.UserId, "a", "b")
                .Build();
            var tableNameClause4 = sqlCommand4.Sql;
            Assert.AreEqual("`Test` RIGHT JOIN `User` a ON `Test`.`UserId`=a.`Id` RIGHT JOIN `CheckInLog` b ON a.`Id`=b.`UserId`", tableNameClause4);
        }

        [TestMethod]
        public void TestFullJoinTableName()
        {
            var sqlCommand = SqlFactory.CreateTableNameClauseBuilder<TestEntity>()
                .FullJoin<UserEntity>(entity => entity.UserId, entity2 => entity2.Id)
                .Build();
            var tableNameClause = sqlCommand.Sql;
            Assert.AreEqual("Test FULL JOIN User ON Test.UserId=User.Id", tableNameClause);

            var sqlCommand2 = SqlFactory.CreateTableNameClauseBuilder<TestEntity>(DatabaseType.MySql)
                .FullJoin<UserEntity>(entity => entity.UserId, entity2 => entity2.Id)
                .Build();
            var tableNameClause2 = sqlCommand2.Sql;
            Assert.AreEqual("`Test` FULL JOIN `User` ON `Test`.`UserId`=`User`.`Id`", tableNameClause2);

            var sqlCommand3 = SqlFactory.CreateTableNameClauseBuilder<TestEntity>(DatabaseType.MySql)
                .FullJoin<UserEntity>(entity => entity.UserId, entity2 => entity2.Id, "a")
                .Build();
            var tableNameClause3 = sqlCommand3.Sql;
            Assert.AreEqual("`Test` FULL JOIN `User` a ON `Test`.`UserId`=a.`Id`", tableNameClause3);

            var sqlCommand4 = SqlFactory.CreateTableNameClauseBuilder<TestEntity>(DatabaseType.MySql)
                .FullJoin<UserEntity>(entity => entity.UserId, entity2 => entity2.Id, "a")
                .FullJoin<UserEntity, CheckInLogEntity>(entity => entity.Id, entity2 => entity2.UserId, "a", "b")
                .Build();
            var tableNameClause4 = sqlCommand4.Sql;
            Assert.AreEqual("`Test` FULL JOIN `User` a ON `Test`.`UserId`=a.`Id` FULL JOIN `CheckInLog` b ON a.`Id`=b.`UserId`", tableNameClause4);
        }
        #endregion

        #region WHERE Clause
        [TestMethod]
        public void TestSingleTableSqlParameterized()
        {
            //CountryType[] list = { CountryType.China, CountryType.America, CountryType.Russia };
            List<CountryType> list = new List<CountryType> { CountryType.China, CountryType.America, CountryType.Russia };
            var sqlCommand = SqlFactory.CreateWhereClauseBuilder<TestEntity>(DatabaseType.MySql)
                .Where(entity => entity.UserId == 10010L || entity.IsVip)
                .Where(entity => list.Contains(entity.Country))
                .Where(entity => !string.IsNullOrEmpty(entity.PhoneNumber))
                .Build();
            var whereClause = sqlCommand.Sql;
            var parameters = sqlCommand.Parameter;
            var expectedParameters = new Dictionary<string, object>
            {
                { "UserId", 10010L },
                { "Country", list },
                { "IsVip", true }
            };
            Assert.AreEqual("(`UserId` = @UserId OR `IsVip` = @IsVip) AND `Country` IN @Country AND `PhoneNumber` IS NOT NULL AND `PhoneNumber` <> ''", whereClause);
            AssertSqlParameters(expectedParameters, SqlParameterUtil.ConvertToDicParameter(parameters));
        }

        [TestMethod]
        public void TestSingleTableNotSqlParameterized()
        {
            //CountryType[] list = { CountryType.China, CountryType.America, CountryType.Russia };
            List<CountryType> list = new List<CountryType> { CountryType.China, CountryType.America, CountryType.Russia };
            var sqlCommand = SqlFactory.CreateWhereClauseBuilder<TestEntity>(DatabaseType.MySql)
                .Where(entity => entity.UserId == 10010L || entity.IsVip)
                .Where(entity => list.Contains(entity.Country))
                .Where(entity => !string.IsNullOrEmpty(entity.PhoneNumber))
                .SetSqlParameterized(false)
                .Build();
            var whereClause = sqlCommand.Sql;
            var parameters = sqlCommand.Parameter;
            var expectedParameters = new Dictionary<string, object>
            {
                { "UserId", 10010L },
                { "Country", list },
                { "IsVip", true }
            };
            Assert.AreEqual("(`UserId` = 10010 OR `IsVip` = 1) AND `Country` IN (1,2,4) AND `PhoneNumber` IS NOT NULL AND `PhoneNumber` <> ''", whereClause);
            AssertSqlParameters(expectedParameters, SqlParameterUtil.ConvertToDicParameter(parameters));
        }

        [TestMethod]
        public void TestMultiTableSqlParameterized()
        {
            var sqlCommand = SqlFactory.CreateWhereClauseBuilder<TestEntity>(DatabaseType.MySql)
                .Where(entity => entity.UserId == 10010L || entity.IsVip)
                .Where<CheckInLogEntity>(entity => entity.CreateTime > DateTime.Parse("2024-09-25 12:00:00"))
                .Where<CheckInLogEntity>(entity => entity.UserId == 101L)
                .Build();
            var whereClause = sqlCommand.Sql;
            var parameters = sqlCommand.Parameter;
            var expectedParameters = new Dictionary<string, object>
            {
                { "UserId", 10010L },
                { "IsVip", true },
                { "CreateTime", DateTime.Parse("2024-09-25 12:00:00") },
                { "UserId_2", 101L },
            };
            Assert.AreEqual("(`Test`.`UserId` = @UserId OR `Test`.`IsVip` = @IsVip) AND `CheckInLog`.`CreateTime` > @CreateTime AND `CheckInLog`.`UserId` = @UserId_2", whereClause);
            AssertSqlParameters(expectedParameters, SqlParameterUtil.ConvertToDicParameter(parameters));
        }

        [TestMethod]
        public void TestMultiTableNotSqlParameterized()
        {
            var sqlCommand = SqlFactory.CreateWhereClauseBuilder<TestEntity>(DatabaseType.MySql)
                .Where(entity => entity.UserId == 10010L || entity.IsVip)
                .Where<CheckInLogEntity>(entity => entity.CreateTime > DateTime.Parse("2024-09-25 12:00:00"))
                .Where<CheckInLogEntity>(entity => entity.UserId == 101L)
                .SetSqlParameterized(false)
                .Build();
            var whereClause = sqlCommand.Sql;
            var parameters = sqlCommand.Parameter;
            var expectedParameters = new Dictionary<string, object>
            {
                { "UserId", 10010L },
                { "IsVip", true },
                { "CreateTime", DateTime.Parse("2024-09-25 12:00:00") },
                { "UserId_2", 101L },
            };
            Assert.AreEqual("(`Test`.`UserId` = 10010 OR `Test`.`IsVip` = 1) AND `CheckInLog`.`CreateTime` > '2024-09-25 12:00:00' AND `CheckInLog`.`UserId` = 101", whereClause);
            AssertSqlParameters(expectedParameters, SqlParameterUtil.ConvertToDicParameter(parameters));
        }

        [TestMethod]
        public void TestMsAccessSqlParameterized()
        {
            var sqlCommand = SqlFactory.CreateWhereClauseBuilder<TestEntity>(DatabaseType.MsAccess)
                .Where(entity => entity.UserId == 10010L || entity.IsVip)
                .Where<CheckInLogEntity>(entity => entity.CreateTime > DateTime.Parse("2024-09-25 12:00:00"))
                .Where<CheckInLogEntity>(entity => entity.UserId == 101L)
                .Build();
            var whereClause = sqlCommand.Sql;
            var parameters = sqlCommand.Parameter;
            var expectedParameters = new Dictionary<string, object>
            {
                { "UserId", 10010L },
                { "IsVip", true },
                { "CreateTime", DateTime.Parse("2024-09-25 12:00:00") },
                { "UserId_2", 101L },
            };
            Assert.AreEqual("([Test].[UserId] = @UserId OR [Test].[IsVip] = @IsVip) AND [CheckInLog].[CreateTime] > @CreateTime AND [CheckInLog].[UserId] = @UserId_2", whereClause);
            AssertSqlParameters(expectedParameters, SqlParameterUtil.ConvertToDicParameter(parameters));
        }

        [TestMethod]
        public void TestMsAccessNotSqlParameterized()
        {
            var sqlCommand = SqlFactory.CreateWhereClauseBuilder<TestEntity>(DatabaseType.MsAccess)
                .Where(entity => entity.UserId == 10010L || entity.IsVip)
                .Where<CheckInLogEntity>(entity => entity.CreateTime > DateTime.Parse("2024-09-25 12:00:00"))
                .Where<CheckInLogEntity>(entity => entity.UserId == 101L)
                .SetSqlParameterized(false)
                .Build();
            var whereClause = sqlCommand.Sql;
            var parameters = sqlCommand.Parameter;
            var expectedParameters = new Dictionary<string, object>
            {
                { "UserId", 10010L },
                { "IsVip", true },
                { "CreateTime", DateTime.Parse("2024-09-25 12:00:00") },
                { "UserId_2", 101L },
            };
            Assert.AreEqual("([Test].[UserId] = 10010 OR [Test].[IsVip] = 1) AND [CheckInLog].[CreateTime] > '2024-09-25 12:00:00' AND [CheckInLog].[UserId] = 101", whereClause);
            AssertSqlParameters(expectedParameters, SqlParameterUtil.ConvertToDicParameter(parameters));
        }

        [TestMethod]
        public void TestDuckDBSqlParameterized()
        {
            var sqlCommand = SqlFactory.CreateWhereClauseBuilder<TestEntity>(DatabaseType.DuckDB)
                .Where(entity => entity.UserId == 10010L || entity.IsVip)
                .Where<CheckInLogEntity>(entity => entity.CreateTime > DateTime.Parse("2024-09-25 12:00:00"))
                .Where<CheckInLogEntity>(entity => entity.UserId == 101L)
                .Build();
            var whereClause = sqlCommand.Sql;
            var parameters = sqlCommand.Parameter;
            var expectedParameters = new Dictionary<string, object>
            {
                { "UserId", 10010L },
                { "IsVip", true },
                { "CreateTime", DateTime.Parse("2024-09-25 12:00:00") },
                { "UserId_2", 101L },
            };
            Assert.AreEqual("(\"Test\".\"UserId\" = ? OR \"Test\".\"IsVip\" = ?) AND \"CheckInLog\".\"CreateTime\" > ? AND \"CheckInLog\".\"UserId\" = ?", whereClause);
            AssertSqlParameters(expectedParameters, SqlParameterUtil.ConvertToDicParameter(parameters));
        }

        [TestMethod]
        public void TestDuckDBNotSqlParameterized()
        {
            var sqlCommand = SqlFactory.CreateWhereClauseBuilder<TestEntity>(DatabaseType.DuckDB)
                .Where(entity => entity.UserId == 10010L || entity.IsVip)
                .Where<CheckInLogEntity>(entity => entity.CreateTime > DateTime.Parse("2024-09-25 12:00:00"))
                .Where<CheckInLogEntity>(entity => entity.UserId == 101L)
                .SetSqlParameterized(false)
                .Build();
            var whereClause = sqlCommand.Sql;
            var parameters = sqlCommand.Parameter;
            var expectedParameters = new Dictionary<string, object>
            {
                { "UserId", 10010L },
                { "IsVip", true },
                { "CreateTime", DateTime.Parse("2024-09-25 12:00:00") },
                { "UserId_2", 101L },
            };
            Assert.AreEqual("(\"Test\".\"UserId\" = 10010 OR \"Test\".\"IsVip\" = 1) AND \"CheckInLog\".\"CreateTime\" > '2024-09-25 12:00:00' AND \"CheckInLog\".\"UserId\" = 101", whereClause);
            AssertSqlParameters(expectedParameters, SqlParameterUtil.ConvertToDicParameter(parameters));
        }

        [TestMethod]
        public void TestUnknownSqlParameterized()
        {
            var sqlCommand = SqlFactory.CreateWhereClauseBuilder<TestEntity>()
                .Where(entity => entity.UserId == 10010L || entity.IsVip)
                .Where<CheckInLogEntity>(entity => entity.CreateTime > DateTime.Parse("2024-09-25 12:00:00"))
                .Where<CheckInLogEntity>(entity => entity.UserId == 101L)
                .Build();
            var whereClause = sqlCommand.Sql;
            var parameters = sqlCommand.Parameter;
            var expectedParameters = new Dictionary<string, object>
            {
                { "UserId", 10010L },
                { "IsVip", true },
                { "CreateTime", DateTime.Parse("2024-09-25 12:00:00") },
                { "UserId_2", 101L },
            };
            Assert.AreEqual("(Test.UserId = @UserId OR Test.IsVip = @IsVip) AND CheckInLog.CreateTime > @CreateTime AND CheckInLog.UserId = @UserId_2", whereClause);
            AssertSqlParameters(expectedParameters, SqlParameterUtil.ConvertToDicParameter(parameters));
        }

        [TestMethod]
        public void TestUnknownNotSqlParameterized()
        {
            var sqlCommand = SqlFactory.CreateWhereClauseBuilder<TestEntity>()
                .Where(entity => entity.UserId == 10010L || entity.IsVip)
                .Where<CheckInLogEntity>(entity => entity.CreateTime > DateTime.Parse("2024-09-25 12:00:00"))
                .Where<CheckInLogEntity>(entity => entity.UserId == 101L)
                .SetSqlParameterized(false)
                .Build();
            var whereClause = sqlCommand.Sql;
            var parameters = sqlCommand.Parameter;
            var expectedParameters = new Dictionary<string, object>
            {
                { "UserId", 10010L },
                { "IsVip", true },
                { "CreateTime", DateTime.Parse("2024-09-25 12:00:00") },
                { "UserId_2", 101L },
            };
            Assert.AreEqual("(Test.UserId = 10010 OR Test.IsVip = 1) AND CheckInLog.CreateTime > '2024-09-25 12:00:00' AND CheckInLog.UserId = 101", whereClause);
            AssertSqlParameters(expectedParameters, SqlParameterUtil.ConvertToDicParameter(parameters));
        }
        #endregion

        #region ORDER BY Clause
        [TestMethod]
        public void TestSingleTable()
        {
            var sqlCommand = SqlFactory.CreateOrderByClauseBuilder<TestEntity>(DatabaseType.MySql)
                .OrderBy(OrderByType.Desc, entity => entity.CreateTime)
                .OrderBy(OrderByType.Desc, entity => entity.Id)
                .Build();
            var orderByClause = sqlCommand.Sql;
            Assert.AreEqual("`CreateTime` DESC, `Id` DESC", orderByClause);
        }

        [TestMethod]
        public void TestSingleTable2()
        {
            var sqlCommand = SqlFactory.CreateOrderByClauseBuilder<TestEntity>(DatabaseType.MySql)
                .OrderBy(OrderByType.Desc, entity => new { entity.CreateTime, entity.UserId })
                .OrderBy(OrderByType.Desc, entity => entity.Id)
                .Build();
            var orderByClause = sqlCommand.Sql;
            Assert.AreEqual("`CreateTime`, `UserId` DESC, `Id` DESC", orderByClause);
        }

        [TestMethod]
        public void TestMultiTable()
        {
            var sqlCommand = SqlFactory.CreateOrderByClauseBuilder<TestEntity>(DatabaseType.MySql)
                .OrderBy(OrderByType.Desc, entity => entity.CreateTime)
                .OrderBy(OrderByType.Desc, entity => entity.Id)
                .IsMultiTable(true)
                .Build();
            var orderByClause = sqlCommand.Sql;
            Assert.AreEqual("`Test`.`CreateTime` DESC, `Test`.`Id` DESC", orderByClause);
        }

        [TestMethod]
        public void TestMultiTable2()
        {
            var sqlCommand = SqlFactory.CreateOrderByClauseBuilder<TestEntity>(DatabaseType.MySql)
                .OrderBy(OrderByType.Desc, entity => entity.CreateTime)
                .OrderBy<CheckInLogEntity>(OrderByType.Desc, entity => entity.Id)
                .Build();
            var orderByClause = sqlCommand.Sql;
            Assert.AreEqual("`Test`.`CreateTime` DESC, `CheckInLog`.`Id` DESC", orderByClause);
        }
        #endregion
    }
}
