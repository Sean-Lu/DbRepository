using System;
using System.Collections.Generic;
using Example.Dapper.Core.Domain.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sean.Core.DbRepository.Util;

namespace Sean.Core.DbRepository.Test
{
    /// <summary>
    /// <see cref="WhereClauseSqlBuilder{TEntity}"/>
    /// </summary>
    [TestClass]
    public class WhereClauseSqlBuilderTest : TestBase
    {
        private readonly ISqlAdapter _sqlAdapter;

        public WhereClauseSqlBuilderTest()
        {
            _sqlAdapter = new DefaultSqlAdapter<TestEntity>(DatabaseType.MySql);
        }

        [TestMethod]
        public void ValidateSingleTableSqlParameterized()
        {
            var sqlCommand = WhereClauseSqlBuilder<TestEntity>.Create(_sqlAdapter)
                .Where(entity => entity.UserId == 10010L || entity.IsVip)
                .Where(entity => !string.IsNullOrEmpty(entity.PhoneNumber))
                .Build();
            var whereClause = sqlCommand.Sql;
            var parameters = sqlCommand.Parameter;
            var expectedParameters = new Dictionary<string, object>
            {
                { "UserId", 10010L },
                { "IsVip", true }
            };
            Assert.AreEqual("(`UserId` = @UserId OR `IsVip` = @IsVip) AND `PhoneNumber` IS NOT NULL AND `PhoneNumber` <> ''", whereClause);
            AssertSqlParameters(expectedParameters, SqlParameterUtil.ConvertToDicParameter(parameters));
        }

        [TestMethod]
        public void ValidateSingleTableNotSqlParameterized()
        {
            var sqlCommand = WhereClauseSqlBuilder<TestEntity>.Create(_sqlAdapter)
                .Where(entity => entity.UserId == 10010L || entity.IsVip)
                .Where(entity => !string.IsNullOrEmpty(entity.PhoneNumber))
                .SetSqlParameterized(false)
                .Build();
            var whereClause = sqlCommand.Sql;
            var parameters = sqlCommand.Parameter;
            var expectedParameters = new Dictionary<string, object>
            {
                { "UserId", 10010L },
                { "IsVip", true }
            };
            Assert.AreEqual("(`UserId` = 10010 OR `IsVip` = 1) AND `PhoneNumber` IS NOT NULL AND `PhoneNumber` <> ''", whereClause);
            AssertSqlParameters(expectedParameters, SqlParameterUtil.ConvertToDicParameter(parameters));
        }

        [TestMethod]
        public void ValidateMultiTableSqlParameterized()
        {
            var sqlCommand = WhereClauseSqlBuilder<TestEntity>.Create(_sqlAdapter)
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
        public void ValidateMultiTableNotSqlParameterized()
        {
            var sqlCommand = WhereClauseSqlBuilder<TestEntity>.Create(_sqlAdapter)
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

        #region DatabaseType
        [TestMethod]
        public void ValidateMsAccessSqlParameterized()
        {
            var sqlCommand = WhereClauseSqlBuilder<TestEntity>.Create(DatabaseType.MsAccess)
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
        public void ValidateMsAccessNotSqlParameterized()
        {
            var sqlCommand = WhereClauseSqlBuilder<TestEntity>.Create(DatabaseType.MsAccess)
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
        public void ValidateDuckDBSqlParameterized()
        {
            var sqlCommand = WhereClauseSqlBuilder<TestEntity>.Create(DatabaseType.DuckDB)
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
        public void ValidateDuckDBNotSqlParameterized()
        {
            var sqlCommand = WhereClauseSqlBuilder<TestEntity>.Create(DatabaseType.DuckDB)
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
        public void ValidateUnknownSqlParameterized()
        {
            var sqlCommand = WhereClauseSqlBuilder<TestEntity>.Create(DatabaseType.Unknown)
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
        public void ValidateUnknownNotSqlParameterized()
        {
            var sqlCommand = WhereClauseSqlBuilder<TestEntity>.Create(DatabaseType.Unknown)
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
    }
}
