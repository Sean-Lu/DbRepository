using System;

namespace Sean.Core.DbRepository
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class SequenceAttribute : BaseAttribute
    {
        public SequenceAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
