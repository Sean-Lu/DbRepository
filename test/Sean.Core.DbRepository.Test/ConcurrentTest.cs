using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Sean.Core.DbRepository.Test;

/// <summary>
/// 并发测试
/// </summary>
[TestClass]
public class ConcurrentTest : DapperTestBase
{
    private readonly TestLogger _logger = TestInfrastructure.CreateLogger();
    private readonly TestRepository _testRepository = TestInfrastructure.CreateRepository();

    /// <summary>
    /// 并发写入数据测试
    /// </summary>
    [TestMethod]
    public void ConcurrentInsertDataTest()
    {
        var preCount = _testRepository.Count(entity => true);
        _logger.LogInfo($"###### 新增数据之前有 {preCount} 条数据");

        // 创建一个任务列表
        var testCount = 1000;
        var tasks = new Task[testCount];

        // 创建并启动1000个任务
        for (int i = 0; i < tasks.Length; i++)
        {
            var index = i + 1;
            tasks[i] = Task.Run(() =>
            {
                _logger.LogInfo($"[{index}] [{Task.CurrentId}] 异步任务开始");
                var testEntity = new TestEntity
                {
                    UserId = 10002,
                    UserName = "Test01",
                    Age = 18,
                    IsVip = true,
                    AccountBalance = 99.95M,
                    Remark = "并发写入数据测试",
                    CreateTime = DateTime.Now,
                    UpdateTime = DateTime.Now
                };
                var addResult = _testRepository.Add(testEntity);
                _logger.LogInfo($"[{index}] [{Task.CurrentId}] 异步任务结束");
                Assert.IsTrue(addResult, $"[{index}] [{Task.CurrentId}] 新增测试数据失败");
            });
        }

        var deleteResult = 0;
        try
        {
            // 任务异常必须传播给测试框架，不能只记录日志后吞掉。
            Task.WaitAll(tasks);

            var nowCount = _testRepository.Count(entity => true);
            _logger.LogInfo($"###### 新增数据之后有 {nowCount} 条数据");
            Assert.AreEqual(testCount, nowCount - preCount);
        }
        finally
        {
            deleteResult = _testRepository.Delete(entity => entity.Remark == "并发写入数据测试");
        }
        Assert.AreEqual(testCount, deleteResult);
    }

    /// <summary>
    /// 并发写入数据测试（使用事务）
    /// </summary>
    [TestMethod]
    public void ConcurrentInsertDataWithTransactionTest()
    {
        var preCount = _testRepository.Count(entity => true);
        _logger.LogInfo($"###### 新增数据之前有 {preCount} 条数据");

        // 创建一个任务列表
        var testCount = 1000;
        var tasks = new Task[testCount];

        // 创建并启动1000个任务
        for (int i = 0; i < tasks.Length; i++)
        {
            var index = i + 1;
            tasks[i] = Task.Run(() =>
            {
                _logger.LogInfo($"[{index}] [{Task.CurrentId}] 异步任务开始");
                var addResult = _testRepository.ExecuteAutoTransaction(trans =>
                {
                    var testEntity = new TestEntity
                    {
                        UserId = 10002,
                        UserName = "Test01",
                        Age = 18,
                        IsVip = true,
                        AccountBalance = 99.95M,
                        Remark = "并发写入数据测试（使用事务）",
                        CreateTime = DateTime.Now,
                        UpdateTime = DateTime.Now
                    };
                    if (!_testRepository.Add(testEntity, transaction: trans))// ****** 注意要传事务
                    {
                        return false;
                    }

                    return true;
                });
                _logger.LogInfo($"[{index}] [{Task.CurrentId}] 异步任务结束");
                Assert.IsTrue(addResult, $"[{index}] [{Task.CurrentId}] 新增测试数据失败");
            });
        }

        var deleteResult = 0;
        try
        {
            // 任务异常必须传播给测试框架，不能只记录日志后吞掉。
            Task.WaitAll(tasks);

            var nowCount = _testRepository.Count(entity => true);
            _logger.LogInfo($"###### 新增数据之后有 {nowCount} 条数据");
            Assert.AreEqual(testCount, nowCount - preCount);
        }
        finally
        {
            deleteResult = _testRepository.Delete(entity => entity.Remark == "并发写入数据测试（使用事务）");
        }
        Assert.AreEqual(testCount, deleteResult);
    }
}
