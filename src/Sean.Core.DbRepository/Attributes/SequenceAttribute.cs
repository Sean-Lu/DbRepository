using System;

namespace Sean.Core.DbRepository;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class SequenceAttribute : BaseAttribute
{
    public SequenceAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Sequence name.
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// Whether to use trigger. The default value is true.
    /// </summary>
    public bool UseTrigger { get; } = true;
}