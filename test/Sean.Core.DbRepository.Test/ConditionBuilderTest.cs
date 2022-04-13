using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Example.Domain.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sean.Core.DbRepository.Test
{
    [TestClass]
    public class ConditionBuilderTest
    {
        /// <summary>
        /// 普通多条件
        /// </summary>
        [TestMethod]
        public void Validate1()
        {
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.Age > 5
                                                                           && entity.UserId > 5
                                                                           && entity.UserName.StartsWith("1") //  like '1%'
                                                                           && entity.UserName.EndsWith("1") //  like '%1'
                                                                           && entity.UserName.Contains("1");//  like '%1%' 
            var conditionBuilderVisitor = new ConditionBuilderVisitor();
            conditionBuilderVisitor.Visit(whereExpression);
            var whereClause = conditionBuilderVisitor.GetCondition();
            var expectedWhereClause = "((((([Age] > 5) AND ([UserId] > 5)) AND ([UserName] LIKE '1%')) AND ([UserName] LIKE '%1')) AND ([UserName] LIKE '%1%'))";
            Assert.IsTrue(whereClause == expectedWhereClause);
        }

        /// <summary>
        /// 外部参数变量
        /// </summary>
        [TestMethod]
        public void Validate2()
        {
            string name = "AAA";
            Expression<Func<TestEntity, bool>> whereExpression = testEntity => testEntity.Age > 5 && testEntity.UserName == name
                                                                               || testEntity.UserId > 5;
            var conditionBuilderVisitor = new ConditionBuilderVisitor();
            conditionBuilderVisitor.Visit(whereExpression);
            var whereClause = conditionBuilderVisitor.GetCondition();
            var expectedWhereClause = "((([Age] > 5) AND ([UserName] = 'AAA')) OR ([UserId] > 5))";
            Assert.IsTrue(whereClause == expectedWhereClause);
        }

        /// <summary>
        /// 内部常量多条件
        /// </summary>
        [TestMethod]
        public void Validate3()
        {
            Expression<Func<TestEntity, bool>> whereExpression = entity => entity.Age > 5 || (entity.UserName == "A" && entity.UserId > 5);
            var conditionBuilderVisitor = new ConditionBuilderVisitor();
            conditionBuilderVisitor.Visit(whereExpression);
            var whereClause = conditionBuilderVisitor.GetCondition();
            var expectedWhereClause = "(([Age] > 5) OR (([UserName] = A) AND ([UserId] > 5)))";
            Assert.IsTrue(whereClause == expectedWhereClause);
        }
    }
}
