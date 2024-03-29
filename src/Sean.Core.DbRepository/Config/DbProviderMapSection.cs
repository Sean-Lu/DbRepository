﻿#if NETFRAMEWORK
using System.Configuration;

namespace Sean.Core.DbRepository;

public class DbProviderMapSection : ConfigurationSection
{
    [ConfigurationProperty("databases", IsRequired = false)]
    public DatabaseElementCollection Databases => this["databases"] as DatabaseElementCollection;
}
#endif