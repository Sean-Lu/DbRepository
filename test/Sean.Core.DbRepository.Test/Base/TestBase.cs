using Example.Application.Extensions;
using Sean.Core.Ioc;
using Sean.Utility.Extensions;
using Sean.Utility.Impls.Log;

namespace Sean.Core.DbRepository.Test
{
    public abstract class TestBase
    {
        static TestBase()
        {
            IocContainer.Instance.ConfigureServices(services =>
            {
                services.AddApplicationDI();
            });

            #region 配置Logger
            SimpleLocalLoggerBase.DateTimeFormat = time => time.ToLongDateTime();
            #endregion
        }
    }
}
