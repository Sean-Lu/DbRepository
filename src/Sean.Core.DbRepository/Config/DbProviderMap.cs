using System.Data.Common;

namespace Sean.Core.DbRepository.Config
{
    public class DbProviderMap
    {
        public DbProviderMap(DatabaseType type, string providerInvariantName, string factoryTypeAssemblyQualifiedName)
        {
            Type = type;
            ProviderInvariantName = providerInvariantName;
            FactoryTypeAssemblyQualifiedName = factoryTypeAssemblyQualifiedName;
        }

        public DbProviderMap(DatabaseType type, DbProviderFactory providerFactory)
        {
            Type = type;
            ProviderFactory = providerFactory;
        }

        public DatabaseType Type { get; set; }
        public string ProviderInvariantName { get; set; }
        public string FactoryTypeAssemblyQualifiedName { get; set; }
        public DbProviderFactory ProviderFactory { get; set; }
    }
}