#if NET40
using System;

namespace Sean.Core.DbRepository
{
    /// <summary>
    /// 指定类将映射到的数据库表。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TableAttribute : BaseAttribute
    {
        public TableAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public string Schema { get; set; }
    }
}
#endif
