namespace Sean.Core.DbRepository.Extensions;

public static class RepositoryExtensions
{
    #region SqlBuilder
    /// <summary>
    /// Create an instance of <see cref="IReplaceable{TEntity}"/>.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="repository"></param>
    /// <returns></returns>
    public static IReplaceable<TEntity> CreateReplaceableBuilder<TEntity>(this IBaseRepository repository)
    {
        return ReplaceableSqlBuilder<TEntity>.Create(repository.DbType).SetTableName(repository.TableName());
    }
    /// <summary>
    /// Create an instance of <see cref="IReplaceable{TEntity}"/>.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="repository"></param>
    /// <returns></returns>
    public static IReplaceable<TEntity> CreateReplaceableBuilder<TEntity>(this IBaseRepository<TEntity> repository) where TEntity : class
    {
        return ReplaceableSqlBuilder<TEntity>.Create(repository.DbType).SetTableName(repository.TableName());
    }

    /// <summary>
    /// Create an instance of <see cref="IInsertable{TEntity}"/>.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="repository"></param>
    /// <returns></returns>
    public static IInsertable<TEntity> CreateInsertableBuilder<TEntity>(this IBaseRepository repository)
    {
        return InsertableSqlBuilder<TEntity>.Create(repository.DbType).SetTableName(repository.TableName());
    }
    /// <summary>
    /// Create an instance of <see cref="IInsertable{TEntity}"/>.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="repository"></param>
    /// <returns></returns>
    public static IInsertable<TEntity> CreateInsertableBuilder<TEntity>(this IBaseRepository<TEntity> repository) where TEntity : class
    {
        return InsertableSqlBuilder<TEntity>.Create(repository.DbType).SetTableName(repository.TableName());
    }

    /// <summary>
    /// Create an instance of <see cref="IDeleteable{TEntity}"/>.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="repository"></param>
    /// <returns></returns>
    public static IDeleteable<TEntity> CreateDeleteableBuilder<TEntity>(this IBaseRepository repository)
    {
        return DeleteableSqlBuilder<TEntity>.Create(repository.DbType).SetTableName(repository.TableName());
    }
    /// <summary>
    /// Create an instance of <see cref="IDeleteable{TEntity}"/>.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="repository"></param>
    /// <returns></returns>
    public static IDeleteable<TEntity> CreateDeleteableBuilder<TEntity>(this IBaseRepository<TEntity> repository) where TEntity : class
    {
        return DeleteableSqlBuilder<TEntity>.Create(repository.DbType).SetTableName(repository.TableName());
    }

    /// <summary>
    /// Create an instance of <see cref="IUpdateable{TEntity}"/>.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="repository"></param>
    /// <returns></returns>
    public static IUpdateable<TEntity> CreateUpdateableBuilder<TEntity>(this IBaseRepository repository)
    {
        return UpdateableSqlBuilder<TEntity>.Create(repository.DbType).SetTableName(repository.TableName());
    }
    /// <summary>
    /// Create an instance of <see cref="IUpdateable{TEntity}"/>.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="repository"></param>
    /// <returns></returns>
    public static IUpdateable<TEntity> CreateUpdateableBuilder<TEntity>(this IBaseRepository<TEntity> repository) where TEntity : class
    {
        return UpdateableSqlBuilder<TEntity>.Create(repository.DbType).SetTableName(repository.TableName());
    }

    /// <summary>
    /// Create an instance of <see cref="IQueryable{TEntity}"/>.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="repository"></param>
    /// <returns></returns>
    public static IQueryable<TEntity> CreateQueryableBuilder<TEntity>(this IBaseRepository repository)
    {
        return QueryableSqlBuilder<TEntity>.Create(repository.DbType).SetTableName(repository.TableName());
    }
    /// <summary>
    /// Create an instance of <see cref="IQueryable{TEntity}"/>.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="repository"></param>
    /// <returns></returns>
    public static IQueryable<TEntity> CreateQueryableBuilder<TEntity>(this IBaseRepository<TEntity> repository) where TEntity : class
    {
        return QueryableSqlBuilder<TEntity>.Create(repository.DbType).SetTableName(repository.TableName());
    }

    /// <summary>
    /// Create an instance of <see cref="ICountable{TEntity}"/>.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="repository"></param>
    /// <returns></returns>
    public static ICountable<TEntity> CreateCountableBuilder<TEntity>(this IBaseRepository repository)
    {
        return CountableSqlBuilder<TEntity>.Create(repository.DbType).SetTableName(repository.TableName());
    }
    /// <summary>
    /// Create an instance of <see cref="ICountable{TEntity}"/>.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="repository"></param>
    /// <returns></returns>
    public static ICountable<TEntity> CreateCountableBuilder<TEntity>(this IBaseRepository<TEntity> repository) where TEntity : class
    {
        return CountableSqlBuilder<TEntity>.Create(repository.DbType).SetTableName(repository.TableName());
    }

    /// <summary>
    /// Create an instance of <see cref="ITableNameClause{TEntity}"/>.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="repository"></param>
    /// <returns></returns>
    public static ITableNameClause<TEntity> CreateTableNameClauseBuilder<TEntity>(this IBaseRepository repository)
    {
        return TableNameClauseSqlBuilder<TEntity>.Create(repository.DbType).SetTableName(repository.TableName());
    }
    /// <summary>
    /// Create an instance of <see cref="ITableNameClause{TEntity}"/>.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="repository"></param>
    /// <returns></returns>
    public static ITableNameClause<TEntity> CreateTableNameClauseBuilder<TEntity>(this IBaseRepository<TEntity> repository) where TEntity : class
    {
        return TableNameClauseSqlBuilder<TEntity>.Create(repository.DbType).SetTableName(repository.TableName());
    }

    /// <summary>
    /// Create an instance of <see cref="IWhereClause{TEntity}"/>.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="repository"></param>
    /// <returns></returns>
    public static IWhereClause<TEntity> CreateWhereClauseBuilder<TEntity>(this IBaseRepository repository)
    {
        return WhereClauseSqlBuilder<TEntity>.Create(repository.DbType).SetTableName(repository.TableName());
    }
    /// <summary>
    /// Create an instance of <see cref="IWhereClause{TEntity}"/>.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="repository"></param>
    /// <returns></returns>
    public static IWhereClause<TEntity> CreateWhereClauseBuilder<TEntity>(this IBaseRepository<TEntity> repository) where TEntity : class
    {
        return WhereClauseSqlBuilder<TEntity>.Create(repository.DbType).SetTableName(repository.TableName());
    }

    /// <summary>
    /// Create an instance of <see cref="IOrderByClause{TEntity}"/>.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="repository"></param>
    /// <returns></returns>
    public static IOrderByClause<TEntity> CreateOrderByClauseBuilder<TEntity>(this IBaseRepository repository)
    {
        return OrderByClauseSqlBuilder<TEntity>.Create(repository.DbType).SetTableName(repository.TableName());
    }
    /// <summary>
    /// Create an instance of <see cref="IOrderByClause{TEntity}"/>.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="repository"></param>
    /// <returns></returns>
    public static IOrderByClause<TEntity> CreateOrderByClauseBuilder<TEntity>(this IBaseRepository<TEntity> repository) where TEntity : class
    {
        return OrderByClauseSqlBuilder<TEntity>.Create(repository.DbType).SetTableName(repository.TableName());
    }
    #endregion
}