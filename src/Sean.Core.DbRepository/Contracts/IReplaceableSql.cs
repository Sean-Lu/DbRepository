namespace Sean.Core.DbRepository
{
    public interface IReplaceableSql : ISqlParameter
    {
        string Sql { get; set; }
    }
}