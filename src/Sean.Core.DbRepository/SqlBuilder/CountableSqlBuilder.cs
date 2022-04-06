using System;

namespace Sean.Core.DbRepository
{
    public class CountableSqlBuilder : BaseSqlBuilder
    {
        public const string SqlTemplate = "SELECT COUNT(1) FROM {0}{1};";

        public ICountableSql Build()
        {
            throw new NotImplementedException();
        }
    }
}