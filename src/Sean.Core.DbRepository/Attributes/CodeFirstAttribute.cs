using System;

namespace Sean.Core.DbRepository;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class CodeFirstAttribute : BaseAttribute
{
}