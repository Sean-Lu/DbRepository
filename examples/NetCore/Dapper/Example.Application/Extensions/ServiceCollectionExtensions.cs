using System.Reflection;
using Example.Domain.Extensions;
using Example.Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Example.Application.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 应用层依赖注入
        /// </summary>
        /// <param name="services"></param>
        public static void AddApplicationDI(this IServiceCollection services)
        {
            services.AddDomainDI();

            services.AddAutoMapper(expression =>
            {
                expression.AllowNullCollections = true;
                expression.AllowNullDestinationValues = true;
                expression.Advanced.AllowAdditiveTypeMapCreation = true;
                expression.AddMaps(typeof(AutoMapperProfile));
            });

            services.RegisterServicesByAssemblyInterface(Assembly.GetExecutingAssembly(), "Service", ServiceLifetime.Transient);
        }
    }
}
