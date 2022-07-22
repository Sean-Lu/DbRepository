using System;
using System.Collections.Generic;
using System.Text;
using Example.Domain.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sean.Core.DbRepository.Test
{
    /// <summary>
    /// <see cref="SqlWhereClauseBuilder{TEntity}"/>
    /// </summary>
    [TestClass]
    public class SqlWhereClauseBuilderTest
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
            var sqlWhereClauseBuilder = SqlWhereClauseBuilder<TestEntity>.Create(_sqlAdapter);
            sqlWhereClauseBuilder.Where(entity => entity.UserId == 10010 && !string.IsNullOrEmpty(entity.PhoneNumber) && entity.IsVip);
            var whereClause = sqlWhereClauseBuilder.GetParameterizedWhereClause();
            var parameters = sqlWhereClauseBuilder.Parameter;
            var expectedParameters = new Dictionary<string, object>
            {
               { nameof(TestEntity.UserId), 10010L },
               { nameof(TestEntity.IsVip), true }
            };
            Assert.AreEqual($"`UserId` = @UserId AND `PhoneNumber` is not null AND `PhoneNumber` <> '' AND `IsVip` = @IsVip", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        [TestMethod]
        public void ValidateMultiExpression()
        {
            var sqlWhereClauseBuilder = SqlWhereClauseBuilder<TestEntity>.Create(_sqlAdapter);
            sqlWhereClauseBuilder.Where(entity => entity.UserId == 10010 || entity.IsVip);
            sqlWhereClauseBuilder.Where(entity => !string.IsNullOrEmpty(entity.PhoneNumber));
            var whereClause = sqlWhereClauseBuilder.GetParameterizedWhereClause();
            var parameters = sqlWhereClauseBuilder.Parameter;
            var expectedParameters = new Dictionary<string, object>
            {
                { nameof(TestEntity.UserId), 10010L },
                { nameof(TestEntity.IsVip), true }
            };
            Assert.AreEqual($"(`UserId` = @UserId OR `IsVip` = @IsVip) AND `PhoneNumber` is not null AND `PhoneNumber` <> ''", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        private void AssertParameters(IDictionary<string, object> expectedParameters, IDictionary<string, object> actualParameters)
        {
            Assert.AreEqual(expectedParameters.Count, actualParameters.Count);
            foreach (var key in expectedParameters.Keys)
            {
                Assert.IsTrue(actualParameters.ContainsKey(key), $"The {nameof(actualParameters)} does not contain key <{key}>.");
                Assert.IsTrue(expectedParameters[key].Equals(actualParameters[key]), $"The expected value is <{expectedParameters[key]}>, the actual value is <{actualParameters[key]}>.");
            }
        }
    }
}
