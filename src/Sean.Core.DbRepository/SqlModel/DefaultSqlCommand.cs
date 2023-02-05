namespace Sean.Core.DbRepository;

public class DefaultSqlCommand : ISqlCommand
{
    public string Sql { get; set; }
    public object Parameter { get; set; }
}

public class DefaultSqlCommand<T> : ISqlCommand<T>
{
    public string Sql { get; set; }
    public T Parameter { get; set; }
}