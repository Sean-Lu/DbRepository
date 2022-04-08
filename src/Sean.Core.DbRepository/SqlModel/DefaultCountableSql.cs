namespace Sean.Core.DbRepository
{
    public class DefaultCountableSql : ICountableSql
    {
        public object Parameter { get; set; }
        public string CountSql { get; set; }
    }
}