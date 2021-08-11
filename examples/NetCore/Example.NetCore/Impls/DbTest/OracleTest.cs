using System;
using Dapper;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using Sean.Core.DbRepository;
using Sean.Core.DbRepository.Impls;
using Sean.Utility.Contracts;
using Sean.Utility.Extensions;
using Sean.Utility.Format;

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
            if (result.Kind == DateTimeKind.Unspecified)
            {
                result = result.SetDateTimeKind(DateTimeKind.Utc).ToLocalTime();
            }
            Console.WriteLine(result.ToLongDateTimeWithTimezone());
        }
    }
}
