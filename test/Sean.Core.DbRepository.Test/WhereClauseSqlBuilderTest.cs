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
        public void ValidateSingleExpression()
        {
            var sqlCommand = WhereClauseSqlBuilder<TestEntity>.Create(_sqlAdapter)
                .Where(entity => entity.UserId == 10010 && !string.IsNullOrEmpty(entity.PhoneNumber) && entity.IsVip)
                .Build();
            var whereClause = sqlCommand.Sql;
            var parameters = sqlCommand.Parameter;
            var expectedParameters = new Dictionary<string, object>
            {
               { "UserId", 10010L },
               { "IsVip", true }
            };
            Assert.AreEqual($"`UserId` = @UserId AND `PhoneNumber` IS NOT NULL AND `PhoneNumber` <> '' AND `IsVip` = @IsVip", whereClause);
            AssertSqlParameters(expectedParameters, SqlParameterUtil.ConvertToDicParameter(parameters));
        }

        [TestMethod]
        public void ValidateMultiExpression()
        {
            var sqlCommand = WhereClauseSqlBuilder<TestEntity>.Create(_sqlAdapter)
                .Where(entity => entity.UserId == 10010 || entity.IsVip)
                .Where(entity => !string.IsNullOrEmpty(entity.PhoneNumber))
                .Build();
            var whereClause = sqlCommand.Sql;
            var parameters = sqlCommand.Parameter;
            var expectedParameters = new Dictionary<string, object>
            {
                { "UserId", 10010L },
                { "IsVip", true }
            };
            Assert.AreEqual($"(`UserId` = @UserId OR `IsVip` = @IsVip) AND `PhoneNumber` IS NOT NULL AND `PhoneNumber` <> ''", whereClause);
            AssertSqlParameters(expectedParameters, SqlParameterUtil.ConvertToDicParameter(parameters));
        }

        [TestMethod]
        //public void ValidateComplexExpression()
        public void ValidateNotSqlParameterized()
        {
            var sqlCommand = WhereClauseSqlBuilder<TestEntity>.Create(_sqlAdapter)
                .Where(entity => entity.UserId == 10010 || entity.IsVip)
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
            Assert.AreEqual($"(`UserId` = 10010 OR `IsVip` = 1) AND `PhoneNumber` IS NOT NULL AND `PhoneNumber` <> ''", whereClause);
            AssertSqlParameters(expectedParameters, SqlParameterUtil.ConvertToDicParameter(parameters));
        }

        [TestMethod]
        public void ValidateNotSqlParameterized2()
        {
            var sqlCommand = WhereClauseSqlBuilder<TestEntity>.Create(_sqlAdapter)
                .Where(entity => entity.UserId == 10010 || entity.IsVip)
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
            Assert.AreEqual($"(`Test`.`UserId` = 10010 OR `Test`.`IsVip` = 1) AND `CheckInLog`.`CreateTime` > '2024-09-25 12:00:00' AND `CheckInLog`.`UserId` = 101", whereClause);
            AssertSqlParameters(expectedParameters, SqlParameterUtil.ConvertToDicParameter(parameters));
        }
    }
}
