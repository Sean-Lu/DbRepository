﻿namespace Sean.Core.DbRepository;

public interface IWhereClause<TEntity> : IBaseSqlBuilder<IWhereClause<TEntity>>, ISqlWhere<TEntity, IWhereClause<TEntity>>
{
    IWhereClause<TEntity> SetParameter(object param);
    IWhereClause<TEntity> IsMultiTable(bool isMultiTable);
    IWhereClause<TEntity> IncludeKeyword(bool includeKeyword);
}