using System.Reflection;
using Example.Dapper.Domain.Extensions;
using Sean.Core.DependencyInjection;

namespace Example.Dapper.Application.Extensions
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
