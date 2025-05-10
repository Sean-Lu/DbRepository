using System;

namespace Sean.Core.DbRepository;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class NamingConventionAttribute : Attribute
{
    public NamingConventionAttribute(NamingConvention namingConvention)
    {
        NamingConvention = namingConvention;
    }

    public NamingConvention NamingConvention { get; }
}