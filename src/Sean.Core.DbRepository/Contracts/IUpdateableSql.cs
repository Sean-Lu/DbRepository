﻿namespace Sean.Core.DbRepository
{
    public interface IUpdateableSql : ISqlParameter
    {
        string Sql { get; set; }
    }
}