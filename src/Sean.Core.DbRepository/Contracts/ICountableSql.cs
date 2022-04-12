namespace Sean.Core.DbRepository
{
    public interface ICountableSql : ISqlParameter
    {
        string Sql { get; set; }
    }
}