using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Example.Domain.Contracts;
using Example.Domain.Entities;
using Example.Domain.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sean.Core.DbRepository.Extensions;
using Sean.Core.Ioc;
using Sean.Utility.Contracts;

namespace Sean.Core.DbRepository.Test
{
    [TestClass]
    public class TableRepositoryTest : TestBase
    {
        private readonly ILogger _logger;
        private readonly ITestRepository _testRepository;

        public TableRepositoryTest()
        {
            _logger = IocContainer.Instance.GetService<ISimpleLogger<TableRepositoryTest>>();
            _testRepository = IocContainer.Instance.GetService<ITestRepository>();
        }

        /// <summary>
        /// CRUD
        /// </summary>
        [TestMethod]
        public void TestMethod()
        {
            var userId = 10001L;
            var testEntity = new TestEntity
            {
                UserId = userId,
                UserName = "测试",
                Sex = SexType.Male,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            };

            _logger.LogInfo("新增数据（***添加测试数据，用于后面的测试***）");
            var addResult = _testRepository.Add(testEntity, true);
            Assert.IsTrue(addResult);
            Assert.IsTrue(testEntity.Id > 0);
            _logger.LogInfo($"新增数据返回自增id：{testEntity.Id}");

            _logger.LogInfo("统计数据");
            var countResult = _testRepository.Count(entity => entity.UserId == testEntity.UserId);
            Assert.IsTrue(countResult > 0);

            _logger.LogInfo("更新数据（更新全部字段，where过滤条件默认为主键字段）");
            testEntity.AccountBalance = 6.66M;
            var updateResult = _testRepository.Update(testEntity);
            Assert.IsTrue(updateResult > 0);

            _logger.LogInfo("更新数据（更新全部字段，自定义where过滤条件）");
            testEntity.AccountBalance = 8.88M;
            updateResult = _testRepository.Update(testEntity, whereExpression: entity => entity.Id == testEntity.Id);
            Assert.IsTrue(updateResult > 0);

            _logger.LogInfo("更新数据（更新部分字段，where过滤条件默认为主键字段）");
            testEntity.IsVip = true;
            testEntity.Status = 1;
            updateResult = _testRepository.Update(testEntity, entity => new { entity.IsVip, entity.Status });
            Assert.IsTrue(updateResult > 0);

            _logger.LogInfo("查询数据（单个）");
            var getResult = _testRepository.Get(entity => entity.Id == testEntity.Id);
            Assert.IsTrue(getResult is { IsVip: true, Status: 1 });

            _logger.LogInfo("更新数据（更新部分字段，自定义where过滤条件）");
            testEntity.AccountBalance = 10000M;
            testEntity.Status = 2;
            updateResult = _testRepository.Update(testEntity
                , entity => new { entity.AccountBalance, entity.Status }
                , entity => entity.Id == testEntity.Id
                            && entity.Status == 1
                            && entity.AccountBalance < 100
                            && entity.Sex == SexType.Male
                            && !entity.IsBlack);
            Assert.IsTrue(updateResult > 0);

            _logger.LogInfo("查询数据（单个，只返回指定的部分字段）");
            var getResult2 = _testRepository.Get(entity => entity.Id == testEntity.Id, entity => new { entity.Id, entity.AccountBalance, entity.Status });
            Assert.IsTrue(getResult2 is { AccountBalance: 10000M, Status: 2 });

            _logger.LogInfo("查询数据（分页，只返回指定的部分字段）");
            var orderByCondition = OrderByConditionBuilder<TestEntity>.Build(OrderByType.Asc, entity => entity.UserId);
            orderByCondition.Next = OrderByConditionBuilder<TestEntity>.Build(OrderByType.Desc, entity => entity.CreateTime);
            var queryResult = _testRepository.Query(entity => entity.UserId == testEntity.UserId, entity => new { entity.Id, entity.AccountBalance }, orderByCondition, 1, 10)?.ToList();
            Assert.IsTrue(queryResult != null && queryResult.Any() && queryResult.Count(c => c.Id == testEntity.Id) > 0);

            _logger.LogInfo("查询数据（返回排序后的全表数据，查全表仅用于数据量较少的情况，否则建议使用分页查询）");
            var queryAllResult = _testRepository.Query(entity => true, null, orderByCondition)?.ToList();
            Assert.IsTrue(queryAllResult != null && queryAllResult.Any() && queryAllResult.Count(c => c.Id == testEntity.Id) > 0);

            _logger.LogInfo("删除数据（***删除上面新增的测试数据***）");
            var deleteResult = _testRepository.Delete(entity => entity.UserId == testEntity.UserId && entity.Status != 0);
            Assert.IsTrue(deleteResult > 0);

            _logger.LogInfo("统计数据（***验证测试数据已经删除***）");
            var countResult2 = _testRepository.Count(entity => entity.UserId == testEntity.UserId);
            Assert.IsTrue(countResult2 == 0);
        }
    }
}
