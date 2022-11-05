namespace Sean.Core.DbRepository;

public class DefaultSqlWithParameter : ISqlWithParameter
{
    public string Sql { get; set; }
    public object Parameter { get; set; }
}

public class DefaultSqlWithParameter<T> : ISqlWithParameter<T>
{
    public string Sql { get; set; }
    public T Parameter { get; set; }
}