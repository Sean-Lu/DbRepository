using Microsoft.Extensions.Configuration;
using Sean.Core.DbRepository;
using Sean.Core.DbRepository.Impls;
using Sean.Utility.Contracts;

namespace Example.NetCore.Impls.DbTest
{
    public class SqlServerCeTest : BaseRepository, ISimpleDo
    {
        public SqlServerCeTest(IConfiguration configuration) : base(configuration, "test_SqlServerCe")
        {
        }

        public void Execute()
        {

        }
    }
}
