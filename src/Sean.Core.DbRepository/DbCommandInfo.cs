using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace Sean.Core.DbRepository
{
    /// <summary>
    /// Database command info
    /// </summary>
    public class DbCommandInfo
    {
        /// <summary>
        /// Database transaction
        /// </summary>
        public IDbTransaction Transaction { get; set; }
        /// <summary>
        /// Database connection
        /// </summary>
        public IDbConnection Connection { get; set; }

        /// <summary>
        /// Command type
        /// </summary>
        public CommandType CommandType { get; set; }
        /// <summary>
        /// Command text
        /// </summary>
        public string CommandText { get; set; }
        /// <summary>
        /// The time (in seconds) to wait for the command to execute. The default value is 30 seconds.
        /// </summary>
        public int? CommandTimeout { get; set; }
        /// <summary>
        /// The parameters of the SQL statement or stored procedure.
        /// </summary>
        public IEnumerable<DbParameter> Parameters { get; set; }

        public DbCommandInfo(string commandText, IEnumerable<DbParameter> parameters = null, CommandType commandType = CommandType.Text, int? commandTimeout = null)
        {
            CommandText = commandText;
            Parameters = parameters;
            CommandType = commandType;
            CommandTimeout = commandTimeout;
        }
    }
}
