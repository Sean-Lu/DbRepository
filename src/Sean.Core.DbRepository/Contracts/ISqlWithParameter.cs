namespace Sean.Core.DbRepository;

public interface ISqlWithParameter : ISql
{
    object Parameter { get; set; }
}

public interface ISqlWithParameter<T> : ISql
{
    T Parameter { get; set; }
}