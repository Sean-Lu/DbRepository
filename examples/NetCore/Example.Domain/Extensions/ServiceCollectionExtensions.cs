using System.Data.SqlClient;
using System.Data.SQLite;
using System.Reflection;
using Example.Infrastructure.Extensions;
using Example.Infrastructure.Impls;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using Oracle.ManagedDataAccess.Client;
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

            DatabaseType.MySql.SetDbProviderMap(new DbProviderMap("MySql.Data.MySqlClient", MySqlClientFactory.Instance));
            DatabaseType.SqlServer.SetDbProviderMap(new DbProviderMap("System.Data.SqlClient", SqlClientFactory.Instance));
            DatabaseType.Oracle.SetDbProviderMap(new DbProviderMap("Oracle.ManagedDataAccess.Client", OracleClientFactory.Instance));
            DatabaseType.SQLite.SetDbProviderMap(new DbProviderMap("System.Data.SQLite", SQLiteFactory.Instance));
        }
    }
}
