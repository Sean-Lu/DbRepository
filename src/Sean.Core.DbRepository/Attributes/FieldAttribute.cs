﻿namespace Sean.Core.DbRepository
{
    /// <summary>
    /// 数据库表字段
    /// </summary>
    public class FieldAttribute : BaseAttribute
    {
        public FieldAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
