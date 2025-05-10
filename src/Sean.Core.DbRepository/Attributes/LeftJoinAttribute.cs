using System;

namespace Sean.Core.DbRepository;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class LeftJoinAttribute : Attribute
{
    public Type JoinTableType { get; set; }
    public string Alias { get; set; }
    public string LocalKey { get; set; }
    public string ForeignKey { get; set; }

    public LeftJoinAttribute()
    {
    }
    public LeftJoinAttribute(Type joinTableType, string localKey, string foreignKey, string alias = null)
    {
        JoinTableType = joinTableType ?? throw new ArgumentNullException(nameof(joinTableType));
        LocalKey = localKey ?? throw new ArgumentNullException(nameof(localKey));
        ForeignKey = foreignKey ?? throw new ArgumentNullException(nameof(foreignKey));
        Alias = alias;
    }
}