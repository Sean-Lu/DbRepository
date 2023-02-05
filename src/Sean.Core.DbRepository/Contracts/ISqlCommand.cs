namespace Sean.Core.DbRepository;

public interface ISqlCommand
{
    string Sql { get; set; }
    object Parameter { get; set; }
}

public interface ISqlCommand<T>
{
    string Sql { get; set; }
    T Parameter { get; set; }
}