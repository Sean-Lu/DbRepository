using System.Reflection;
using Example.Infrastructure.Extensions;
using Example.Infrastructure.Impls;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using Sean.Core.DbRepository;
using Sean.Core.DbRepository.Config;
using Sean.Core.DbRepository.Extensions;
using Sean.Core.DbRepository.Factory;

namespace Example.Domain.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 领域层依赖注入
        /// </summary>
        /// <param name="services"></param>
        public static void AddDomainDI(this IServiceCollection services)
        {
            services.RegisterServicesByAssemblyInterface(Assembly.GetExecutingAssembly(), "Repository", ServiceLifetime.Transient);
            DbFactory.Serializer = NewJsonSerializer.Instance;
            DatabaseType.MySql.SetDbProviderMap(new DbProviderMap("MySql.Data.MySqlClient", MySqlClientFactory.Instance));
        }
    }
}
