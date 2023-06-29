using System;

namespace Sean.Core.DbRepository;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class SequenceAttribute : BaseAttribute
{
    public SequenceAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }
    public bool UseTrigger { get; } = true;
}