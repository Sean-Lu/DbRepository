using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Example.NetFramework.Entities;
using MySql.Data.MySqlClient;
using Sean.Core.DbRepository;
using Sean.Core.DbRepository.Dapper.Impls;
using Sean.Core.DbRepository.Extensions;
using Sean.Core.DbRepository.Factory;
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

        public override void OutputExecutedSql(string sql, object param)
        {
            _logger.LogInfo($"执行了SQL: {sql}");
        }

        public void Execute()
        {
            //var time = Factory.ExecuteScalar<DateTime>("select now()");
            //_logger.LogInfo($"数据库当前时间：{time.ToLongDateTimeWithTimezone()}");

            #region 查询数据
            var list = QueryPage(NewSqlFactory(true)
                .Page(5, 3)
                .Where($"{nameof(CheckInLogEntity.UserId)}=@{nameof(CheckInLogEntity.UserId)}")
                .SetParameter(new { UserId = 100000 }));
            _logger.LogInfo($"从数据库中查询到数据：{Environment.NewLine}{JsonHelper.SerializeFormatIndented(list)}");
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
            //_logger.LogInfo($"新增单条数据结果：{addResult}{Environment.NewLine}{JsonHelper.SerializeFormatIndented(entity)}");
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