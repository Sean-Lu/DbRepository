using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Example.Application.Extensions;
using Example.Infrastructure.Impls;
using Microsoft.Extensions.DependencyInjection;
using Sean.Core.Ioc;
using Sean.Utility.Contracts;
using Sean.Utility.Extensions;
using Sean.Utility.Format;
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

                services.AddSimpleLocalLogger();

                services.AddTransient<IJsonSerializer, NewJsonSerializer>();
                JsonHelper.Serializer = NewJsonSerializer.Instance;
            });

            #region 配置Logger
            SimpleLocalLoggerBase.DateTimeFormat = time => time.ToLongDateTime();
            #endregion
        }
    }
}
