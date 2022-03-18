namespace Sean.Core.DbRepository
{
    public interface IInsertableSql : ISqlParameter
    {
        string InsertSql { get; }
    }
}