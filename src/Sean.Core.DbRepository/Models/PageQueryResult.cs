using System.Collections.Generic;

namespace Sean.Core.DbRepository;

public class PageQueryResult<T>
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<T> List { get; set; }
}