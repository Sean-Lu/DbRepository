namespace Sean.Core.DbRepository
{
    public class DefaultUpdateableSql : IUpdateableSql
    {
        public object Parameter { get; set; }
        public string UpdateSql { get; set; }
    }
}