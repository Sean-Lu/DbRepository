namespace Sean.Core.DbRepository
{
    public class DefaultInsertableSql : IInsertableSql
    {
        public object Parameter { get; set; }
        public string Sql { get; set; }
    }
}
