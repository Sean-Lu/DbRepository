using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Example.Dapper.Core.Domain.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository.Test
{
    /// <summary>
    /// Expression表达式树测试：fieldExpression
    /// </summary>
    [TestClass]
    public class FieldExpressionTest
    {
        //private readonly TestEntity _model;

        public FieldExpressionTest()
        {
            //_model = new TestEntity
            //{
            //    UserId = 10001
            //};
        }

        #region 通过 TEntity 返回字段（推荐）
        /// <summary>
        /// 单个字段
        /// </summary>
        [TestMethod]
        public void ValidateSingleField()
        {
            Expression<Func<TestEntity, object>> fieldExpression = entity => entity.Status;
            var fields = fieldExpression.GetFieldNames();
            var expectedFields = new List<string>
            {
                nameof(TestEntity.Status)
            };
            AssertFields(expectedFields, fields);
        }

        /// <summary>
        /// 多个字段（匿名类型）
        /// </summary>
        [TestMethod]
        public void ValidateMultiField()
        {
            Expression<Func<TestEntity, object>> fieldExpression = entity => new { entity.Status, entity.UpdateTime };
            var fields = fieldExpression.GetFieldNames();
            var expectedFields = new List<string>
            {
                nameof(TestEntity.Status),
                nameof(TestEntity.UpdateTime)
            };
            AssertFields(expectedFields, fields);
        }

        /// <summary>
        /// 多个字段（数组）
        /// </summary>
        [TestMethod]
        public void ValidateMultiField2()
        {
            Expression<Func<TestEntity, object>> fieldExpression = entity => new object[] { entity.Status, entity.UpdateTime };// 建议使用匿名类型代替
            var fields = fieldExpression.GetFieldNames();
            var expectedFields = new List<string>
            {
                nameof(TestEntity.Status),
                nameof(TestEntity.UpdateTime)
            };
            AssertFields(expectedFields, fields);
        }

        /// <summary>
        /// 多个字段（IEnumerable）
        /// </summary>
        [TestMethod]
        public void ValidateMultiField3()
        {
            Expression<Func<TestEntity, object>> fieldExpression = entity => new List<object> { entity.Status, entity.UpdateTime };// 建议使用匿名类型代替
            var fields = fieldExpression.GetFieldNames();
            var expectedFields = new List<string>
            {
                nameof(TestEntity.Status),
                nameof(TestEntity.UpdateTime)
            };
            AssertFields(expectedFields, fields);
        }
        #endregion

        #region 不通过 TEntity 返回字段
        [TestMethod]
        public void ValidateConstant()
        {
            Expression<Func<TestEntity, object>> fieldExpression = entity => "Status";
            var fields = fieldExpression.GetFieldNames();
            var expectedFields = new List<string>
            {
                nameof(TestEntity.Status)
            };
            AssertFields(expectedFields, fields);
        }

        [TestMethod]
        public void ValidateNameof()
        {
            Expression<Func<TestEntity, object>> fieldExpression = entity => nameof(TestEntity.Status);
            var fields = fieldExpression.GetFieldNames();
            var expectedFields = new List<string>
            {
                nameof(TestEntity.Status)
            };
            AssertFields(expectedFields, fields);
        }

        [TestMethod]
        public void ValidateVariable()
        {
            var field = "Status";
            Expression<Func<TestEntity, object>> fieldExpression = entity => field;
            var fields = fieldExpression.GetFieldNames();
            var expectedFields = new List<string>
            {
                nameof(TestEntity.Status)
            };
            AssertFields(expectedFields, fields);
        }

        [TestMethod]
        public void ValidateVariable2()
        {
            var model = new TestEntity
            {
                Remark = "Status"
            };
            var field = model.Remark;
            Expression<Func<TestEntity, object>> fieldExpression = entity => field;
            var fields = fieldExpression.GetFieldNames();
            var expectedFields = new List<string>
            {
                nameof(TestEntity.Status)
            };
            AssertFields(expectedFields, fields);
        }

        [TestMethod]
        public void ValidateMemberAccess()
        {
            var model = new TestEntity
            {
                Remark = "Status"
            };
            Expression<Func<TestEntity, object>> fieldExpression = entity => model.Remark;
            var fields = fieldExpression.GetFieldNames();
            var expectedFields = new List<string>
            {
                nameof(TestEntity.Status)
            };
            AssertFields(expectedFields, fields);
        }

        /// <summary>
        /// List
        /// </summary>
        [TestMethod]
        public void ValidateList()
        {
            List<string> fieldList = new List<string>
            {
                nameof(TestEntity.Status),
                nameof(TestEntity.UpdateTime)
            };
            Expression<Func<TestEntity, object>> fieldExpression = entity => fieldList;
            var fields = fieldExpression.GetFieldNames();
            var expectedFields = new List<string>
            {
                nameof(TestEntity.Status),
                nameof(TestEntity.UpdateTime)
            };
            AssertFields(expectedFields, fields);
        }

        /// <summary>
        /// List
        /// </summary>
        [TestMethod]
        public void ValidateList2()
        {
            Expression<Func<TestEntity, object>> fieldExpression = entity => new List<string>
            {
                nameof(TestEntity.Status),
                nameof(TestEntity.UpdateTime)
            };
            var fields = fieldExpression.GetFieldNames();
            var expectedFields = new List<string>
            {
                nameof(TestEntity.Status),
                nameof(TestEntity.UpdateTime)
            };
            AssertFields(expectedFields, fields);
        }

        /// <summary>
        /// List
        /// </summary>
        [TestMethod]
        public void ValidateListFromMethod()
        {
            List<string> fieldList = GetFieldList();
            Expression<Func<TestEntity, object>> fieldExpression = entity => fieldList;
            var fields = fieldExpression.GetFieldNames();
            var expectedFields = new List<string>
            {
                nameof(TestEntity.Status),
                nameof(TestEntity.UpdateTime)
            };
            AssertFields(expectedFields, fields);
        }

        /// <summary>
        /// List
        /// </summary>
        [TestMethod]
        public void ValidateListFromMethod2()
        {
            Expression<Func<TestEntity, object>> fieldExpression = entity => GetFieldList();
            var fields = fieldExpression.GetFieldNames();
            var expectedFields = new List<string>
            {
                nameof(TestEntity.Status),
                nameof(TestEntity.UpdateTime)
            };
            AssertFields(expectedFields, fields);
        }

        /// <summary>
        /// IList
        /// </summary>
        [TestMethod]
        public void ValidateIList()
        {
            IList<string> fieldList = new List<string>
            {
                nameof(TestEntity.Status),
                nameof(TestEntity.UpdateTime)
            };
            Expression<Func<TestEntity, object>> fieldExpression = entity => fieldList;
            var fields = fieldExpression.GetFieldNames();
            var expectedFields = new List<string>
            {
                nameof(TestEntity.Status),
                nameof(TestEntity.UpdateTime)
            };
            AssertFields(expectedFields, fields);
        }

        /// <summary>
        /// IEnumerable
        /// </summary>
        [TestMethod]
        public void ValidateIEnumerable()
        {
            IEnumerable<string> fieldList = new List<string>
            {
                nameof(TestEntity.Status),
                nameof(TestEntity.UpdateTime)
            };
            Expression<Func<TestEntity, object>> fieldExpression = entity => fieldList;
            var fields = fieldExpression.GetFieldNames();
            var expectedFields = new List<string>
            {
                nameof(TestEntity.Status),
                nameof(TestEntity.UpdateTime)
            };
            AssertFields(expectedFields, fields);
        }

        /// <summary>
        /// Array
        /// </summary>
        [TestMethod]
        public void ValidateArray()
        {
            string[] fieldList = new List<string>
            {
                nameof(TestEntity.Status),
                nameof(TestEntity.UpdateTime)
            }.ToArray();
            Expression<Func<TestEntity, object>> fieldExpression = entity => fieldList;
            var fields = fieldExpression.GetFieldNames();
            var expectedFields = new List<string>
            {
                nameof(TestEntity.Status),
                nameof(TestEntity.UpdateTime)
            };
            AssertFields(expectedFields, fields);
        }

        /// <summary>
        /// Array
        /// </summary>
        [TestMethod]
        public void ValidateArray2()
        {
            Expression<Func<TestEntity, object>> fieldExpression = entity => new string[]
            {
                nameof(TestEntity.Status),
                nameof(TestEntity.UpdateTime)
            };
            var fields = fieldExpression.GetFieldNames();
            var expectedFields = new List<string>
            {
                nameof(TestEntity.Status),
                nameof(TestEntity.UpdateTime)
            };
            AssertFields(expectedFields, fields);
        }
        #endregion

        [TestMethod]
        public void ValidateDynamicAddFields()
        {
            Expression<Func<TestEntity, object>> fieldExpression = entity => new { entity.Status, entity.UpdateTime };
            var isFieldExists = fieldExpression.IsFieldExists(entity => entity.Age);
            Assert.AreEqual(false, isFieldExists);
            fieldExpression = fieldExpression.AddFieldNames(entity => entity.Age);
            var isFieldExists2 = fieldExpression.IsFieldExists(entity => entity.Age);
            Assert.AreEqual(true, isFieldExists2);
            var fields = fieldExpression.GetFieldNames();
            var expectedFields = new List<string>
            {
                nameof(TestEntity.Status),
                nameof(TestEntity.UpdateTime),
                nameof(TestEntity.Age)
            };
            AssertFields(expectedFields, fields);
        }

        private void AssertFields(List<string> expectedFields, List<string> actualFields)
        {
            Assert.AreEqual(expectedFields.Count, actualFields.Count);
            foreach (var field in expectedFields)
            {
                Assert.IsTrue(actualFields.Contains(field), $"The {nameof(actualFields)} does not contain <{field}>.");
            }
        }

        private List<string> GetFieldList()
        {
            return new List<string>
            {
                nameof(TestEntity.Status),
                nameof(TestEntity.UpdateTime)
            };
        }
    }
}
