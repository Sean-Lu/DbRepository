using System;

namespace Sean.Core.DbRepository
{
    /// <summary>
    /// 忽略字段
    /// </summary>
    [Obsolete("请使用 System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute 代替。")]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class IgnoreAttribute : BaseAttribute
    {

    }
}
