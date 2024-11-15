using System;

namespace Sean.Core.DbRepository;

/// <summary>
/// MySQL: DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class AutoUpdateCurrentTimestampAttribute : BaseAttribute
{
    public bool SetDefault { get; }

    public AutoUpdateCurrentTimestampAttribute(bool setDefault)
    {
        SetDefault = setDefault;
    }
}