namespace Sean.Core.DbRepository
{
    public interface IUpdateableSql : ISqlParameter
    {
        string UpdateSql { get; set; }
    }
}