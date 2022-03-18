﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Example.Domain.Entities;
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

        /// <summary>
        /// 单个字段
        /// </summary>
        [TestMethod]
        public void ValidateSingleField()
        {
            Expression<Func<TestEntity, object>> fieldExpression = entity => entity.Status;
            var fields = fieldExpression.GetMemberNames();
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
            var fields = fieldExpression.GetMemberNames();
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
            var fields = fieldExpression.GetMemberNames();
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
            var fields = fieldExpression.GetMemberNames();
            var expectedFields = new List<string>
            {
                nameof(TestEntity.Status),
                nameof(TestEntity.UpdateTime)
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
    }
}
