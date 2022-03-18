using System;
using Dapper;
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
            var result = Execute(c => c.QuerySingle<DateTime>(sql, new { }));
            Console.WriteLine(result.SetDateTimeKindIfUnspecified().ToLongDateTimeWithTimezone());
        }
    }
}
