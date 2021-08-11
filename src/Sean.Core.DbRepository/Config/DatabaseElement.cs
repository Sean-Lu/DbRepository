using System.Configuration;
using Sean.Core.DbRepository.Factory;

namespace Sean.Core.DbRepository.Config
{
    public class DatabaseElement : ConfigurationElement
    {
        private const string PropertyName = "name";
        private const string PropertyProviderInvariantName = "providerInvariantName";
        private const string PropertyFactoryTypeAssemblyQualifiedName = "factoryTypeAssemblyQualifiedName";

        public DatabaseElement()
        {
            Name = (DatabaseType)this[PropertyName];
            ProviderInvariantName = (string)this[PropertyProviderInvariantName];
            FactoryTypeAssemblyQualifiedName = (string)this[PropertyFactoryTypeAssemblyQualifiedName];
        }

        public DatabaseElement(DatabaseType name, string providerInvariantName, string factoryTypeAssemblyQualifiedName)
        {
            Name = name;
            ProviderInvariantName = providerInvariantName;
            FactoryTypeAssemblyQualifiedName = factoryTypeAssemblyQualifiedName;
        }

        /// <summary>
        /// The name of the database
        /// </summary>
        [ConfigurationProperty(PropertyName, IsRequired = true, IsKey = true)]
        public DatabaseType Name { get; set; }

        [ConfigurationProperty(PropertyProviderInvariantName, IsRequired = true)]
        public string ProviderInvariantName { get; set; }

        /// <summary>
        /// FactoryType, AssemblyQualifiedName
        /// </summary>
        [ConfigurationProperty(PropertyFactoryTypeAssemblyQualifiedName, IsRequired = false)]
        public string FactoryTypeAssemblyQualifiedName { get; set; }

        public DbProviderMap Map()
        {
            return new(Name, ProviderInvariantName, FactoryTypeAssemblyQualifiedName);
        }
    }
}
