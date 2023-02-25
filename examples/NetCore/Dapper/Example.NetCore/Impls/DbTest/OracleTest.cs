using System;
//using Dapper;
using Microsoft.Extensions.Configuration;
using Sean.Core.DbRepository;
using Sean.Utility.Contracts;
using Sean.Utility.Extensions;

namespace Example.NetCore.Impls.DbTest
{
    /// <summary>
    /// Oracle
    /// </summary>
    public class OracleTest : BaseRepository, ISimpleDo
    {
        private readonly ILogger _logger;

        public OracleTest(IConfiguration configuration, ISimpleLogger<OracleTest> logger) : base(configuration, "test_Oracle")
        {
            _logger = logger;
        }

        public void Execute()
        {
            var sql = "SELECT SYSDATE FROM DUAL";
            //DateTime result = Execute(c => c.QueryFirstOrDefault<DateTime>(sql));
            DateTime result = Get<DateTime>(sql);
            Console.WriteLine(result.SetDateTimeKindIfUnspecified().ToLongDateTimeWithTimezone());
        }
    }
}
