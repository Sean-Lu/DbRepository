using Microsoft.Extensions.Configuration;
using Sean.Core.DbRepository;
using Sean.Core.DbRepository.Impls;
using Sean.Utility.Contracts;

namespace Example.NetCore.Impls.DbTest
{
    public class FirebirdTest : BaseRepository, ISimpleDo
    {
        public FirebirdTest(IConfiguration configuration) : base(configuration, "test_Firebird")
        {
        }

        public void Execute()
        {

        }
    }
}
