#if NET40
using System;

namespace Sean.Core.DbRepository
{
    /// <summary>
    /// 指定数据库生成属性值的方式。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class DatabaseGeneratedAttribute : BaseAttribute
    {
        public DatabaseGeneratedAttribute(DatabaseGeneratedOption databaseGeneratedOption)
        {
            DatabaseGeneratedOption = databaseGeneratedOption;
        }

        public DatabaseGeneratedOption DatabaseGeneratedOption { get; }
    }

    /// <summary>
    /// 表示用于为数据库中的属性生成值的模式。
    /// </summary>
    public enum DatabaseGeneratedOption
    {
        /// <summary>
        /// 数据库不生成值。
        /// </summary>
        None = 0,
        /// <summary>
        /// 在插入行时，数据库将生成值。
        /// </summary>
        Identity = 1,
        /// <summary>
        /// 在插入或更新行时，数据库将生成值。
        /// </summary>
        Computed = 2
    }
}
#endif
