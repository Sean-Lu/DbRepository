using System;
using System.Collections.Generic;
using Dapper;
using Newtonsoft.Json;
using Sean.Core.DbRepository;
using Sean.Core.DbRepository.Impls;
using Sean.Utility.Contracts;
using Sean.Utility.Format;
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
            var sql = "select * from sqlite_master where type='table' order by name limit 2";
            var result = Factory.GetList<dynamic>(sql);
            //var result = Execute(c => c.Query<dynamic>(sql, new { }));
            _logger.LogInfo(JsonConvert.SerializeObject(result, Formatting.Indented));
        }
    }
}
