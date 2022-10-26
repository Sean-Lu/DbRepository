using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Sean.Core.DbRepository
{
    public class TableNameCache
    {
        private static readonly ConcurrentBag<string> _tableNameCache = new();

        public static bool Exists(string tableName)
        {
            return !string.IsNullOrWhiteSpace(tableName) && _tableNameCache.Contains(tableName);
        }

        public static void Add(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(tableName));

            if (!_tableNameCache.Contains(tableName))
            {
                _tableNameCache.Add(tableName);
            }
        }

        public static void Clear()
        {
            while (!_tableNameCache.IsEmpty)
            {
                _tableNameCache.TryTake(out _);
            }
        }
    }
}
