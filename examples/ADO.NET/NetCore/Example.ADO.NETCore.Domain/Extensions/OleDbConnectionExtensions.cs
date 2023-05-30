#if UseMsAccess
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace Example.ADO.NETCore.Domain.Extensions
{
    public static class OleDbConnectionExtensions
    {
        public static List<string> GetTableNames(this OleDbConnection connection)
        {
            var result = new List<string>();
            DataTable schemaTable = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[] { null, null, null, "TABLE" });
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

        public static bool IsTableExists(this OleDbConnection connection, string tableName)
        {
            DataTable schemaTable = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[] { null, null, tableName, "TABLE" });
            if (schemaTable != null && schemaTable.Rows.Count > 0)
            {
                //Console.WriteLine("Table exists.");
                return true;
            }

            //Console.WriteLine("Table does not exist.");
            return false;
        }

        public static bool IsTableFieldExists(this OleDbConnection connection, string tableName, string fieldName)
        {
            DataTable schemaTable = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Columns, new object[] { null, null, tableName, fieldName });
            if (schemaTable != null && schemaTable.Rows.Count > 0)
            {
                //Console.WriteLine("Column exists.");
                return true;
            }

            //Console.WriteLine("Column does not exist.");
            return false;
        }
    }
}
#endif