using System;

namespace Sean.Core.DbRepository;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class NumberAttribute : BaseAttribute
{
    public NumberAttribute(int precision)
    {
        Precision = precision;
    }
    public NumberAttribute(int precision, int scale)
    {
        Precision = precision;
        Scale = scale;
    }

    public int Precision { get; }
    public int Scale { get; }
}