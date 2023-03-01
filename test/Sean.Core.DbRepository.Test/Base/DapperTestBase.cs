using Example.Dapper.Core.Application.Extensions;
using Example.Dapper.Core.Infrastructure;

namespace Sean.Core.DbRepository.Test
{
    public abstract class DapperTestBase : TestBase
    {
        static DapperTestBase()
        {
            DIManager.ConfigureServices(services =>
            {
                services.AddApplicationDI();
            });
        }
    }
}
