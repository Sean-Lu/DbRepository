using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Example.NetFramework.Entities;
using MySql.Data.MySqlClient;
using Sean.Core.DbRepository;
using Sean.Core.DbRepository.Dapper.Impls;
using Sean.Core.DbRepository.Extensions;
using Sean.Core.DbRepository.Impls;
using Sean.Utility.Contracts;
using Sean.Utility.Extensions;
using Sean.Utility.Format;
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
        {
            _logger = new SimpleLocalLogger<MySqlTest>();
        }

        public override void OutputExecutedSql(string sql)
        {
            _logger.LogInfo($"执行了SQL: {sql}");
        }

        public void Execute()
        {
            //var time = Factory.ExecuteScalar<DateTime>("select now()");
            //_logger.LogInfo($"数据库当前时间：{time.ToLongDateTimeWithTimezone()}");

            //var list = Search(100000, 5, 3);
            //_logger.LogInfo($"从数据库中查询到数据：{Environment.NewLine}{JsonHelper.SerializeFormatIndented(list)}");

            #region 新增单条数据
            var entity = new CheckInLogEntity
            {
                UserId = 100000,
                CheckInType = 1,
                CreateTime = DateTime.Now,
                IP = "127.0.0.1"
            };
            var addResult = Add(entity, true);
            _logger.LogInfo($"新增单条数据结果：{addResult}{Environment.NewLine}{JsonHelper.SerializeFormatIndented(entity)}");
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
            //var addBatchResult = Add(list, true);
            //_logger.LogInfo($"新增批量数据结果：{addBatchResult}{Environment.NewLine}{JsonHelper.SerializeFormatIndented(list)}");
            #endregion

            //var isTableExists = IsTableExists($"{MainTableName}");
            //_logger.LogInfo($"表是否存在：{isTableExists}");
        }

        public IEnumerable<CheckInLogEntity> Search(long userId, int pageIndex, int pageSize)
        {
            var sql = $"SELECT * FROM `{MainTableName}` WHERE {nameof(CheckInLogEntity.UserId)}=@{nameof(CheckInLogEntity.UserId)} LIMIT {(pageIndex - 1) * pageSize},{pageSize}";
            return Execute(c => c.Query<CheckInLogEntity>(sql, new { UserId = userId }));
        }

        /// <summary>
        /// UNION ALL：查询所有分表数据
        /// </summary>
        private void UnionAllTest()
        {
            var sqlList = new List<string>();
            for (var i = 0; i < 256; i++)
            {
                var tableName = $"OrderExtUser_{Convert.ToString(i, 16).PadLeft(2, '0').ToLower()}";
                sqlList.Add($"SELECT * FROM {tableName}");
            }
            var sql = string.Join(" UNION ALL ", sqlList);
            var result = Execute(c => c.Query<dynamic>(sql));
        }
    }
}