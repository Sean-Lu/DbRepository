namespace Sean.Core.DbRepository;

public interface ISqlHaving<TEntity, out TResult>
{
    #region [HAVING]
    /// <summary>
    /// HAVING aggregate_function(column_name) operator value
    /// </summary>
    /// <param name="having">The [HAVING] keyword is not included.</param>
    /// <returns></returns>
    TResult Having(string having);
    #endregion
}