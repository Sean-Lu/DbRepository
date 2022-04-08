namespace Sean.Core.DbRepository
{
    public class DefaultInsertableSql : IInsertableSql
    {
        public object Parameter { get; set; }
        public string InsertSql { get; set; }
    }
}
