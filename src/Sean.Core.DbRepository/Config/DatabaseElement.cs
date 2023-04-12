#if NETFRAMEWORK
using System;
using System.Configuration;

namespace Sean.Core.DbRepository
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

        public DatabaseType? DbType => _customSetPropertyValue ? _name : Enum.TryParse(Name, out DatabaseType result) ? result : null;

        /// <summary>
        /// The name of the database
        /// </summary>
        [ConfigurationProperty(PropertyName, IsRequired = true, IsKey = true)]
        public string Name => (string)this[PropertyName];

        [ConfigurationProperty(PropertyProviderInvariantName, IsRequired = true)]
        public string ProviderInvariantName => _customSetPropertyValue ? _providerInvariantName : (string)this[PropertyProviderInvariantName];

        /// <summary>
        /// FactoryType, AssemblyQualifiedName
        /// </summary>
        [ConfigurationProperty(PropertyFactoryTypeAssemblyQualifiedName, IsRequired = true)]
        public string FactoryTypeAssemblyQualifiedName => _customSetPropertyValue ? _factoryTypeAssemblyQualifiedName : (string)this[PropertyFactoryTypeAssemblyQualifiedName];
    }
}
#endif