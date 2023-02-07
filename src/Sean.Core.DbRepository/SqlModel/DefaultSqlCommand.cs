using System.Data;

namespace Sean.Core.DbRepository;

public class DefaultSqlCommand : ISqlCommand
{
    public string Sql { get; set; }
    public object Parameter { get; set; }
    public bool Master { get; set; } = true;
    public IDbTransaction Transaction { get; set; }
    public int? CommandTimeout { get; set; }
    public CommandType CommandType { get; set; } = CommandType.Text;
}

public class DefaultSqlCommand<T> : DefaultSqlCommand, ISqlCommand<T>
{
    public new T Parameter { get; set; }
}