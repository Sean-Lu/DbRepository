using Sean.Core.DbRepository.Extensions;
using Sean.Core.DbRepository.Util;
using System.Linq.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sean.Core.DbRepository;

public class TableNameClauseSqlBuilder<TEntity> : BaseSqlBuilder<ITableNameClause<TEntity>>, ITableNameClause<TEntity>
{
    private string JoinTableSql => _joinTable.IsValueCreated && _joinTable.Value.Length > 0 ? _joinTable.Value.ToString() : string.Empty;

    private readonly Lazy<StringBuilder> _joinTable = new();

    private bool _includeKeyword;

    private TableNameClauseSqlBuilder(DatabaseType dbType) : base(dbType, typeof(TEntity).GetEntityInfo().TableName)
    {
    }
    private TableNameClauseSqlBuilder(ISqlAdapter sqlAdapter) : base(sqlAdapter, typeof(TEntity).GetEntityInfo().TableName)
    {
    }

    public static ITableNameClause<TEntity> Create()
    {
        return new TableNameClauseSqlBuilder<TEntity>(DatabaseType.Unknown);
    }
    public static ITableNameClause<TEntity> Create(ISqlAdapter sqlAdapter)
    {
        return new TableNameClauseSqlBuilder<TEntity>(sqlAdapter);
    }
    public static ITableNameClause<TEntity> Create(DatabaseType databaseType)
    {
        return new TableNameClauseSqlBuilder<TEntity>(databaseType);
    }

    #region [Join Table]
    public virtual ITableNameClause<TEntity> InnerJoin(string joinTableSql)
    {
        if (!string.IsNullOrWhiteSpace(joinTableSql))
        {
            _joinTable.Value.Append($" INNER JOIN {joinTableSql}");
        }
        return this;
    }
    public virtual ITableNameClause<TEntity> LeftJoin(string joinTableSql)
    {
        if (!string.IsNullOrWhiteSpace(joinTableSql))
        {
            _joinTable.Value.Append($" LEFT JOIN {joinTableSql}");
        }
        return this;
    }
    public virtual ITableNameClause<TEntity> RightJoin(string joinTableSql)
    {
        if (!string.IsNullOrWhiteSpace(joinTableSql))
        {
            _joinTable.Value.Append($" RIGHT JOIN {joinTableSql}");
        }
        return this;
    }
    public virtual ITableNameClause<TEntity> FullJoin(string joinTableSql)
    {
        if (!string.IsNullOrWhiteSpace(joinTableSql))
        {
            _joinTable.Value.Append($" FULL JOIN {joinTableSql}");
        }
        return this;
    }

    public virtual ITableNameClause<TEntity> InnerJoin<TEntity2>(Expression<Func<TEntity, object>> leftTableFieldExpression, Expression<Func<TEntity2, object>> rightTableFieldExpression, string rightTableAliasName = null)
    {
        return InnerJoin(SqlBuilderUtil.GetJoinSql(SqlAdapter, leftTableFieldExpression, rightTableFieldExpression, SqlAdapter.AliasName, rightTableAliasName));
    }
    public virtual ITableNameClause<TEntity> LeftJoin<TEntity2>(Expression<Func<TEntity, object>> leftTableFieldExpression, Expression<Func<TEntity2, object>> rightTableFieldExpression, string rightTableAliasName = null)
    {
        return LeftJoin(SqlBuilderUtil.GetJoinSql(SqlAdapter, leftTableFieldExpression, rightTableFieldExpression, SqlAdapter.AliasName, rightTableAliasName));
    }
    public virtual ITableNameClause<TEntity> RightJoin<TEntity2>(Expression<Func<TEntity, object>> leftTableFieldExpression, Expression<Func<TEntity2, object>> rightTableFieldExpression, string rightTableAliasName = null)
    {
        return RightJoin(SqlBuilderUtil.GetJoinSql(SqlAdapter, leftTableFieldExpression, rightTableFieldExpression, SqlAdapter.AliasName, rightTableAliasName));
    }
    public virtual ITableNameClause<TEntity> FullJoin<TEntity2>(Expression<Func<TEntity, object>> leftTableFieldExpression, Expression<Func<TEntity2, object>> rightTableFieldExpression, string rightTableAliasName = null)
    {
        return FullJoin(SqlBuilderUtil.GetJoinSql(SqlAdapter, leftTableFieldExpression, rightTableFieldExpression, SqlAdapter.AliasName, rightTableAliasName));
    }

    public virtual ITableNameClause<TEntity> InnerJoin<TEntity2, TEntity3>(Expression<Func<TEntity2, object>> leftTableFieldExpression, Expression<Func<TEntity3, object>> rightTableFieldExpression, string leftTableAliasName = null, string rightTableAliasName = null)
    {
        return InnerJoin(SqlBuilderUtil.GetJoinSql(SqlAdapter, leftTableFieldExpression, rightTableFieldExpression, leftTableAliasName, rightTableAliasName));
    }
    public virtual ITableNameClause<TEntity> LeftJoin<TEntity2, TEntity3>(Expression<Func<TEntity2, object>> leftTableFieldExpression, Expression<Func<TEntity3, object>> rightTableFieldExpression, string leftTableAliasName = null, string rightTableAliasName = null)
    {
        return LeftJoin(SqlBuilderUtil.GetJoinSql(SqlAdapter, leftTableFieldExpression, rightTableFieldExpression, leftTableAliasName, rightTableAliasName));
    }
    public virtual ITableNameClause<TEntity> RightJoin<TEntity2, TEntity3>(Expression<Func<TEntity2, object>> leftTableFieldExpression, Expression<Func<TEntity3, object>> rightTableFieldExpression, string leftTableAliasName = null, string rightTableAliasName = null)
    {
        return RightJoin(SqlBuilderUtil.GetJoinSql(SqlAdapter, leftTableFieldExpression, rightTableFieldExpression, leftTableAliasName, rightTableAliasName));
    }
    public virtual ITableNameClause<TEntity> FullJoin<TEntity2, TEntity3>(Expression<Func<TEntity2, object>> leftTableFieldExpression, Expression<Func<TEntity3, object>> rightTableFieldExpression, string leftTableAliasName = null, string rightTableAliasName = null)
    {
        return FullJoin(SqlBuilderUtil.GetJoinSql(SqlAdapter, leftTableFieldExpression, rightTableFieldExpression, leftTableAliasName, rightTableAliasName));
    }
    #endregion

    public virtual ITableNameClause<TEntity> IncludeKeyword(bool includeKeyword)
    {
        _includeKeyword = includeKeyword;
        return this;
    }

    protected override ISqlCommand BuildSqlCommand()
    {
        var sb = new StringBuilder();
        sb.Append($"{(_includeKeyword ? " FROM " : string.Empty)}{SqlAdapter.FormatTableName(TableName)}{JoinTableSql}");

        var sql = new DefaultSqlCommand(SqlAdapter.DbType)
        {
            Sql = sb.ToString()
        };
        return sql;
    }
}