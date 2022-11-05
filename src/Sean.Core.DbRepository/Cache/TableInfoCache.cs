using Sean.Utility.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Sean.Core.DbRepository
{
    public static class TableInfoCache
    {
        private static readonly ConcurrentDictionary<string, List<string>> _tableInfoCache = new();

        public static bool Exists(string tableName)
        {
            return !string.IsNullOrWhiteSpace(tableName) && _tableInfoCache.ContainsKey(tableName);
        }

        public static bool Exists(string tableName, string fieldName)
        {
            return !string.IsNullOrWhiteSpace(tableName)
                   && !string.IsNullOrWhiteSpace(fieldName)
                   && _tableInfoCache.TryGetValue(tableName, out var fields)
                   && fields != null
                   && fields.Contains(fieldName);
        }

        public static void Add(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(tableName));

            if (Exists(tableName))
            {
                return;
            }

            _tableInfoCache.AddOrUpdate(tableName, null);
        }

        public static void Add(string tableName, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(tableName));
            if (string.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(fieldName));

            if (_tableInfoCache.TryGetValue(tableName, out var fields)
                && fields != null
                && fields.Contains(fieldName))
            {
                return;
            }

            fields ??= new List<string>();
            fields.Add(fieldName);

            _tableInfoCache.AddOrUpdate(tableName, fields);
        }

        public static void Remove(string tableName)
        {
            _tableInfoCache.TryRemove(tableName, out _);
        }

        public static void Remove(string tableName, string fieldName)
        {
            if (!_tableInfoCache.TryGetValue(tableName, out var fields)
                || fields == null
                || !fields.Contains(fieldName))
            {
                return;
            }

            fields.Remove(fieldName);

            _tableInfoCache.AddOrUpdate(tableName, fields);
        }

        public static void Clear()
        {
            _tableInfoCache.Clear();
        }
    }
}
