#if NETFRAMEWORK
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;

namespace Sean.Core.DbRepository.Extensions;

public static class OdbcConnectionExtensions
{
    public static List<string> GetTableNames(this OdbcConnection connection)
    {
        var result = new List<string>();
        DataTable schemaTable = connection.GetSchema("Tables");
        if (schemaTable != null)
        {
            foreach (DataRow row in schemaTable.Rows)
            {
                string tableName = (string)row["TABLE_NAME"];
                //Console.WriteLine(tableName);
                result.Add(tableName);
            }
        }
        return result;
    }

    public static bool IsTableExists(this OdbcConnection connection, string tableName)
    {
        DataTable schemaTable = connection.GetSchema("Tables", new string[] { null, null, tableName, "TABLE" });
        if (schemaTable != null && schemaTable.Rows.Count > 0)
        {
            //Console.WriteLine("Table exists.");
            return true;
        }

        //Console.WriteLine("Table does not exist.");
        return false;
    }

    public static bool IsTableFieldExists(this OdbcConnection connection, string tableName, string fieldName)
    {
        DataTable schemaTable = connection.GetSchema("Columns", new string[] { null, null, tableName, fieldName });
        if (schemaTable != null && schemaTable.Rows.Count > 0)
        {
            //Console.WriteLine("Column exists.");
            return true;
        }

        //Console.WriteLine("Column does not exist.");
        return false;
    }
}
#endif