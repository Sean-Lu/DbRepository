using System;

namespace Sean.Core.DbRepository;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class NumericAttribute : BaseAttribute
{
    public NumericAttribute(int precision)
    {
        Precision = precision;
    }
    public NumericAttribute(int precision, int scale)
    {
        Precision = precision;
        Scale = scale;
    }

    public int Precision { get; }
    public int Scale { get; }
}