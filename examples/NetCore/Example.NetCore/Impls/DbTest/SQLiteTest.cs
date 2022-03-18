using System;
using Dapper;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Sean.Core.DbRepository;
using Sean.Utility.Contracts;

namespace Example.NetCore.Impls.DbTest
{
    /// <summary>
    /// SQLite
    /// </summary>
    public class SQLiteTest : BaseRepository, ISimpleDo
    {
        public SQLiteTest(IConfiguration configuration) : base(configuration, "test_SQLite")
        {
        }

        public void Execute()
        {
            var sql = "SELECT * FROM Test limit 5";
            var result = Execute(c => c.Query<dynamic>(sql, new { }));
            Console.WriteLine(JsonConvert.SerializeObject(result));
        }
    }
}
