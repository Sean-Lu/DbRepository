namespace Sean.Core.DbRepository
{
    public class DefaultDeleteableSql : IDeleteableSql
    {
        public object Parameter { get; set; }
        public string DeleteSql { get; set; }
    }
}