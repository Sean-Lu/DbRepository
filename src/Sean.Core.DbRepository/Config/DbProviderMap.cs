using System.Data.Common;

namespace Sean.Core.DbRepository;

public class DbProviderMap
{
    public DbProviderMap(string providerInvariantName, string factoryTypeAssemblyQualifiedName)
    {
        ProviderInvariantName = providerInvariantName;
        FactoryTypeAssemblyQualifiedName = factoryTypeAssemblyQualifiedName;
    }

    public DbProviderMap(string providerInvariantName, DbProviderFactory providerFactory)
    {
        ProviderInvariantName = providerInvariantName;
        ProviderFactory = providerFactory;
    }

    public string ProviderInvariantName { get; }
    public string FactoryTypeAssemblyQualifiedName { get; }
    public DbProviderFactory ProviderFactory { get; internal set; }
}