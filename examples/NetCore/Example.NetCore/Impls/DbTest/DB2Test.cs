using Microsoft.Extensions.Configuration;
using Sean.Core.DbRepository;
using Sean.Utility.Contracts;

namespace Example.NetCore.Impls.DbTest
{
    public class DB2Test : BaseRepository, ISimpleDo
    {
        public DB2Test(IConfiguration configuration) : base(configuration, "test_DB2")
        {
        }

        public void Execute()
        {

        }
    }
}
