using System;
using Sean.Core.DependencyInjection;

namespace Example.Infrastructure
{
    /// <summary>
    /// 依赖注入容器管理
    /// </summary>
    public static class DIManager
    {
        public static IDIResolve Container => _container;

        private static IDIContainer _container;

        public static void Register(Action<IDIRegister> action)
        {
            if (_container == null)
            {
                var builder = new ContainerBuilder();

                var container = builder.Build();

                _container = container;
            }

            action?.Invoke(_container);
        }
    }
}
