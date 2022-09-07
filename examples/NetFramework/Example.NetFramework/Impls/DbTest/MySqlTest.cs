using System;
using System.Collections.Generic;
using Dapper;
using Example.NetFramework.Entities;
using Newtonsoft.Json;
using Sean.Core.DbRepository;
using Sean.Core.DbRepository.Dapper;
using Sean.Core.DbRepository.Extensions;
using Sean.Utility.Contracts;
using Sean.Utility.Impls.Log;

namespace Example.NetFramework.Impls.DbTest
{
    /// <summary>
    /// MySql
    /// </summary>
    public class MySqlTest : BaseRepository<CheckInLogEntity>, ISimpleDo
    {
        private readonly ILogger _logger;

        public MySqlTest() : base()
        //public MySqlTest() : base(new MultiConnectionSettings(new ConnectionStringOptions("DataSource=127.0.0.1;Database=test;uid=root;pwd=12345!a", DatabaseType.MySql)))
        {
            _logger = new SimpleLocalLogger<MySqlTest>();
        }

        public override void OnSqlExecuted(SqlExecutedContext context)
        {
            _logger.LogInfo($"执行了SQL: {context.Sql}{Environment.NewLine}参数：{JsonConvert.SerializeObject(context.SqlParameter, Formatting.Indented)}");
        }

        public void Execute()
        {
            //DapperQueryTest();
            //return;

            //var time = Factory.ExecuteScalar<DateTime>("select now()");
            //_logger.LogInfo($"数据库当前时间：{time.ToLongDateTimeWithTimezone()}");

            #region 查询数据
            var orderByCondition = OrderByConditionBuilder<CheckInLogEntity>.Build(OrderByType.Asc, entity => entity.UserId);
            orderByCondition.Next = OrderByConditionBuilder<CheckInLogEntity>.Build(OrderByType.Desc, entity => entity.CreateTime);
            var list = Query(entity => entity.UserId == 100010, orderByCondition, 1, 3, master: false);// 从库查询
            _logger.LogInfo($"从数据库中查询到数据：{Environment.NewLine}{JsonConvert.SerializeObject(list, Formatting.Indented)}");
            #endregion

            #region 新增单条数据
            //var entity = new CheckInLogEntity
            //{
            //    UserId = 100000,
            //    CheckInType = 1,
            //    CreateTime = DateTime.Now,
            //    IP = "127.0.0.1"
            //};
            //var addResult = Add(entity, true);
            //_logger.LogInfo($"新增单条数据结果：{addResult}{Environment.NewLine}{JsonConvert.SerializeObject(entity, Formatting.Indented)}");
            #endregion

            #region 新增批量数据
            //var list = new List<CheckInLogEntity>
            //{
            //    new CheckInLogEntity
            //    {
            //        UserId = 100000,
            //        CheckInType = 1,
            //        CreateTime = DateTime.Now,
            //        IP = "127.0.0.1"
            //    }
            //};
            //var addBatchResult = Add(list);
            //_logger.LogInfo($"新增批量数据结果：{addBatchResult}{Environment.NewLine}{JsonConvert.SerializeObject(list, Formatting.Indented)}");
            #endregion

            //var isTableExists = IsTableExists($"{MainTableName}");
            //_logger.LogInfo($"表是否存在：{isTableExists}");
        }

        private void DapperQueryTest()
        {
            var queryableSql = this.CreateQueryableBuilder(true)
                .Where(entity => entity.UserId == 100000)
                .Build();

            #region Dapper > QueryFirst\QueryFirstOrDefault
            // 没有结果返回时，QueryFirst 方法会报错（System.InvalidOperationException:“序列不包含任何元素”），QueryFirstOrDefault 方法会返回默认值
            // 有多个结果返回时，2个方法都会返回第一个结果
            //var get1 = Execute(c => c.QueryFirst<CheckInLogEntity>(queryableSql.Sql, queryableSql.Parameter));
            var get2 = Execute(c => c.QueryFirstOrDefault<CheckInLogEntity>(queryableSql.Sql, queryableSql.Parameter));
            #endregion

            #region Dapper > QuerySingle\QuerySingleOrDefault
            // 没有结果返回时，QuerySingle 方法会报错（System.InvalidOperationException:“序列不包含任何元素”），QuerySingleOrDefault 方法会返回默认值
            // 有多个结果返回时，2个方法都会报错（System.InvalidOperationException:“序列包含一个以上的元素”）
            //var get3 = Execute(c => c.QuerySingle<CheckInLogEntity>(queryableSql.Sql, queryableSql.Parameter));
            //var get4 = Execute(c => c.QuerySingleOrDefault<CheckInLogEntity>(queryableSql.Sql, queryableSql.Parameter));
            #endregion
        }

        /// <summary>
        /// UNION ALL：查询所有分表数据
        /// </summary>
        private void UnionAllTest()
        {
            var hexCount = 2;// 16进制位数
            var sqlList = new List<string>();
            for (var i = 0; i < Math.Pow(16, hexCount); i++)
            {
                var tableName = $"OrderExtUser_{Convert.ToString(i, 16).PadLeft(hexCount, '0').ToLower()}";
                sqlList.Add($"SELECT * FROM {tableName}");
            }
            var sql = string.Join(" UNION ALL ", sqlList);
            var result = Execute(c => c.Query<dynamic>(sql));
        }
    }
}