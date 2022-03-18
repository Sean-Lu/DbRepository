namespace Sean.Core.DbRepository
{
    public interface ICountableSql : ISqlParameter
    {
        string CountSql { get; }
    }
}