using System;
using Dapper;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Sean.Core.DbRepository;
using Sean.Utility.Contracts;

namespace Example.NetCore.Impls.DbTest
{
    /// <summary>
    /// SQL Server
    /// </summary>
    public class SqlServerTest : BaseRepository, ISimpleDo
    {
        public SqlServerTest(IConfiguration configuration) : base(configuration, "test_SqlServer")
        {
        }

        public void Execute()
        {
            var sql = "SELECT top 2 * FROM Test";
            var result = Execute(c => c.Query<dynamic>(sql, new { }));
            Console.WriteLine(JsonConvert.SerializeObject(result));
        }
    }
}
