namespace Sean.Core.DbRepository
{
    public class DefaultCountableSql : ICountableSql
    {
        public object Parameter { get; set; }
        public string Sql { get; set; }
    }
}