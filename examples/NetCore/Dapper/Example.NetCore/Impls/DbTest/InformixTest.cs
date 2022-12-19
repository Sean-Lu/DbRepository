using Microsoft.Extensions.Configuration;
using Sean.Core.DbRepository;
using Sean.Utility.Contracts;

namespace Example.NetCore.Impls.DbTest
{
    public class InformixTest : BaseRepository, ISimpleDo
    {
        public InformixTest(IConfiguration configuration) : base(configuration, "test_Informix")
        {
        }

        public void Execute()
        {

        }
    }
}
