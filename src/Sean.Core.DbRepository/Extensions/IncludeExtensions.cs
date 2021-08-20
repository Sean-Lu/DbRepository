using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sean.Core.DbRepository.Extensions
{
    public static class IncludeExtensions
    {
        public static string ToSqlString(this Include include)
        {
            switch (include)
            {
                case Include.None:
                    return string.Empty;
                case Include.Left:
                    return "(";
                case Include.Right:
                    return ")";
                default:
                    throw new NotSupportedException(include.ToString());
            }
        }
    }
}
