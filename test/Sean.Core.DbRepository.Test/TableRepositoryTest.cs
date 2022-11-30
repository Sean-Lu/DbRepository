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
    /// <summary>
    /// XXXRepository 测试
    /// </summary>
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
        public void ValidateCRUD()
        {
            var userId = 10001L;
            var testEntity = AddTestData(userId, true);

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
            var orderBy = OrderByConditionBuilder<TestEntity>.Build(OrderByType.Asc, entity => entity.UserId);
            orderBy.Next = OrderByConditionBuilder<TestEntity>.Build(OrderByType.Desc, entity => entity.CreateTime);
            var queryResult = _testRepository.Query(entity => entity.UserId == testEntity.UserId, orderBy, 1, 10, entity => new { entity.Id, entity.AccountBalance })?.ToList();
            Assert.IsTrue(queryResult != null && queryResult.Any() && queryResult.Count(c => c.Id == testEntity.Id) > 0);

            _logger.LogInfo("查询数据（返回排序后的全表数据，查全表仅用于数据量较少的情况，否则建议使用分页查询）");
            var queryAllResult = _testRepository.Query(entity => true, orderBy)?.ToList();
            Assert.IsTrue(queryAllResult != null && queryAllResult.Any() && queryAllResult.Count(c => c.Id == testEntity.Id) > 0);

            DeleteTestData(testEntity.UserId);

            _logger.LogInfo("统计数据（***验证测试数据是否已经删除***）");
            var countResult2 = _testRepository.Count(entity => entity.UserId == userId);
            Assert.IsTrue(countResult2 == 0);
        }

        /// <summary>
        /// <see cref="IBaseRepository{TEntity}.Increment{TValue}"/>
        /// </summary>
        [TestMethod]
        public void ValidateIncrAndDecr()
        {
            var userId = 10002L;
            var testEntity = AddTestData(userId, true);

            _logger.LogInfo("Increment - 数值递增");
            var incrResult = _testRepository.Increment(10.0M, entity => entity.AccountBalance, entity => entity.Id == testEntity.Id);
            Assert.IsTrue(incrResult);

            _logger.LogInfo("Increment - 验证");
            var getResult = _testRepository.Get(entity => entity.Id == testEntity.Id, entity => new { entity.Id, entity.AccountBalance });
            Assert.IsTrue(getResult != null && getResult.Id > 0 && getResult.AccountBalance == 10.0M);

            _logger.LogInfo("Decrement - 数值递减");
            var incrResult2 = _testRepository.Decrement(2.0M, entity => entity.AccountBalance, entity => entity.Id == testEntity.Id);
            Assert.IsTrue(incrResult2);

            _logger.LogInfo("Decrement - 验证");
            var getResult2 = _testRepository.Get(entity => entity.Id == testEntity.Id, entity => new { entity.Id, entity.AccountBalance });
            Assert.IsTrue(getResult2 != null && getResult2.Id > 0 && getResult2.AccountBalance == 8.0M);

            DeleteTestData(userId);
        }

        [TestMethod]
        public void ValidateAddOrUpdate()
        {
            var userId = 10003L;
            //var testEntity = AddTestData(userId, true);

            var testEntity = new TestEntity
            {
                Id = userId,
                UserId = userId,
                UserName = "Test",
                Country = CountryType.China,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            };
            var addOrUpdateResult = _testRepository.AddOrUpdate(testEntity);// 第1次：记录不存在，新增数据
            Assert.IsTrue(addOrUpdateResult);

            var getResult = _testRepository.Get(entity => entity.Id == userId);// 验证
            Assert.IsTrue(getResult != null && getResult.Id > 0 && getResult.UserId == userId && getResult.UserName == "Test" && getResult.Country == CountryType.China);

            testEntity.Country = CountryType.England;
            var addOrUpdateResult2 = _testRepository.AddOrUpdate(testEntity);// 第2次：记录已存在，更新数据
            Assert.IsTrue(addOrUpdateResult2);

            var getResult2 = _testRepository.Get(entity => entity.Id == userId);// 验证
            Assert.IsTrue(getResult2 != null && getResult2.Id > 0 && getResult2.UserId == userId && getResult2.UserName == "Test" && getResult2.Country == CountryType.England);

            DeleteTestData(userId);
        }

        /// <summary>
        /// 添加测试数据
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="returnId"></param>
        /// <returns></returns>
        private TestEntity AddTestData(long userId, bool returnId = true)
        {
            var testEntity = new TestEntity
            {
                UserId = userId,
                UserName = "测试",
                Sex = SexType.Male,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            };

            _logger.LogInfo("新增数据（***添加测试数据***）");
            var addResult = _testRepository.Add(testEntity, true);
            Assert.IsTrue(addResult);
            if (returnId)
            {
                Assert.IsTrue(testEntity.Id > 0);
                _logger.LogInfo($"新增数据返回自增id：{testEntity.Id}");
            }
            return testEntity;
        }

        /// <summary>
        /// 删除测试数据
        /// </summary>
        /// <param name="userId"></param>
        private void DeleteTestData(long userId)
        {
            _logger.LogInfo("删除数据（***删除测试数据***）");
            var deleteResult = _testRepository.Delete(entity => entity.UserId == userId /*&& entity.Status != 0*/);
            Assert.IsTrue(deleteResult > 0);
        }
    }
}
