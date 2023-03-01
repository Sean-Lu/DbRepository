using System;
using Microsoft.Extensions.DependencyInjection;
using Sean.Core.Ioc;

namespace Example.ADO.NETCore.Infrastructure
{
    public static class DIManager
    {
        public static void ConfigureServices(Action<IServiceCollection> action)
        {
            IocContainer.Instance.ConfigureServices(action);
        }

        public static TService GetService<TService>()
        {
            return IocContainer.Instance.GetService<TService>();
        }
    }
}
