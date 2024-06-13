using System.Text.RegularExpressions;

namespace Sean.Core.DbRepository.Util;

public static class SqlMatchUtil
{
    public static bool IsWriteOperation(string sql)
    {
        // 使用正则表达式匹配不区分大小写的写入操作关键字
        var writeOperationsPattern = @"^\s*(INSERT|UPDATE|DELETE|REPLACE|ALTER)\s";
        return Regex.IsMatch(sql, writeOperationsPattern, RegexOptions.IgnoreCase);
    }
}