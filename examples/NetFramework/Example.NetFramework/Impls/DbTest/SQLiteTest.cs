using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Globalization;
using System.Reflection;
using Dapper;
using Example.NetFramework.Entities;
using Newtonsoft.Json;
using Sean.Core.DbRepository;
using Sean.Core.DbRepository.Impls;
using Sean.Utility.Contracts;
using Sean.Utility.Impls.Log;

namespace Example.NetFramework.Impls.DbTest
{
    /// <summary>
    /// SQLite
    /// </summary>
    public class SQLiteTest : BaseRepository, ISimpleDo
    {
        private readonly ILogger _logger;

        public SQLiteTest() : base("test_SQLite")
        //public SQLiteTest() : base(new MultiConnectionSettings(new List<ConnectionStringOptions> { new ConnectionStringOptions(@"data source=D:\test.db;version=3", DatabaseType.SQLite) }))
        {
            _logger = new SimpleLocalLogger<SQLiteTest>();
        }

        public void Execute()
        {
            #region 利用反射机制修改 DateTime.ToString() 的默认格式
            Console.WriteLine($"修改默认时间格式之前：{DateTime.Now}");
            if (DateTimeFormatInfo.CurrentInfo != null)
            {
                var type = DateTimeFormatInfo.CurrentInfo.GetType();
                var field = type.GetField("generalLongTimePattern", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(DateTimeFormatInfo.CurrentInfo, "yyyy-MM-dd HH:mm:ss");
                }
            }
            Console.WriteLine($"修改默认时间格式之后：{DateTime.Now}");
            #endregion

            #region 新增数据
            var sqlFactory = NewSqlFactory<TestEntity>(true);
            //var insertResult = Factory.ExecuteNonQuery(sqlFactory.InsertSql, new DbParameter[] { new SQLiteParameter(nameof(TestEntity.CreateTime), DateTime.Now) });
            var insertResult2 = Execute(c => c.Execute(sqlFactory.InsertSql, new TestEntity { CreateTime = DateTime.Now }));
            #endregion

            #region 查询数据
            var sqlFactory2 = NewSqlFactory<TestEntity>(true).Page(1, 2);
            //var queryResult = Factory.GetList<TestEntity>(sqlFactory2.QuerySql);
            var queryResult2 = Execute(c => c.Query<TestEntity>(sqlFactory2.QuerySql, new { }));
            _logger.LogInfo(JsonConvert.SerializeObject(queryResult2, Formatting.Indented));
            #endregion
        }
    }
}
