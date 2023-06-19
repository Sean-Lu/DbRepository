using Sean.Utility.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Sean.Core.DbRepository;

public static class TableInfoCache
{
    private static readonly ConcurrentDictionary<string, List<string>> _tableInfoCache = new();
    //private static readonly ConcurrentDictionary<string, object> _locker = new();

    public static bool IsTableExists(string dbKey, bool master, string tableName)
    {
        return !string.IsNullOrWhiteSpace(dbKey)
               && !string.IsNullOrWhiteSpace(tableName)
               && _tableInfoCache.ContainsKey(GetTableKey(dbKey, master, tableName));
    }

    public static bool IsTableFieldExists(string dbKey, bool master, string tableName, string fieldName)
    {
        return !string.IsNullOrWhiteSpace(dbKey)
               && !string.IsNullOrWhiteSpace(tableName)
               && !string.IsNullOrWhiteSpace(fieldName)
               && _tableInfoCache.TryGetValue(GetTableKey(dbKey, master, tableName), out var fields)
               && fields != null
               && fields.Contains(fieldName);
    }

    public static void AddTable(string dbKey, bool master, string tableName)
    {
        if (string.IsNullOrWhiteSpace(dbKey))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(dbKey));
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(tableName));

        if (IsTableExists(dbKey, master, tableName))
        {
            return;
        }

        _tableInfoCache.AddOrUpdate(GetTableKey(dbKey, master, tableName), null);
    }

    public static void AddTableField(string dbKey, bool master, string tableName, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(dbKey))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(dbKey));
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(tableName));
        if (string.IsNullOrWhiteSpace(fieldName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(fieldName));

        var tableKey = GetTableKey(dbKey, master, tableName);
        if (_tableInfoCache.TryGetValue(tableKey, out var fields)
            && fields != null
            && fields.Contains(fieldName))
        {
            return;
        }

        fields ??= new List<string>();
        fields.Add(fieldName);

        _tableInfoCache.AddOrUpdate(tableKey, fields);
    }

    public static void RemoveTable(string dbKey, bool master, string tableName)
    {
        _tableInfoCache.TryRemove(GetTableKey(dbKey, master, tableName), out _);
    }

    public static void RemoveTableField(string dbKey, bool master, string tableName, string fieldName)
    {
        var tableKey = GetTableKey(dbKey, master, tableName);
        if (!_tableInfoCache.TryGetValue(tableKey, out var fields)
            || fields == null
            || !fields.Contains(fieldName))
        {
            return;
        }

        fields.Remove(fieldName);

        _tableInfoCache.AddOrUpdate(tableKey, fields);
    }

    public static void Clear()
    {
        _tableInfoCache.Clear();
    }

    private static string GetTableKey(string dbKey, bool master, string tableName)
    {
        return $"{dbKey}_{master}_{tableName}";
    }
}