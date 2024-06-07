using System.Text.RegularExpressions;
using System;

namespace Sean.Core.DbRepository.Extensions;

public static class StringExtensions
{
    public static string ToNamingConvention(this string str, NamingConvention convention)
    {
        if (string.IsNullOrWhiteSpace(str))
            return str;

        switch (convention)
        {
            case NamingConvention.Default:
                return str;
            case NamingConvention.PascalCase:
                var camelCaseString = str.ToNamingConvention(NamingConvention.CamelCase);
                return char.ToUpper(camelCaseString[0]) + camelCaseString.Substring(1);
            case NamingConvention.CamelCase:
                var snakeCaseString = str.ToNamingConvention(NamingConvention.SnakeCase);
                return Regex.Replace(snakeCaseString, @"_\w", m => m.Value.Substring(1).ToUpper());
            case NamingConvention.SnakeCase:
                return Regex.Replace(str, @"([A-Z])([A-Z][a-z])|([a-z0-9])([A-Z])", "$1$3_$2$4").ToLower();
            case NamingConvention.UpperSnakeCase:
                return Regex.Replace(str, @"([A-Z])([A-Z][a-z])|([a-z0-9])([A-Z])", "$1$3_$2$4").ToUpper();
            default:
                throw new NotSupportedException("Unsupported naming convention.");
        }
    }
}