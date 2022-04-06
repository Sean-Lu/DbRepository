using System;

namespace Sean.Core.DbRepository
{
    public class InsertableSqlBuilder : BaseSqlBuilder
    {
        public const string SqlTemplate = "INSERT INTO {0}({1}) VALUES{2};";

        public IInsertableSql Build()
        {
            throw new NotImplementedException();
        }
    }
}
