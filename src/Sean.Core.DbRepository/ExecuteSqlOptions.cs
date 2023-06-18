namespace Sean.Core.DbRepository;

public class ExecuteSqlOptions
{
    public string Sql { get; set; }
    public bool AllowExecuteMultiSql { get; set; } = true;
    public string MultiSqlSeparator { get; set; }
}