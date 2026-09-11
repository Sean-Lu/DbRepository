using System;
using System.Collections.Concurrent;

namespace Sean.Core.DbRepository;

public static class TableInfoCache
{
    private static readonly ConcurrentDictionary<Tuple<string, bool, string>, ConcurrentDictionary<string, byte>> _tableInfoCache = new();

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
               && fields.ContainsKey(fieldName);
    }

    public static void AddTable(string dbKey, bool master, string tableName)
    {
        if (string.IsNullOrWhiteSpace(dbKey))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(dbKey));
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(tableName));

        _tableInfoCache.GetOrAdd(
            GetTableKey(dbKey, master, tableName),
            _ => new ConcurrentDictionary<string, byte>());
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
        var fields = _tableInfoCache.GetOrAdd(
            tableKey,
            _ => new ConcurrentDictionary<string, byte>());
        fields.TryAdd(fieldName, 0);
    }

    public static void RemoveTable(string dbKey, bool master, string tableName)
    {
        _tableInfoCache.TryRemove(GetTableKey(dbKey, master, tableName), out _);
    }

    public static void RemoveTableField(string dbKey, bool master, string tableName, string fieldName)
    {
        var tableKey = GetTableKey(dbKey, master, tableName);
        if (!_tableInfoCache.TryGetValue(tableKey, out var fields))
        {
            return;
        }

        fields.TryRemove(fieldName, out _);
    }

    public static void Clear()
    {
        _tableInfoCache.Clear();
    }

    private static Tuple<string, bool, string> GetTableKey(string dbKey, bool master, string tableName)
    {
        // 分别比较连接、主从标记和表名，避免名称中的分隔符导致跨数据库缓存误命中或误删除。
        return Tuple.Create(dbKey, master, tableName);
    }
}
