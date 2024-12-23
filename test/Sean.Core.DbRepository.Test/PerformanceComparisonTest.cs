﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Dapper;
using Example.Dapper.Core.Domain.Contracts;
using Example.Dapper.Core.Domain.Entities;
using Example.Dapper.Core.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sean.Core.DbRepository.Extensions;
using Sean.Utility.Contracts;

namespace Sean.Core.DbRepository.Test
{
    /// <summary>
    /// 性能对比测试
    /// </summary>
    [TestClass]
    public class PerformanceComparisonTest : DapperTestBase
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly ITestRepository _testRepository;
        private readonly bool _enablePerformanceComparisonTest;

        public PerformanceComparisonTest()
        {
            _logger = DIManager.GetService<ISimpleLogger<PerformanceComparisonTest>>();
            _configuration = DIManager.GetService<IConfiguration>();
            _testRepository = DIManager.GetService<ITestRepository>();
            _enablePerformanceComparisonTest = _configuration.GetValue<bool>("UnitTestOptions:EnablePerformanceComparisonTest");
        }

        /// <summary>
        /// 批量新增耗时时间对比
        /// </summary>
        [TestMethod]
        public void CompareBulkInsertTimeConsumed()
        {
            if (!_enablePerformanceComparisonTest)
            {
                return;
            }

            CompareBulkInsertTimeConsumed(50);// 批量新增 50 条数据
            CompareBulkInsertTimeConsumed(200);// 批量新增 200 条数据
            CompareBulkInsertTimeConsumed(1000);// 批量新增 1000 条数据
            CompareBulkInsertTimeConsumed(2000);// 批量新增 2000 条数据
            CompareBulkInsertTimeConsumed(5000);// 批量新增 5000 条数据
        }

        private void CompareBulkInsertTimeConsumed(int insertEntityCount)
        {
            #region 数据准备
            var list = new List<TestEntity>();
            for (int i = 0; i < insertEntityCount; i++)
            {
                list.Add(new TestEntity
                {
                    UserId = 100000 + i + 1,
                    UserName = $"u{100000 + i + 1}",
                    Age = i + 1,
                    Sex = SexType.Female,
                    IsVip = true,
                    Country = CountryType.China,
                    AccountBalance = 1000,
                    Status = 1,
                    CreateTime = DateTime.Now,
                    UpdateTime = DateTime.Now
                });
            }
            #endregion

            #region [Dapper.Execute]批量新增数据
            _testRepository.Delete(entity => true);// 删除所有数据
            _testRepository.Execute(conn =>
            {
                var stopwatch = new Stopwatch();
                stopwatch.Restart();
                var insertableSql = _testRepository.CreateInsertableBuilder()
                    .SetParameter(list.First())
                    .Build();
                stopwatch.Stop();
                var buildSqlElapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                stopwatch.Restart();
                var result = conn.Execute(insertableSql.Sql, list);// 批量新增数据
                stopwatch.Stop();
                Assert.IsTrue(result == list.Count);
                var executeElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                _logger.LogInfo($"[Dapper.Execute]批量新增数据成功 {result} 条，执行耗时 {executeElapsedMilliseconds} 毫秒，BuildSql 耗时 {buildSqlElapsedMilliseconds} 毫秒，总耗时 {buildSqlElapsedMilliseconds + executeElapsedMilliseconds } 毫秒！");

                return result > 0;
            });
            #endregion

            #region [BulkInsert]批量新增数据
            _testRepository.Delete(entity => true);// 删除所有数据
            _testRepository.Execute(conn =>
            {
                var stopwatch = new Stopwatch();
                stopwatch.Restart();
                var insertableSql = _testRepository.CreateInsertableBuilder()
                    .SetParameter(list)// BulkInsert
                    .Build();
                stopwatch.Stop();
                var buildSqlElapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                stopwatch.Restart();
                var result = conn.Execute(insertableSql.Sql, insertableSql.Parameter);// 批量新增数据
                stopwatch.Stop();
                Assert.IsTrue(result == list.Count);
                var executeElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                _logger.LogInfo($"[BulkInsert]批量新增数据成功 {result} 条，执行耗时 {executeElapsedMilliseconds} 毫秒，BuildSql 耗时 {buildSqlElapsedMilliseconds} 毫秒，总耗时 {buildSqlElapsedMilliseconds + executeElapsedMilliseconds} 毫秒！");

                return result > 0;
            });
            #endregion

            _testRepository.Delete(entity => true);// 删除所有数据
        }
    }
}
