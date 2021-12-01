using System;
using System.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Sean.Core.DbRepository;
using Sean.Core.DbRepository.Impls;
using Sean.Utility.Contracts;
using Sean.Utility.Format;

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
