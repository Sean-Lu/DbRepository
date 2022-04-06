using System;

namespace Sean.Core.DbRepository
{
    public class QueryableSqlBuilder : BaseSqlBuilder
    {
        public const string SqlTemplate = "SELECT {1} FROM {0}{2};";

        public IQueryableSql Build()
        {
            throw new NotImplementedException();
        }
    }
}