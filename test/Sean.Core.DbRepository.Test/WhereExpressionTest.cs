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
    public class WhereExpressionTest : TestBase
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
                { "UserId", 10001L }
            };
            Assert.AreEqual("`UserId` = @UserId", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
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
                { "Age", 18 }
            };
            Assert.AreEqual("`Age` >= @Age", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// 常量
        /// </summary>
        [TestMethod]
        public void ValidateConstant3()
        {
            Expression<Func<TestEntity, bool>> whereExpression = entity => 18 <= entity.Age;
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { "Age", 18 }
            };
            Assert.AreEqual("`Age` >= @Age", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
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
                { "Age", age }
            };
            Assert.AreEqual("`Age` >= @Age", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
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
                { "UserId", _model.UserId }
            };
            Assert.AreEqual("`UserId` = @UserId", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
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
                { "IsVip", true }
            };
            Assert.AreEqual("`IsVip` = @IsVip", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
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
                { "IsVip", false }
            };
            Assert.AreEqual("`IsVip` = @IsVip", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// DateTime
        /// </summary>
        [TestMethod]
        public void ValidateDateTime()
        {
            var startTime = DateTime.Today;
            var endTime = DateTime.Today.AddDays(1);
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.CreateTime >= startTime
                                                                           && entity.CreateTime < startTime.AddDays(1);
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { "CreateTime", startTime },
                { "CreateTime_2", endTime },
            };
            Assert.AreEqual("`CreateTime` >= @CreateTime AND `CreateTime` < @CreateTime_2", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// DateTime
        /// </summary>
        [TestMethod]
        public void ValidateDateTime2()
        {
            var startTime = DateTime.Today;
            var endTime = DateTime.Today.AddDays(1);
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.CreateTime >= DateTime.Today
                                                                           && entity.CreateTime < DateTime.Today.AddDays(1);
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { "CreateTime", startTime },
                { "CreateTime_2", endTime },
            };
            Assert.AreEqual("`CreateTime` >= @CreateTime AND `CreateTime` < @CreateTime_2", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// DateTime?
        /// </summary>
        [TestMethod]
        public void ValidateDateTimeNullable()
        {
            var startTime = DateTime.Today;
            var endTime = DateTime.Today.AddDays(1);
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.UpdateTime.HasValue
                                                                           && entity.UpdateTime >= startTime
                                                                           && entity.UpdateTime < endTime;
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { "UpdateTime", startTime },
                { "UpdateTime_2", endTime },
            };
            Assert.AreEqual("`UpdateTime` is not null AND `UpdateTime` >= @UpdateTime AND `UpdateTime` < @UpdateTime_2", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
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
                { "Sex", (int)SexType.Male }
            };
            Assert.AreEqual("`Sex` = @Sex", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// Enum
        /// </summary>
        [TestMethod]
        public void ValidateEnum2()
        {
            Expression<Func<TestEntity, bool>> whereExpression = entity => SexType.Male == entity.Sex;
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { "Sex", (int)SexType.Male }
            };
            Assert.AreEqual("`Sex` = @Sex", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// Enum
        /// </summary>
        [TestMethod]
        public void ValidateEnum3()
        {
            SexType[] sexTypes = { SexType.Male, SexType.Female };
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.Sex == sexTypes[1];
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { "Sex", (int)SexType.Female }
            };
            Assert.AreEqual("`Sex` = @Sex", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// &&
        /// </summary>
        [TestMethod]
        public void ValidateAnd()
        {
            var accountBalance = 100M;
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.UserId == _model.UserId
                                                                           && entity.AccountBalance < accountBalance;
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { "UserId", _model.UserId },
                { "AccountBalance", accountBalance }
            };
            Assert.AreEqual("`UserId` = @UserId AND `AccountBalance` < @AccountBalance", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// &&
        /// </summary>
        [TestMethod]
        public void ValidateAnd2()
        {
            var accountBalance = 100M;
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.UserId == _model.UserId
                                                                           && entity.AccountBalance < accountBalance
                                                                           && !entity.IsBlack;
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { "UserId", _model.UserId },
                { "AccountBalance", accountBalance },
                { "IsBlack", false },
            };
            Assert.AreEqual("`UserId` = @UserId AND `AccountBalance` < @AccountBalance AND `IsBlack` = @IsBlack", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// ||
        /// </summary>
        [TestMethod]
        public void ValidateOr()
        {
            var accountBalance = 100M;
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.UserId == _model.UserId
                                                                           || entity.AccountBalance >= accountBalance;
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { "UserId", _model.UserId },
                { "AccountBalance", accountBalance }
            };
            Assert.AreEqual("(`UserId` = @UserId OR `AccountBalance` >= @AccountBalance)", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// ||
        /// </summary>
        [TestMethod]
        public void ValidateOr2()
        {
            var accountBalance = 100M;
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.UserId == _model.UserId
                                                                           || entity.AccountBalance >= accountBalance && entity.IsVip;
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { "UserId", _model.UserId },
                { "AccountBalance", accountBalance },
                { "IsVip", true }
            };
            Assert.AreEqual("(`UserId` = @UserId OR `AccountBalance` >= @AccountBalance AND `IsVip` = @IsVip)", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// ||
        /// </summary>
        [TestMethod]
        public void ValidateOr3()
        {
            var accountBalance = 100M;
            Expression<Func<TestEntity, bool>> whereExpression = entity => (entity.UserId == _model.UserId || entity.AccountBalance >= accountBalance)
                                                                           && entity.IsVip;
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { "UserId", _model.UserId },
                { "AccountBalance", accountBalance },
                { "IsVip", true }
            };
            Assert.AreEqual("(`UserId` = @UserId OR `AccountBalance` >= @AccountBalance) AND `IsVip` = @IsVip", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
        }

        [TestMethod]
        public void ValidateMultiCondition()
        {
            var accountBalance = 100M;
            Expression<Func<TestEntity, bool>> whereExpression = entity => (entity.UserId == _model.UserId || entity.AccountBalance >= accountBalance)
                                                                           && entity.IsVip
                                                                           && entity.Age > 20
                                                                           && (!entity.IsBlack || entity.Country == CountryType.China);
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { "UserId", _model.UserId },
                { "AccountBalance", accountBalance },
                { "IsVip", true },
                { "Age", 20 },
                { "IsBlack", false },
                { "Country", (int)CountryType.China }
            };
            Assert.AreEqual("(`UserId` = @UserId OR `AccountBalance` >= @AccountBalance) AND `IsVip` = @IsVip AND `Age` > @Age AND (`IsBlack` = @IsBlack OR `Country` = @Country)", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
        }

        [TestMethod]
        public void ValidateMultiCondition2()
        {
            var accountBalance = 100M;
            Expression<Func<TestEntity, bool>> whereExpression = entity => (entity.UserId == _model.UserId || entity.AccountBalance >= accountBalance)
                                                                           && entity.IsVip
                                                                           || entity.Age > 20
                                                                           && (!entity.IsBlack || entity.Country == CountryType.China);
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { "UserId", _model.UserId },
                { "AccountBalance", accountBalance },
                { "IsVip", true },
                { "Age", 20 },
                { "IsBlack", false },
                { "Country", (int)CountryType.China },
            };
            Assert.AreEqual("((`UserId` = @UserId OR `AccountBalance` >= @AccountBalance) AND `IsVip` = @IsVip OR `Age` > @Age AND (`IsBlack` = @IsBlack OR `Country` = @Country))", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
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
                { "Remark", "测试%" }
            };
            Assert.AreEqual("`Remark` LIKE @Remark", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
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
                { "Remark", $"%{key}" }
            };
            Assert.AreEqual("`Remark` LIKE @Remark", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
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
                { "Remark", $"%{model.UserName}" }
            };
            Assert.AreEqual("`Remark` LIKE @Remark", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
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
                { "Remark", $"%{key}%" }
            };
            Assert.AreEqual("`Remark` LIKE @Remark", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
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
                { "Remark", $"%{model.UserName}%" }
            };
            Assert.AreEqual("`Remark` LIKE @Remark", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
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
                { "UserName", values }
            };
            Assert.AreEqual("`UserName` IN @UserName", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
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
                { "UserName", values }
            };
            Assert.AreEqual("`UserName` IN @UserName", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
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
                { "UserName", values }
            };
            Assert.AreEqual("`UserName` IN @UserName", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
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
                { "UserName", values }
            };
            Assert.AreEqual("`UserName` IN @UserName", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
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
                { "UserName", values }
            };
            Assert.AreEqual("`UserName` IN @UserName", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
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
                { "UserName", "测试" }
            };
            Assert.AreEqual("`UserName` = @UserName", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
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
                { "UserName", userName }
            };
            Assert.AreEqual("`UserName` = @UserName", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
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
                { "UserName", userName }
            };
            Assert.AreEqual("`UserName` <> @UserName", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// HasValue
        /// </summary>
        [TestMethod]
        public void ValidateHasValue()
        {
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.UpdateTime.HasValue;
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>();
            Assert.AreEqual("`UpdateTime` is not null", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// HasValue
        /// </summary>
        [TestMethod]
        public void ValidateHasValue2()
        {
            Expression<Func<TestEntity, bool>> whereExpression = entity => !entity.UpdateTime.HasValue;
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>();
            Assert.AreEqual("`UpdateTime` is null", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// HasValue
        /// </summary>
        [TestMethod]
        public void ValidateHasValue3()
        {
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.IsVip
                                                                           && entity.UpdateTime.HasValue;
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                {nameof(TestEntity.IsVip),true}
            };
            Assert.AreEqual("`IsVip` = @IsVip AND `UpdateTime` is not null", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
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
            Assert.AreEqual("(`Email` is null OR `Email` = '')", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
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
            Assert.AreEqual("`Email` is not null AND `Email` <> ''", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// Nullable value in 'whereExpression'.
        /// </summary>
        [TestMethod]
        public void ValidateNullableValue()
        {
            long? excludeId = 10001;
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.Id != excludeId;
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { "Id", 10001L }
            };
            Assert.AreEqual("`Id` <> @Id", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// Nullable value in 'whereExpression'.
        /// </summary>
        [TestMethod]
        public void ValidateNullableValue2()
        {
            long? excludeId = null;
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.Id != excludeId;
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>();
            Assert.AreEqual("`Id` is not null", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// Nullable value in 'whereExpression'.
        /// </summary>
        [TestMethod]
        public void ValidateNullableValue3()
        {
            long? excludeId = 10001;
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.Id != excludeId.Value;
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { "Id", 10001L }
            };
            Assert.AreEqual("`Id` <> @Id", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// Nullable value in 'whereExpression'.
        /// </summary>
        [TestMethod]
        public void ValidateNullableValue4()
        {
            long? excludeId = null;
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.Id != excludeId.Value;
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>();
            Assert.AreEqual("`Id` is not null", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
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
            Assert.AreEqual("`Email` is null", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
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
            Assert.AreEqual("`Email` is not null", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
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
            AssertSqlParameters(expectedParameters, parameters);
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
            AssertSqlParameters(expectedParameters, parameters);
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
            AssertSqlParameters(expectedParameters, parameters);
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
            AssertSqlParameters(expectedParameters, parameters);
        }

        /// <summary>
        /// 动态Lambda表达式
        /// </summary>
        [TestMethod]
        public void ValidateDynamicLambdaExpression()
        {
            Expression<Func<TestEntity, bool>> whereExpression = entity => true;
            whereExpression = whereExpression.AndAlso(entity => entity.Age > 18 && entity.Age < 25);
            whereExpression = whereExpression.AndAlso(entity => entity.IsVip);
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { "Age", 18 },
                { "Age_2", 25 },
                { "IsVip", true }
            };
            Assert.AreEqual("1=1 AND `Age` > @Age AND `Age` < @Age_2 AND `IsVip` = @IsVip", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
        }
        /// <summary>
        /// 动态Lambda表达式
        /// </summary>
        [TestMethod]
        public void ValidateDynamicLambdaExpression2()
        {
            Expression<Func<TestEntity, bool>> whereExpression = entity => true;
            whereExpression = whereExpression.AndAlso(entity => entity.Age > 18 && entity.Age < 25);
            whereExpression = whereExpression.AndAlso(entity => entity.IsVip);
            Expression<Func<TestEntity, bool>> whereExpression2 = entity => false;
            whereExpression2 = whereExpression2.OrElse(entity => entity.Country == CountryType.China);
            whereExpression2 = whereExpression2.OrElse(entity => entity.AccountBalance > 8888);
            whereExpression = whereExpression.AndAlso(whereExpression2);
            var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, out var parameters);
            var expectedParameters = new Dictionary<string, object>
            {
                { "Age", 18 },
                { "Age_2", 25 },
                { "IsVip", true },
                { "Country", 1 },
                { "AccountBalance", 8888M }
            };
            Assert.AreEqual("1=1 AND `Age` > @Age AND `Age` < @Age_2 AND `IsVip` = @IsVip AND ((1=2 OR `Country` = @Country) OR `AccountBalance` > @AccountBalance)", whereClause);
            AssertSqlParameters(expectedParameters, parameters);
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
    }
}
