using System.Configuration;
using Sean.Core.DbRepository.Factory;

namespace Sean.Core.DbRepository.Config
{
    public class DatabaseElement : ConfigurationElement
    {
        private const string PropertyName = "name";
        private const string PropertyProviderInvariantName = "providerInvariantName";
        private const string PropertyFactoryTypeAssemblyQualifiedName = "factoryTypeAssemblyQualifiedName";

        private readonly DatabaseType _name;
        private readonly string _providerInvariantName;
        private readonly string _factoryTypeAssemblyQualifiedName;
        private readonly bool _customSetPropertyValue;

        public DatabaseElement()
        {
        }

        public DatabaseElement(DatabaseType name, string providerInvariantName, string factoryTypeAssemblyQualifiedName)
        {
            _name = name;
            _providerInvariantName = providerInvariantName;
            _factoryTypeAssemblyQualifiedName = factoryTypeAssemblyQualifiedName;
            _customSetPropertyValue = true;
        }

        /// <summary>
        /// The name of the database
        /// </summary>
        [ConfigurationProperty(PropertyName, IsRequired = true, IsKey = true)]
        public DatabaseType Name
        {
            get
            {
                if (_customSetPropertyValue)
                    return _name;
                return (DatabaseType)this[PropertyName];
            }
        }

        [ConfigurationProperty(PropertyProviderInvariantName, IsRequired = true)]
        public string ProviderInvariantName
        {
            get
            {
                if (_customSetPropertyValue)
                    return _providerInvariantName;
                return (string)this[PropertyProviderInvariantName];
            }
        }

        /// <summary>
        /// FactoryType, AssemblyQualifiedName
        /// </summary>
        [ConfigurationProperty(PropertyFactoryTypeAssemblyQualifiedName, IsRequired = false)]
        public string FactoryTypeAssemblyQualifiedName
        {
            get
            {
                if (_customSetPropertyValue)
                    return _factoryTypeAssemblyQualifiedName;
                return (string)this[PropertyFactoryTypeAssemblyQualifiedName];
            }
        }

        public DbProviderMap Map()
        {
            return new(Name, ProviderInvariantName, FactoryTypeAssemblyQualifiedName);
        }
    }
}
