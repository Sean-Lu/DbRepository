﻿namespace Sean.Core.DbRepository
{
    public class DefaultQueryableSql : IQueryableSql
    {
        public object Parameter { get; set; }
        public string Sql { get; set; }
    }
}