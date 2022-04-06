using System;

namespace Sean.Core.DbRepository
{
    public class UpdateableSqlBuilder : BaseSqlBuilder
    {
        public const string SqlTemplate = "UPDATE {0} SET {1}{2};";

        public IUpdateableSql Build()
        {
            throw new NotImplementedException();
        }
    }
}