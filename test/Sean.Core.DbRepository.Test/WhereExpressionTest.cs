using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Example.Domain.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository.Test
{
    /// <summary>
    /// Expression表达式树测试：whereExpression
    /// </summary>
    [TestClass]
    public class WhereExpressionTest
    {
        private readonly ISqlAdapter _sqlAdapter;
        private readonly TestEntity _model;

        public WhereExpressionTest()
        {
            _sqlAdapter = new DefaultSqlAdapter(DatabaseType.MySql, null);
            _model = new TestEntity
            {
                UserId = 10001
            };
        }

        #region 支持的写法
        /// <summary>
        /// 常量
        /// </summary>
        [TestMethod]
        public void ValidateConstant()
        {
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.UserId == 10001L;
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { nameof(TestEntity.UserId), 10001L }
            };
            Assert.AreEqual($"{_sqlAdapter.FormatFieldName(nameof(TestEntity.UserId))} = {_sqlAdapter.FormatInputParameter(nameof(TestEntity.UserId))}", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// 常量
        /// </summary>
        [TestMethod]
        public void ValidateConstant2()
        {
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.Age >= 18;
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { nameof(TestEntity.Age), 18 }
            };
            Assert.AreEqual($"{_sqlAdapter.FormatFieldName(nameof(TestEntity.Age))} >= {_sqlAdapter.FormatInputParameter(nameof(TestEntity.Age))}", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// 变量
        /// </summary>
        [TestMethod]
        public void ValidateVariable()
        {
            var age = 18;
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.Age >= age;
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { nameof(TestEntity.Age), age }
            };
            Assert.AreEqual($"{_sqlAdapter.FormatFieldName(nameof(TestEntity.Age))} >= {_sqlAdapter.FormatInputParameter(nameof(TestEntity.Age))}", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// 变量
        /// </summary>
        [TestMethod]
        public void ValidateVariable2()
        {
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.UserId == _model.UserId;
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { nameof(TestEntity.UserId), _model.UserId }
            };
            Assert.AreEqual($"{_sqlAdapter.FormatFieldName(nameof(TestEntity.UserId))} = {_sqlAdapter.FormatInputParameter(nameof(TestEntity.UserId))}", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// bool
        /// </summary>
        [TestMethod]
        public void ValidateBool()
        {
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.IsVip;
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { nameof(TestEntity.IsVip), true }
            };
            Assert.AreEqual($"{_sqlAdapter.FormatFieldName(nameof(TestEntity.IsVip))} = {_sqlAdapter.FormatInputParameter(nameof(TestEntity.IsVip))}", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// bool
        /// </summary>
        [TestMethod]
        public void ValidateBool2()
        {
            Expression<Func<TestEntity, bool>> whereExpression = entity => !entity.IsVip;
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { nameof(TestEntity.IsVip), false }
            };
            Assert.AreEqual($"{_sqlAdapter.FormatFieldName(nameof(TestEntity.IsVip))} = {_sqlAdapter.FormatInputParameter(nameof(TestEntity.IsVip))}", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// DateTime
        /// </summary>
        [TestMethod]
        public void ValidateDateTime()
        {
            var startTime = DateTime.Today;
            var endTime = DateTime.Today.AddDays(1);
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.CreateTime >= startTime && entity.CreateTime < startTime.AddDays(1);
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { $"{nameof(TestEntity.CreateTime)}", startTime },
                { $"{nameof(TestEntity.CreateTime)}_2", endTime },
            };
            Assert.AreEqual($"({_sqlAdapter.FormatFieldName(nameof(TestEntity.CreateTime))} >= {_sqlAdapter.FormatInputParameter(nameof(TestEntity.CreateTime))})" +
                            $" AND ({_sqlAdapter.FormatFieldName(nameof(TestEntity.CreateTime))} < {_sqlAdapter.FormatInputParameter($"{nameof(TestEntity.CreateTime)}_2")})", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// DateTime
        /// </summary>
        [TestMethod]
        public void ValidateDateTime2()
        {
            var startTime = DateTime.Today;
            var endTime = DateTime.Today.AddDays(1);
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.CreateTime >= DateTime.Today && entity.CreateTime < DateTime.Today.AddDays(1);
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { $"{nameof(TestEntity.CreateTime)}", startTime },
                { $"{nameof(TestEntity.CreateTime)}_2", endTime },
            };
            Assert.AreEqual($"({_sqlAdapter.FormatFieldName(nameof(TestEntity.CreateTime))} >= {_sqlAdapter.FormatInputParameter(nameof(TestEntity.CreateTime))})" +
                            $" AND ({_sqlAdapter.FormatFieldName(nameof(TestEntity.CreateTime))} < {_sqlAdapter.FormatInputParameter($"{nameof(TestEntity.CreateTime)}_2")})", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// Enum
        /// </summary>
        [TestMethod]
        public void ValidateEnum()
        {
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.Sex == SexType.Male;
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { $"{nameof(TestEntity.Sex)}", (int)SexType.Male }
            };
            Assert.AreEqual($"{_sqlAdapter.FormatFieldName(nameof(TestEntity.Sex))} = {_sqlAdapter.FormatInputParameter(nameof(TestEntity.Sex))}", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// Enum
        /// </summary>
        [TestMethod]
        public void ValidateEnum2()
        {
            SexType[] sexTypes = { SexType.Male, SexType.Female };
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.Sex == sexTypes[1];
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { $"{nameof(TestEntity.Sex)}", (int)SexType.Female }
            };
            Assert.AreEqual($"{_sqlAdapter.FormatFieldName(nameof(TestEntity.Sex))} = {_sqlAdapter.FormatInputParameter(nameof(TestEntity.Sex))}", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// &&
        /// </summary>
        [TestMethod]
        public void ValidateAnd()
        {
            var accountBalance = 100M;
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.UserId == _model.UserId && entity.AccountBalance < accountBalance;
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { nameof(TestEntity.UserId), _model.UserId },
                { nameof(TestEntity.AccountBalance), accountBalance }
            };
            Assert.AreEqual($"({_sqlAdapter.FormatFieldName(nameof(TestEntity.UserId))} = {_sqlAdapter.FormatInputParameter(nameof(TestEntity.UserId))})" +
                            $" AND ({_sqlAdapter.FormatFieldName(nameof(TestEntity.AccountBalance))} < {_sqlAdapter.FormatInputParameter(nameof(TestEntity.AccountBalance))})", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// &&
        /// </summary>
        [TestMethod]
        public void ValidateAnd2()
        {
            var accountBalance = 100M;
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.UserId == _model.UserId && entity.AccountBalance < accountBalance && !entity.IsBlack;
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { nameof(TestEntity.UserId), _model.UserId },
                { nameof(TestEntity.AccountBalance), accountBalance },
                { nameof(TestEntity.IsBlack), false },
            };
            Assert.AreEqual($"(({_sqlAdapter.FormatFieldName(nameof(TestEntity.UserId))} = {_sqlAdapter.FormatInputParameter(nameof(TestEntity.UserId))})" +
                            $" AND ({_sqlAdapter.FormatFieldName(nameof(TestEntity.AccountBalance))} < {_sqlAdapter.FormatInputParameter(nameof(TestEntity.AccountBalance))}))" +
                            $" AND ({_sqlAdapter.FormatFieldName(nameof(TestEntity.IsBlack))} = {_sqlAdapter.FormatInputParameter(nameof(TestEntity.IsBlack))})", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// ||
        /// </summary>
        [TestMethod]
        public void ValidateOr()
        {
            var accountBalance = 100M;
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.UserId == _model.UserId || entity.AccountBalance >= accountBalance;
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { nameof(TestEntity.UserId), _model.UserId },
                { nameof(TestEntity.AccountBalance), accountBalance }
            };
            Assert.AreEqual($"({_sqlAdapter.FormatFieldName(nameof(TestEntity.UserId))} = {_sqlAdapter.FormatInputParameter(nameof(TestEntity.UserId))})" +
                            $" OR ({_sqlAdapter.FormatFieldName(nameof(TestEntity.AccountBalance))} >= {_sqlAdapter.FormatInputParameter(nameof(TestEntity.AccountBalance))})", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// ||
        /// </summary>
        [TestMethod]
        public void ValidateOr2()
        {
            var accountBalance = 100M;
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.UserId == _model.UserId || entity.AccountBalance >= accountBalance && entity.IsVip;
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { nameof(TestEntity.UserId), _model.UserId },
                { nameof(TestEntity.AccountBalance), accountBalance },
                { nameof(TestEntity.IsVip), true },
            };
            Assert.AreEqual($"({_sqlAdapter.FormatFieldName(nameof(TestEntity.UserId))} = {_sqlAdapter.FormatInputParameter(nameof(TestEntity.UserId))})" +
                            $" OR (({_sqlAdapter.FormatFieldName(nameof(TestEntity.AccountBalance))} >= {_sqlAdapter.FormatInputParameter(nameof(TestEntity.AccountBalance))})" +
                            $" AND ({_sqlAdapter.FormatFieldName(nameof(TestEntity.IsVip))} = {_sqlAdapter.FormatInputParameter(nameof(TestEntity.IsVip))}))", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// ||
        /// </summary>
        [TestMethod]
        public void ValidateOr3()
        {
            var accountBalance = 100M;
            Expression<Func<TestEntity, bool>> whereExpression = entity => (entity.UserId == _model.UserId || entity.AccountBalance >= accountBalance) && entity.IsVip;
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { nameof(TestEntity.UserId), _model.UserId },
                { nameof(TestEntity.AccountBalance), accountBalance },
                { nameof(TestEntity.IsVip), true },
            };
            Assert.AreEqual($"(({_sqlAdapter.FormatFieldName(nameof(TestEntity.UserId))} = {_sqlAdapter.FormatInputParameter(nameof(TestEntity.UserId))})" +
                            $" OR ({_sqlAdapter.FormatFieldName(nameof(TestEntity.AccountBalance))} >= {_sqlAdapter.FormatInputParameter(nameof(TestEntity.AccountBalance))}))" +
                            $" AND ({_sqlAdapter.FormatFieldName(nameof(TestEntity.IsVip))} = {_sqlAdapter.FormatInputParameter(nameof(TestEntity.IsVip))})", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// StartsWith
        /// </summary>
        [TestMethod]
        public void ValidateStartsWith()
        {
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.Remark.StartsWith("测试");
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { nameof(TestEntity.Remark), "测试%" }
            };
            Assert.AreEqual($"{_sqlAdapter.FormatFieldName(nameof(TestEntity.Remark))} LIKE {_sqlAdapter.FormatInputParameter(nameof(TestEntity.Remark))}", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// EndsWith
        /// </summary>
        [TestMethod]
        public void ValidateEndsWith()
        {
            var key = "测试";
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.Remark.EndsWith(key);
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { nameof(TestEntity.Remark), $"%{key}" }
            };
            Assert.AreEqual($"{_sqlAdapter.FormatFieldName(nameof(TestEntity.Remark))} LIKE {_sqlAdapter.FormatInputParameter(nameof(TestEntity.Remark))}", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// EndsWith
        /// </summary>
        [TestMethod]
        public void ValidateEndsWith2()
        {
            var model = new TestEntity { UserName = "Test" };
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.Remark.EndsWith(model.UserName);
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { nameof(TestEntity.Remark), $"%{model.UserName}" }
            };
            Assert.AreEqual($"{_sqlAdapter.FormatFieldName(nameof(TestEntity.Remark))} LIKE {_sqlAdapter.FormatInputParameter(nameof(TestEntity.Remark))}", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// Contains（string）
        /// </summary>
        [TestMethod]
        public void ValidateStringContains()
        {
            var key = "测试";
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.Remark.Contains(key);
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { nameof(TestEntity.Remark), $"%{key}%" }
            };
            Assert.AreEqual($"{_sqlAdapter.FormatFieldName(nameof(TestEntity.Remark))} LIKE {_sqlAdapter.FormatInputParameter(nameof(TestEntity.Remark))}", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// Contains（string）
        /// </summary>
        [TestMethod]
        public void ValidateStringContains2()
        {
            var model = new TestEntity { UserName = "Test" };
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.Remark.Contains(model.UserName);
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { nameof(TestEntity.Remark), $"%{model.UserName}%" }
            };
            Assert.AreEqual($"{_sqlAdapter.FormatFieldName(nameof(TestEntity.Remark))} LIKE {_sqlAdapter.FormatInputParameter(nameof(TestEntity.Remark))}", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// Contains（数组）
        /// </summary>
        [TestMethod]
        public void ValidateArrayContains()
        {
            string[] values = { "a", "b" };
            Expression<Func<TestEntity, bool>> whereExpression = entity => values.Contains(entity.UserName);
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { nameof(TestEntity.UserName), values }
            };
            Assert.AreEqual($"{_sqlAdapter.FormatFieldName(nameof(TestEntity.UserName))} IN {_sqlAdapter.FormatInputParameter(nameof(TestEntity.UserName))}", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// Contains（List）
        /// </summary>
        [TestMethod]
        public void ValidateListContains()
        {
            List<string> values = new List<string> { "a", "b" };
            Expression<Func<TestEntity, bool>> whereExpression = entity => values.Contains(entity.UserName);
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { nameof(TestEntity.UserName), values }
            };
            Assert.AreEqual($"{_sqlAdapter.FormatFieldName(nameof(TestEntity.UserName))} IN {_sqlAdapter.FormatInputParameter(nameof(TestEntity.UserName))}", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// Contains（IList）
        /// </summary>
        [TestMethod]
        public void ValidateIListContains()
        {
            IList<string> values = new List<string> { "a", "b" };
            Expression<Func<TestEntity, bool>> whereExpression = entity => values.Contains(entity.UserName);
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { nameof(TestEntity.UserName), values }
            };
            Assert.AreEqual($"{_sqlAdapter.FormatFieldName(nameof(TestEntity.UserName))} IN {_sqlAdapter.FormatInputParameter(nameof(TestEntity.UserName))}", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// Contains（ICollection）
        /// </summary>
        [TestMethod]
        public void ValidateICollectionContains2()
        {
            ICollection<string> values = new List<string> { "a", "b" };
            Expression<Func<TestEntity, bool>> whereExpression = entity => values.Contains(entity.UserName);
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { nameof(TestEntity.UserName), values }
            };
            Assert.AreEqual($"{_sqlAdapter.FormatFieldName(nameof(TestEntity.UserName))} IN {_sqlAdapter.FormatInputParameter(nameof(TestEntity.UserName))}", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// Contains（IEnumerable）
        /// </summary>
        [TestMethod]
        public void ValidateIEnumerableContains()
        {
            IEnumerable<string> values = new List<string> { "a", "b" };
            Expression<Func<TestEntity, bool>> whereExpression = entity => values.Contains(entity.UserName);
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { nameof(TestEntity.UserName), values }
            };
            Assert.AreEqual($"{_sqlAdapter.FormatFieldName(nameof(TestEntity.UserName))} IN {_sqlAdapter.FormatInputParameter(nameof(TestEntity.UserName))}", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// Equals
        /// </summary>
        [TestMethod]
        public void ValidateEqualsMethod()
        {
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.UserName.Equals("测试");
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { nameof(TestEntity.UserName), "测试" }
            };
            Assert.AreEqual($"{_sqlAdapter.FormatFieldName(nameof(TestEntity.UserName))} = {_sqlAdapter.FormatInputParameter(nameof(TestEntity.UserName))}", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// Equals
        /// </summary>
        [TestMethod]
        public void ValidateEqualsMethod2()
        {
            var userName = "测试";
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.UserName.Equals(userName);
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { nameof(TestEntity.UserName), userName }
            };
            Assert.AreEqual($"{_sqlAdapter.FormatFieldName(nameof(TestEntity.UserName))} = {_sqlAdapter.FormatInputParameter(nameof(TestEntity.UserName))}", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// Equals
        /// </summary>
        [TestMethod]
        public void ValidateEqualsMethod3()
        {
            var userName = "测试";
            Expression<Func<TestEntity, bool>> whereExpression = entity => !entity.UserName.Equals(userName);
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { nameof(TestEntity.UserName), userName }
            };
            Assert.AreEqual($"{_sqlAdapter.FormatFieldName(nameof(TestEntity.UserName))} <> {_sqlAdapter.FormatInputParameter(nameof(TestEntity.UserName))}", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// HasValue
        /// </summary>
        [TestMethod]
        public void ValidateHasValue()
        {
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.NullableTest.HasValue;
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>();
            Assert.AreEqual($"{_sqlAdapter.FormatFieldName(nameof(TestEntity.NullableTest))} is not null", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// HasValue
        /// </summary>
        [TestMethod]
        public void ValidateHasValue2()
        {
            Expression<Func<TestEntity, bool>> whereExpression = entity => !entity.NullableTest.HasValue;
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>();
            Assert.AreEqual($"{_sqlAdapter.FormatFieldName(nameof(TestEntity.NullableTest))} is null", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// HasValue
        /// </summary>
        [TestMethod]
        public void ValidateHasValue3()
        {
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.IsVip && entity.NullableTest.HasValue;
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                {nameof(TestEntity.IsVip),true}
            };
            Assert.AreEqual($"({_sqlAdapter.FormatFieldName(nameof(TestEntity.IsVip))} = {_sqlAdapter.FormatInputParameter(nameof(TestEntity.IsVip))})" +
                            $" AND ({_sqlAdapter.FormatFieldName(nameof(TestEntity.NullableTest))} is not null)", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// IsNullOrEmpty
        /// </summary>
        [TestMethod]
        public void ValidateIsNullOrEmpty()
        {
            Expression<Func<TestEntity, bool>> whereExpression = entity => string.IsNullOrEmpty(entity.Email);
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>();
            Assert.AreEqual($"({_sqlAdapter.FormatFieldName(nameof(TestEntity.Email))} is null OR {_sqlAdapter.FormatFieldName(nameof(TestEntity.Email))} = '')", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// IsNullOrEmpty
        /// </summary>
        [TestMethod]
        public void ValidateIsNullOrEmpty2()
        {
            Expression<Func<TestEntity, bool>> whereExpression = entity => !string.IsNullOrEmpty(entity.Email);
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>();
            Assert.AreEqual($"{_sqlAdapter.FormatFieldName(nameof(TestEntity.Email))} is not null AND {_sqlAdapter.FormatFieldName(nameof(TestEntity.Email))} <> ''", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// null
        /// </summary>
        [TestMethod]
        public void ValidateNullCompare()
        {
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.Email == null;
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>();
            Assert.AreEqual($"{_sqlAdapter.FormatFieldName(nameof(TestEntity.Email))} is null", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// null
        /// </summary>
        [TestMethod]
        public void ValidateNullCompare2()
        {
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.Email != null;
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>();
            Assert.AreEqual($"{_sqlAdapter.FormatFieldName(nameof(TestEntity.Email))} is not null", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// 没有任何条件
        /// </summary>
        [TestMethod]
        public void ValidateNoCondition()
        {
            Expression<Func<TestEntity, bool>> whereExpression = entity => true;
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>();
            Assert.AreEqual("1=1", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// 没有任何条件
        /// </summary>
        [TestMethod]
        public void ValidateNoCondition2()
        {
            Expression<Func<TestEntity, bool>> whereExpression = entity => false;
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>();
            Assert.AreEqual("1=2", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// 没有任何条件
        /// </summary>
        [TestMethod]
        public void ValidateNoCondition3()
        {
            Expression<Func<TestEntity, bool>> whereExpression = entity => 1 == 1;
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>();
            Assert.AreEqual("1=1", whereClause);
            AssertParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// 没有任何条件
        /// </summary>
        [TestMethod]
        public void ValidateNoCondition4()
        {
            Expression<Func<TestEntity, bool>> whereExpression = entity => 1 == 2;
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>();
            Assert.AreEqual("1=2", whereClause);
            AssertParameters(expectedParameters, parameters);
        }
        #endregion 支持的写法

        #region 不支持的写法
        /// <summary>
        /// string.IsNullOrWhiteSpace()【不支持，请考虑使用 string.IsNullOrEmpty() 代替】
        /// </summary>
        [TestMethod]
        public void ValidateIsNullOrWhiteSpace()
        {
            Expression<Func<TestEntity, bool>> whereExpression = entity => string.IsNullOrWhiteSpace(entity.Email);
            Assert.ThrowsException<NotSupportedException>(() =>
            {
                var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            });
        }

        /// <summary>
        /// !StartsWith【不支持，请考虑使用 StartsWith 代替】
        /// </summary>
        [TestMethod]
        public void ValidateNotStartsWith()
        {
            Expression<Func<TestEntity, bool>> whereExpression = entity => !entity.Remark.StartsWith("测试");
            Assert.ThrowsException<NotSupportedException>(() =>
            {
                var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            });
        }

        /// <summary>
        /// !EndsWith【不支持，请考虑使用 EndsWith 代替】
        /// </summary>
        [TestMethod]
        public void ValidateNotEndsWith()
        {
            Expression<Func<TestEntity, bool>> whereExpression = entity => !entity.Remark.EndsWith("测试");
            Assert.ThrowsException<NotSupportedException>(() =>
            {
                var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            });
        }

        /// <summary>
        /// !Contains【不支持，请考虑使用 Contains 代替】
        /// </summary>
        [TestMethod]
        public void ValidateNotContains()
        {
            Expression<Func<TestEntity, bool>> whereExpression = entity => !entity.Remark.Contains("测试");
            Assert.ThrowsException<NotSupportedException>(() =>
            {
                var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            });
        }
        #endregion 不支持的写法

        private void AssertParameters(Dictionary<string, object> expectedParameters, Dictionary<string, object> actualParameters)
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
