using System;

namespace Sean.Core.DbRepository
{
    public class DeleteableSqlBuilder : BaseSqlBuilder
    {
        public const string SqlTemplate = "DELETE FROM {0}{1};";

        public IDeleteableSql Build()
        {
            throw new NotImplementedException();
        }
    }
}