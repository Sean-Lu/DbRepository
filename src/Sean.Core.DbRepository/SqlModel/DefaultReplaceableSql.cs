namespace Sean.Core.DbRepository
{
    public class DefaultReplaceableSql : IReplaceableSql
    {
        public object Parameter { get; set; }
        public string Sql { get; set; }
    }
}
