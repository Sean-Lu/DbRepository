using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository.Util;

public static class SqlParameterUtil
{
    public const string SqlParameterNamePrefixRegex = "[?@:$]";

    private sealed class SqlParameterToken
    {
        public int Index { get; set; }
        public int Length { get; set; }
        public string Name { get; set; }
    }

    public static Dictionary<string, object> ConvertToDicParameter<TEntity>(TEntity entity, Expression<Func<TEntity, object>> fieldExpression = null)
    {
        var fields = fieldExpression?.GetFieldNames();
        return ConvertToDicParameter(entity, fields);
    }

    public static IEnumerable<DbParameter> ConvertToDbParameters(DatabaseType dbType, ISqlCommand sqlCommand, Func<DbParameter> dbParameterFactory)
    {
        if (sqlCommand?.Parameter == null)
        {
            return null;
        }

        var sql = sqlCommand.Sql;
        var parameter = sqlCommand.Parameter;

        if (parameter is IEnumerable<DbParameter> listDbParameters)
        {
            return listDbParameters;
        }

        var dicParameters = ConvertToDicParameter(parameter);
        if (dicParameters == null)
        {
            return new List<DbParameter>();
        }

        if (sqlCommand.UseQuestionMarkParameter)
        {
            return ConvertToDbParameters(dbType, dicParameters, dbParameterFactory);
        }

        if (dbType == DatabaseType.MsAccess)
        {
            if (sqlCommand.SqlParameterSorted && sqlCommand.UnusedSqlParameterRemoved)
            {
                return ConvertToDbParameters(dbType, dicParameters, dbParameterFactory);
            }

            var sortedSqlParameters = ParseSqlParameters(sql, dbType);
            var newDicParameters = new Dictionary<string, object>();
            foreach (var keyValuePair in sortedSqlParameters.OrderBy(parameter => parameter.Value))
            {
                var paraName = keyValuePair.Key;
                if (!dicParameters.ContainsKey(paraName))
                {
                    throw new InvalidOperationException($"The sql parameter [{paraName}] does not exist.");
                }

                newDicParameters.Add(paraName, dicParameters[paraName]);
            }
            return ConvertToDbParameters(dbType, newDicParameters, dbParameterFactory);
        }

        if (!sqlCommand.UnusedSqlParameterRemoved)
        {
            // 字典可能由调用方或构造器复用，执行时的参数过滤不能删除其原始内容。
            if (ReferenceEquals(dicParameters, parameter))
                dicParameters = new Dictionary<string, object>(dicParameters, dicParameters.Comparer);
            RemoveUnusedParameters(dicParameters, sql, dbType);
        }

        return ConvertToDbParameters(dbType, dicParameters, dbParameterFactory);
    }

    public static List<DbParameter> ConvertToDbParameters(DatabaseType dbType, Dictionary<string, object> dicParameters, Func<DbParameter> dbParameterFactory)
    {
        var result = new List<DbParameter>();
        if (dicParameters == null)
        {
            return result;
        }

        foreach (var keyValuePair in dicParameters)
        {
            var sqlParameter = dbParameterFactory();
            sqlParameter.ParameterName = keyValuePair.Key;
            sqlParameter.SetParameterTypeAndValue(keyValuePair.Value, dbType);
            result.Add(sqlParameter);
        }
        return result;
    }

    public static IEnumerable<PropertyInfo> FilterParameters(IEnumerable<PropertyInfo> parameters, string sql)
    {
        var list = new List<PropertyInfo>();
        HashSet<string> sqlParameterNames = null;
        foreach (var p in parameters)
        {
            sqlParameterNames ??= GetSqlParameterNames(sql);
            if (sqlParameterNames.Contains(p.Name))
            {
                list.Add(p);
            }
        }
        return list;
    }
    public static IEnumerable<string> FilterParameters(IEnumerable<string> parameters, string sql)
    {
        var list = new List<string>();
        HashSet<string> sqlParameterNames = null;
        foreach (var p in parameters)
        {
            sqlParameterNames ??= GetSqlParameterNames(sql);
            if (sqlParameterNames.Contains(p))
            {
                list.Add(p);
            }
        }
        return list;
    }
    public static void RemoveUnusedParameters(Dictionary<string, object> parameters, string sql)
    {
        RemoveUnusedParameters(parameters, sql, DatabaseType.Unknown);
    }

    internal static void RemoveUnusedParameters(Dictionary<string, object> parameters, string sql, DatabaseType dbType)
    {
        if (parameters.Count == 0)
        {
            return;
        }

        var sqlParameterNames = GetSqlParameterNames(sql, dbType);
        for (var i = parameters.Count - 1; i >= 0; i--)
        {
            var item = parameters.ElementAt(i);
            if (!sqlParameterNames.Contains(item.Key))
            {
                parameters.Remove(item.Key);
            }
        }
    }

    public static Dictionary<string, int> ParseSqlParameters(string sql)
    {
        return ParseSqlParameters(sql, DatabaseType.Unknown);
    }

    internal static Dictionary<string, int> ParseSqlParameters(string sql, DatabaseType dbType)
    {
        var dict = new Dictionary<string, int>(16);
        var index = 0;
        foreach (var token in ParseSqlParameterTokens(sql, false, dbType))
        {
            if (!dict.ContainsKey(token.Name))
            {
                dict.Add(token.Name, ++index);
            }
        }
        return dict;
    }

    internal static IEnumerable<string> ParseSqlParameterNamesInOrder(string sql, DatabaseType dbType)
    {
        // 位置参数必须保留重复项，例如 @id + @id 转为两个 ? 后需要绑定两次相同的值。
        return ParseSqlParameterTokens(sql, false, dbType).Select(token => token.Name);
    }

    public static string UseQuestionMarkParameter(string sql)
    {
        return UseQuestionMarkParameter(sql, DatabaseType.Unknown);
    }

    internal static string UseQuestionMarkParameter(string sql, DatabaseType dbType)
    {
        return ReplaceTokens(sql, ParseSqlParameterTokens(sql, false, dbType), (_, _) => "?");
    }

    public static string ReplaceParameter(string sql, string paraName, string replace)
    {
        var token = ParseSqlParameterTokens(sql, false, DatabaseType.Unknown)
            .FirstOrDefault(parameter => string.Equals(parameter.Name, paraName, StringComparison.OrdinalIgnoreCase));
        return token == null
            ? sql
            : sql.Substring(0, token.Index) + replace + sql.Substring(token.Index + token.Length);
    }

    internal static string ReplaceSqlParameters(string sql, Func<string, string> replacement, DatabaseType dbType)
    {
        if (replacement == null) throw new ArgumentNullException(nameof(replacement));

        return ReplaceTokens(sql, ParseSqlParameterTokens(sql, false, dbType), (token, _) => replacement(token.Name));
    }

    internal static string ReplaceQuestionMarkParameters(string sql, Func<int, string> replacement, DatabaseType dbType)
    {
        if (replacement == null) throw new ArgumentNullException(nameof(replacement));

        var positionalParameters = ParseSqlParameterTokens(sql, true, dbType)
            .Where(parameter => parameter.Name == null)
            .ToList();
        return ReplaceTokens(sql, positionalParameters, (_, index) => replacement(index));
    }

    private static HashSet<string> GetSqlParameterNames(string sql, DatabaseType dbType = DatabaseType.Unknown)
    {
        return new HashSet<string>(
            ParseSqlParameterTokens(sql, false, dbType).Select(parameter => parameter.Name),
            StringComparer.OrdinalIgnoreCase);
    }

    private static string ReplaceTokens(string sql, IList<SqlParameterToken> tokens,
        Func<SqlParameterToken, int, string> replacement)
    {
        if (tokens.Count == 0)
        {
            return sql;
        }

        var result = new StringBuilder(sql.Length);
        var previousIndex = 0;
        for (var index = 0; index < tokens.Count; index++)
        {
            var token = tokens[index];
            result.Append(sql, previousIndex, token.Index - previousIndex);
            var replace = replacement(token, index);
            result.Append(replace ?? sql.Substring(token.Index, token.Length));
            previousIndex = token.Index + token.Length;
        }
        result.Append(sql, previousIndex, sql.Length - previousIndex);
        return result.ToString();
    }

    /// <summary>
    /// 扫描可执行 SQL 中的参数标记，跳过字符串、引用标识符和注释。
    /// 这里只做参数识别所需的轻量扫描，不尝试解析完整 SQL 语法；方括号不会整体跳过，
    /// 因为 PostgreSQL 数组下标或切片中也可能包含真实参数，例如 values[1:@upper]。
    /// </summary>
    private static List<SqlParameterToken> ParseSqlParameterTokens(string sql, bool includePositionalQuestionMarks,
        DatabaseType dbType)
    {
        if (sql == null) throw new ArgumentNullException(nameof(sql));

        if (dbType == DatabaseType.Unknown && sql.IndexOf("\\'", StringComparison.Ordinal) >= 0)
        {
            // 公开工具方法没有数据库类型参数，无法判断 \' 是 MySQL 转义还是 ANSI 字符串末尾前的普通反斜杠。
            // 两种规则分别扫描后取并集，宁可保留疑似参数，也不能把后面的真实参数误删；已知方言仍使用精确规则。
            var ansiTokens = ParseSqlParameterTokensCore(sql, includePositionalQuestionMarks, DatabaseType.Unknown);
            var mySqlTokens = ParseSqlParameterTokensCore(sql, includePositionalQuestionMarks, DatabaseType.MySql);
            return ansiTokens.Concat(mySqlTokens)
                .GroupBy(token => token.Index)
                .Select(group => group.First())
                .OrderBy(token => token.Index)
                .ToList();
        }

        return ParseSqlParameterTokensCore(sql, includePositionalQuestionMarks, dbType);
    }

    private static List<SqlParameterToken> ParseSqlParameterTokensCore(string sql, bool includePositionalQuestionMarks,
        DatabaseType dbType)
    {

        var result = new List<SqlParameterToken>();
        var index = 0;
        while (index < sql.Length)
        {
            var character = sql[index];
            if (character == '\'' || character == '"' || character == '`')
            {
                // PostgreSQL 的 E'...' 明确启用反斜杠转义；MySQL 系仅在已知数据库类型时按默认模式处理。
                // 未知数据库保持 ANSI 规则，避免把 SQL Server 字符串中的普通反斜杠误判为转义符。
                var backslashEscaped = character == '\''
                    && (IsPostgreSqlEscapeString(sql, index) || IsMySqlCompatible(dbType));
                index = SkipQuotedValue(sql, index, character, backslashEscaped);
                continue;
            }

            if (character == '[' && UsesBracketQuotedIdentifiers(dbType))
            {
                // 仅在明确使用方括号引用标识符的方言中跳过；PostgreSQL 中同一语法位置可能是数组下标。
                index = SkipQuotedValue(sql, index, ']', false);
                continue;
            }

            if (character == '-' && index + 1 < sql.Length && sql[index + 1] == '-')
            {
                // MySQL 系要求 -- 后面是空白或控制字符；其它情况下两个减号仍属于可执行 SQL。
                if (!IsMySqlCompatible(dbType) || IsMySqlDashDashComment(sql, index))
                {
                    index = SkipLineComment(sql, index + 2);
                    continue;
                }
            }

            if (character == '#' && IsMySqlCompatible(dbType))
            {
                index = SkipLineComment(sql, index + 1);
                continue;
            }

            if (character == '/' && index + 1 < sql.Length && sql[index + 1] == '*')
            {
                // PostgreSQL 系、SQL Server 和 DB2 支持嵌套块注释；Oracle、MySQL 等在第一个 */ 处结束。
                index = SkipBlockComment(sql, index + 2, SupportsNestedBlockComments(dbType));
                continue;
            }

            if (character == '$' && TrySkipDollarQuotedValue(sql, index, out var dollarQuotedEnd))
            {
                index = dollarQuotedEnd;
                continue;
            }

            if ((character == 'q' || character == 'Q')
                && TrySkipOracleQuotedValue(sql, index, out var oracleQuotedEnd))
            {
                index = oracleQuotedEnd;
                continue;
            }

            if (!IsSqlParameterPrefix(character))
            {
                index++;
                continue;
            }

            // PostgreSQL 的 :: 强转、SQL Server/MySQL 的 @@ 系统变量都不是参数。
            if (index > 0 && sql[index - 1] == character)
            {
                index++;
                continue;
            }

            // 参数前缀不能直接嵌在普通标识符中，例如 Oracle/PostgreSQL 合法标识符 FOO$BAR。
            if (index > 0 && IsSqlIdentifierContinuationCharacter(sql[index - 1]))
            {
                index++;
                continue;
            }

            var nameStart = index + 1;
            var nameEnd = nameStart;
            while (nameEnd < sql.Length && IsParameterNameCharacter(sql[nameEnd]))
            {
                nameEnd++;
            }

            if (nameEnd > nameStart)
            {
                result.Add(new SqlParameterToken
                {
                    Index = index,
                    Length = nameEnd - index,
                    Name = sql.Substring(nameStart, nameEnd - nameStart)
                });
                index = nameEnd;
                continue;
            }

            if (includePositionalQuestionMarks && character == '?')
            {
                result.Add(new SqlParameterToken { Index = index, Length = 1 });
            }
            index++;
        }

        return result;
    }

    private static int SkipQuotedValue(string sql, int startIndex, char closingCharacter, bool backslashEscaped)
    {
        var index = startIndex + 1;
        while (index < sql.Length)
        {
            if (backslashEscaped && sql[index] == '\\' && index + 1 < sql.Length)
            {
                index += 2;
                continue;
            }

            if (sql[index] != closingCharacter)
            {
                index++;
                continue;
            }

            if (index + 1 < sql.Length && sql[index + 1] == closingCharacter)
            {
                index += 2;
                continue;
            }
            return index + 1;
        }
        return sql.Length;
    }

    private static int SkipLineComment(string sql, int index)
    {
        while (index < sql.Length && sql[index] != '\r' && sql[index] != '\n')
        {
            index++;
        }
        return index;
    }

    private static int SkipBlockComment(string sql, int index, bool nested)
    {
        var depth = 1;
        while (index < sql.Length && depth > 0)
        {
            if (nested && index + 1 < sql.Length && sql[index] == '/' && sql[index + 1] == '*')
            {
                depth++;
                index += 2;
            }
            else if (index + 1 < sql.Length && sql[index] == '*' && sql[index + 1] == '/')
            {
                depth--;
                index += 2;
            }
            else
            {
                index++;
            }
        }
        return index;
    }

    private static bool TrySkipDollarQuotedValue(string sql, int startIndex, out int endIndex)
    {
        endIndex = startIndex;
        // PostgreSQL 要求 dollar quote 与前面的标识符分隔，否则 $ 只是标识符的一部分。
        if (startIndex > 0 && IsSqlIdentifierContinuationCharacter(sql[startIndex - 1]))
        {
            return false;
        }

        var tagEnd = startIndex + 1;
        if (tagEnd < sql.Length && sql[tagEnd] != '$' && !IsDollarQuoteTagStart(sql[tagEnd]))
        {
            return false;
        }

        while (tagEnd < sql.Length && IsParameterNameCharacter(sql[tagEnd]))
        {
            tagEnd++;
        }
        if (tagEnd >= sql.Length || sql[tagEnd] != '$')
        {
            return false;
        }

        var delimiter = sql.Substring(startIndex, tagEnd - startIndex + 1);
        var closingIndex = sql.IndexOf(delimiter, tagEnd + 1, StringComparison.Ordinal);
        if (closingIndex < 0)
        {
            return false;
        }

        endIndex = closingIndex + delimiter.Length;
        return true;
    }

    private static bool TrySkipOracleQuotedValue(string sql, int startIndex, out int endIndex)
    {
        endIndex = startIndex;
        // q 必须是独立的 Oracle 替代引用前缀，不能位于普通标识符中。
        if ((startIndex > 0 && IsSqlIdentifierContinuationCharacter(sql[startIndex - 1]))
            || startIndex + 3 >= sql.Length
            || sql[startIndex + 1] != '\'')
        {
            return false;
        }

        var openingCharacter = sql[startIndex + 2];
        var closingCharacter = openingCharacter switch
        {
            '[' => ']',
            '{' => '}',
            '(' => ')',
            '<' => '>',
            _ => openingCharacter
        };
        for (var index = startIndex + 3; index + 1 < sql.Length; index++)
        {
            if (sql[index] == closingCharacter && sql[index + 1] == '\'')
            {
                endIndex = index + 2;
                return true;
            }
        }
        return false;
    }

    private static bool IsPostgreSqlEscapeString(string sql, int quoteIndex)
    {
        var prefixIndex = quoteIndex - 1;
        return prefixIndex >= 0
            && (sql[prefixIndex] == 'e' || sql[prefixIndex] == 'E')
            && (prefixIndex == 0 || !IsSqlIdentifierContinuationCharacter(sql[prefixIndex - 1]));
    }

    private static bool IsMySqlCompatible(DatabaseType dbType)
    {
        return dbType is DatabaseType.MySql
            or DatabaseType.MariaDB
            or DatabaseType.TiDB
            or DatabaseType.OceanBase;
    }

    private static bool IsMySqlDashDashComment(string sql, int startIndex)
    {
        var followingIndex = startIndex + 2;
        return followingIndex >= sql.Length
            || char.IsWhiteSpace(sql[followingIndex])
            || char.IsControl(sql[followingIndex]);
    }

    private static bool UsesBracketQuotedIdentifiers(DatabaseType dbType)
    {
        return dbType is DatabaseType.SqlServer
            or DatabaseType.MsAccess
            or DatabaseType.SQLite;
    }

    private static bool SupportsNestedBlockComments(DatabaseType dbType)
    {
        return dbType is DatabaseType.SqlServer
            or DatabaseType.PostgreSql
            or DatabaseType.OpenGauss
            or DatabaseType.HighgoDB
            or DatabaseType.IvorySQL
            or DatabaseType.KingbaseES
            or DatabaseType.DB2;
    }

    private static bool IsSqlParameterPrefix(char character)
    {
        return character is '?' or '@' or ':' or '$';
    }

    private static bool IsDollarQuoteTagStart(char character)
    {
        return char.IsLetter(character) || character == '_';
    }

    private static bool IsParameterNameCharacter(char character)
    {
        var unicodeCategory = char.GetUnicodeCategory(character);
        return char.IsLetterOrDigit(character)
            || unicodeCategory == UnicodeCategory.NonSpacingMark
            || unicodeCategory == UnicodeCategory.ConnectorPunctuation;
    }

    private static bool IsSqlIdentifierContinuationCharacter(char character)
    {
        // $ 是 Oracle/PostgreSQL 等数据库未引用标识符允许使用的字符，但不是参数名中的 \w 字符。
        return IsParameterNameCharacter(character) || character == '$';
    }

    internal static Dictionary<string, object> ConvertToDicParameter(object instance, IEnumerable<string> fields = null)
    {
        if (instance == null)
        {
            return new Dictionary<string, object>();
        }

        if (instance is Dictionary<string, object> oldParameter)
        {
            return oldParameter;
        }

        var paramDic = new Dictionary<string, object>();
        if (fields != null && fields.Any())
        {
            // 指定字段
            var type = instance.GetType();
            var tableFieldInfos = type.GetEntityInfo().FieldInfos;
            foreach (var field in fields)
            {
                var fieldInfo = tableFieldInfos.Find(c => c.FieldName == field);
                if (fieldInfo == null)
                {
                    throw new InvalidOperationException($"Table field [{field}] not found in [{type.FullName}].");
                }

                paramDic.Add(fieldInfo.Property.Name, fieldInfo.Property.GetValue(instance, null));
            }
        }
        else
        {
            // 所有字段
            foreach (var fieldInfo in instance.GetType().GetEntityInfo().FieldInfos)
            {
                paramDic.Add(fieldInfo.Property.Name, fieldInfo.Property.GetValue(instance, null));
            }
        }
        return paramDic;
    }
}
