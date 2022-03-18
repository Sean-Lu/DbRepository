using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Sean.Core.DbRepository
{
    [ConfigurationCollection(typeof(DatabaseElement), AddItemName = "database")]
    public class DatabaseElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new DatabaseElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return (element as DatabaseElement)?.Name;
        }

        public DatabaseElement this[int i] => BaseGet(i) as DatabaseElement;

        public void Clear()
        {
            BaseClear();
        }

        public void Add(DatabaseElement element)
        {
            BaseAdd(element);
        }
    }
}
