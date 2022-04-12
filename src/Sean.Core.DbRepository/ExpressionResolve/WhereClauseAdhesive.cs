using System.Collections.Generic;

namespace Sean.Core.DbRepository
{
    public class WhereClauseAdhesive
    {
        public WhereClauseAdhesive(ISqlAdapter sqlAdapter, IDictionary<string, object> parameters)
        {
            Parameters = parameters;
            SqlAdapter = sqlAdapter;
        }

        public IDictionary<string, object> Parameters { get; }

        public ISqlAdapter SqlAdapter { get; }
    }
}
