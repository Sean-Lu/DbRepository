using System;

namespace Sean.Core.DbRepository;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class NamingConventionAttribute : BaseAttribute
{
    public NamingConventionAttribute(NamingConvention namingConvention)
    {
        NamingConvention = namingConvention;
    }

    public NamingConvention NamingConvention { get; }
}