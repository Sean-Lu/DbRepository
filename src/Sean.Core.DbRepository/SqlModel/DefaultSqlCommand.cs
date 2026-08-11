using System;
using Sean.Core.DbRepository.Util;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace Sean.Core.DbRepository;

public class DefaultSqlCommand : ISqlCommand
{
    public DefaultSqlCommand()
    {
    }
    public DefaultSqlCommand(DatabaseType dbType)
    {
        DbType = dbType;
    }
    public DefaultSqlCommand(string sql, object parameter = null, bool useQuestionMarkParameter = false)
    {
        Sql = sql;
        Parameter = parameter;
        _useQuestionMarkParameter = useQuestionMarkParameter;
    }

    public DatabaseType DbType { get; set; }

    public string Sql { get; set; }
    public object Parameter { get; set; }
    public bool Master { get; set; } = true;
    public IDbTransaction Transaction { get; set; }
    public IDbConnection Connection { get; set; }
    public int? CommandTimeout { get; set; }
    public CommandType CommandType { get; set; } = CommandType.Text;

    public OutputParameterOptions OutputParameterOptions { get; set; }

    public BindSqlParameterType BindSqlParameterType { get; set; } = BindSqlParameterType.BindByName;

    public bool UnusedSqlParameterRemoved => _unusedSqlParameterRemoved;
    public bool SqlParameterSorted => _sqlParameterSorted;
    public bool UseQuestionMarkParameter => _useQuestionMarkParameter;

    private bool _unusedSqlParameterRemoved;
    private bool _sqlParameterSorted;
    private bool _useQuestionMarkParameter;

    public void ConvertParameterToDictionaryByName(bool removeUnusedParameter)
    {
        BindSqlParameterType = BindSqlParameterType.BindByName;

        if (Parameter == null)
        {
            return;
        }

        var dicParameters = SqlParameterUtil.ConvertToDicParameter(Parameter);
        if (removeUnusedParameter && !_unusedSqlParameterRemoved && !_useQuestionMarkParameter)
        {
            SqlParameterUtil.RemoveUnusedParameters(dicParameters, Sql, DbType);

            _unusedSqlParameterRemoved = true;
        }

        Parameter = dicParameters;
    }

    public void ConvertParameterToDictionaryByPosition(bool useQuestionMarkParameter)
    {
        BindSqlParameterType = BindSqlParameterType.BindByPosition;

        if (Parameter == null)
        {
            return;
        }

        var dicParameters = SqlParameterUtil.ConvertToDicParameter(Parameter);

        if (_useQuestionMarkParameter)
        {
            Parameter = dicParameters;
            return;
        }

        var dic = new Dictionary<string, object>();
        if (dicParameters != null)
        {
            var sortedSqlParameterNames = useQuestionMarkParameter
                ? SqlParameterUtil.ParseSqlParameterNamesInOrder(Sql, DbType)
                : SqlParameterUtil.ParseSqlParameters(Sql, DbType)
                    .OrderBy(parameter => parameter.Value)
                    .Select(parameter => parameter.Key);
            if (sortedSqlParameterNames != null)
            {
                var paramNumber = 0;
                foreach (var paraName in sortedSqlParameterNames)
                {
                    paramNumber++;
                    if (!dicParameters.ContainsKey(paraName))
                    {
                        throw new InvalidOperationException($"The sql parameter [{paraName}] does not exist.");
                    }
                    dic.Add(!useQuestionMarkParameter ? paraName : paramNumber.ToString(), dicParameters[paraName]);
                }
            }
        }

        _unusedSqlParameterRemoved = true;
        _sqlParameterSorted = true;

        Parameter = dic;

        if (useQuestionMarkParameter)
        {
            ConvertSqlToUseQuestionMarkParameter();
        }
    }

    public void ConvertSqlToUseQuestionMarkParameter()
    {
        if (_useQuestionMarkParameter)
        {
            return;
        }

        Sql = SqlParameterUtil.UseQuestionMarkParameter(Sql, DbType);

        BindSqlParameterType = BindSqlParameterType.BindByPosition;

        _useQuestionMarkParameter = true;
    }

    public void ConvertSqlToNonParameter()
    {
        if (_useQuestionMarkParameter)
        {
            var dicParameters = SqlParameterUtil.ConvertToDicParameter(Parameter);
            if (dicParameters != null && dicParameters.Any())
            {
                var positionalParameters = dicParameters.ToList();
                // ConvertParameterToDictionaryByPosition 生成的键为从 1 开始的序号，
                // 必须按数值排序，不能依赖 Dictionary 在不同目标框架下的枚举顺序。
                if (positionalParameters.All(parameter => int.TryParse(parameter.Key, NumberStyles.None,
                        CultureInfo.InvariantCulture, out _)))
                {
                    positionalParameters = positionalParameters
                        .OrderBy(parameter => int.Parse(parameter.Key, CultureInfo.InvariantCulture))
                        .ToList();
                }

                Sql = SqlParameterUtil.ReplaceQuestionMarkParameters(Sql, index =>
                {
                    if (index >= positionalParameters.Count)
                    {
                        throw new Exception("Not enough sql parameters passed.");
                    }

                    var sqlParameter = positionalParameters[index];
                    var convertResult = SqlBuilderUtil.ConvertToSqlString(DbType, sqlParameter.Value, out var convertible);
                    return convertible ? convertResult : throw new Exception($"The sql parameter [{sqlParameter.Key}] cannot be converted to a string value.");
                }, DbType);
            }
        }
        else
        {
            var dicParameters = SqlParameterUtil.ConvertToDicParameter(Parameter);
            if (dicParameters != null && dicParameters.Any())
            {
                Sql = SqlParameterUtil.ReplaceSqlParameters(Sql, paraName =>
                {
                    if (!dicParameters.ContainsKey(paraName))
                    {
                        return null;
                    }

                    var convertResult = SqlBuilderUtil.ConvertToSqlString(DbType, dicParameters[paraName], out var convertible);
                    return convertible ? convertResult : null;
                }, DbType);
            }
        }
    }
}

public class DefaultSqlCommand<T> : DefaultSqlCommand, ISqlCommand<T>
{
    public new T Parameter { get; set; }
    public new OutputParameterOptions<T> OutputParameterOptions { get; set; }
}
