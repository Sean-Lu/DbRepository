using System.Reflection;
using Example.Domain.Extensions;
using Sean.Core.DependencyInjection;

namespace Example.Application.Extensions
{
    public static class DIExtensions
    {
        /// <summary>
        /// 应用层依赖注入
        /// </summary>
        /// <param name="container"></param>
        public static void AddApplicationDI(this IDIRegister container)
        {
            container.AddDomainDI();

            container.RegisterAssemblyByInterfaceSuffix(Assembly.GetExecutingAssembly(), "Service", ServiceLifeStyle.Transient);
        }
    }
}
