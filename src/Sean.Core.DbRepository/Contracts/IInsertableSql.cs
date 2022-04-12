namespace Sean.Core.DbRepository
{
    public interface IInsertableSql : ISqlParameter
    {
        string Sql { get; set; }
    }
}