namespace Sean.Core.DbRepository
{
    public interface IQueryableSql : ISqlParameter
    {
        string QuerySql { get; set; }
    }
}