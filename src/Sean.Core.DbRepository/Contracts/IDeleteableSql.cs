namespace Sean.Core.DbRepository
{
    public interface IDeleteableSql : ISqlParameter
    {
        string Sql { get; set; }
    }
}