using System;
using System.Collections.Concurrent;
using Sean.Utility.Extensions;

namespace Sean.Core.DbRepository.Dapper.Cache
{
    public class TableInfoCache
    {
        private static readonly ConcurrentDictionary<string, bool> _tableExistsDic = new ConcurrentDictionary<string, bool>();

        public static bool IsTableExists(string tableName)
        {
            if (!string.IsNullOrWhiteSpace(tableName) && _tableExistsDic.TryGetValue(tableName, out var tableExists) && tableExists)
            {
                return true;
            }

            return false;
        }

        public static void IsTableExists(string tableName, bool exist)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(tableName));
            _tableExistsDic.AddOrUpdate(tableName, exist);
        }
    }
}
