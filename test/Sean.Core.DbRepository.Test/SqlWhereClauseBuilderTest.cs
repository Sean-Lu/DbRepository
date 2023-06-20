using System;
using System.Collections.Generic;
using System.Text;
using Example.Dapper.Core.Domain.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sean.Core.DbRepository.Test
{
    /// <summary>
    /// <see cref="SqlWhereClauseBuilder{TEntity}"/>
    /// </summary>
    [TestClass]
    public class SqlWhereClauseBuilderTest : TestBase
    {
        private readonly ISqlAdapter _sqlAdapter;

        public SqlWhereClauseBuilderTest()
        {
            _sqlAdapter = new DefaultSqlAdapter<TestEntity>(DatabaseType.MySql);
        }

        [TestMethod]
        public void ValidateSingleExpression()
        {
            //var model = new TestEntity
            //{

            //};
            //var sqlWhereClauseBuilder = SqlWhereClauseBuilder<TestEntity>.Create(_sqlAdapter, model);
            var sqlWhereClauseBuilder = SqlWhereClauseBuilder<TestEntity>.Create(_sqlAdapter)
                .Where(entity => entity.UserId == 10010 && !string.IsNullOrEmpty(entity.PhoneNumber) && entity.IsVip);
            var whereClause = sqlWhereClauseBuilder.GetParameterizedWhereClause();
            var parameters = sqlWhereClauseBuilder.Parameter;
            var expectedParameters = new Dictionary<string, object>
            {
               { "UserId", 10010L },
               { "IsVip", true }
            };
            Assert.AreEqual($"`UserId` = @UserId AND `PhoneNumber` is not null AND `PhoneNumber` <> '' AND `IsVip` = @IsVip", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
        }

        [TestMethod]
        public void ValidateMultiExpression()
        {
            var sqlWhereClauseBuilder = SqlWhereClauseBuilder<TestEntity>.Create(_sqlAdapter)
                .Where(entity => entity.UserId == 10010 || entity.IsVip)
                .Where(entity => !string.IsNullOrEmpty(entity.PhoneNumber));
            var whereClause = sqlWhereClauseBuilder.GetParameterizedWhereClause();
            var parameters = sqlWhereClauseBuilder.Parameter;
            var expectedParameters = new Dictionary<string, object>
            {
                { "UserId", 10010L },
                { "IsVip", true }
            };
            Assert.AreEqual($"(`UserId` = @UserId OR `IsVip` = @IsVip) AND `PhoneNumber` is not null AND `PhoneNumber` <> ''", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
        }
    }
}
