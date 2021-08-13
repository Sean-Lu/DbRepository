using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sean.Core.DbRepository.Extensions
{
    public static class WhereSqlKeywordExtensions
    {
        public static string ToSqlString(this WhereSqlKeyword keyword)
        {
            switch (keyword)
            {
                case WhereSqlKeyword.None:
                    return string.Empty;
                case WhereSqlKeyword.And:
                    return "AND";
                case WhereSqlKeyword.Or:
                    return "OR";
                default:
                    throw new NotSupportedException(keyword.ToString());
            }
        }
    }
}
