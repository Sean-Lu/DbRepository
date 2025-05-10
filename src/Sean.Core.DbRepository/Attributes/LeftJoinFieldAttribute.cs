using System;

namespace Sean.Core.DbRepository;

[AttributeUsage(AttributeTargets.Property)]
public class LeftJoinFieldAttribute : Attribute
{
    public string JoinAlias { get; set; }
    public string TargetPropertyName { get; set; }

    public LeftJoinFieldAttribute()
    {
    }
    public LeftJoinFieldAttribute(string joinAlias, string targetPropertyName)
    {
        JoinAlias = joinAlias ?? throw new ArgumentNullException(nameof(joinAlias));
        TargetPropertyName = targetPropertyName ?? throw new ArgumentNullException(nameof(targetPropertyName));
    }
}