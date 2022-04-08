namespace Sean.Core.DbRepository
{
    public interface IDeleteableSql : ISqlParameter
    {
        string DeleteSql { get; set; }
    }
}