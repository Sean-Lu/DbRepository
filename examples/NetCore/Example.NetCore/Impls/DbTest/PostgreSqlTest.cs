using Microsoft.Extensions.Configuration;
using Sean.Core.DbRepository;
using Sean.Core.DbRepository.Impls;
using Sean.Utility.Contracts;

namespace Example.NetCore.Impls.DbTest
{
    public class PostgreSqlTest : BaseRepository, ISimpleDo
    {
        public PostgreSqlTest(IConfiguration configuration) : base(configuration, "test_PostgreSql")
        {
        }

        public void Execute()
        {

        }
    }
}
