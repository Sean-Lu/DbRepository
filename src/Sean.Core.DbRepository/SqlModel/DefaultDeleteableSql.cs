namespace Sean.Core.DbRepository
{
    public class DefaultDeleteableSql : IDeleteableSql
    {
        public object Parameter { get; set; }
        public string Sql { get; set; }
    }
}