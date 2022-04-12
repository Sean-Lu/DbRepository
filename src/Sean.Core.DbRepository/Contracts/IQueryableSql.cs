namespace Sean.Core.DbRepository
{
    public interface IQueryableSql : ISqlParameter
    {
        string Sql { get; set; }
    }
}