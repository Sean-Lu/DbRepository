using System;
using Sean.Core.DbRepository.Util;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sean.Core.DbRepository;

public class DefaultSqlCommand : ISqlCommand
{
    public DefaultSqlCommand()
    {
    }
    public DefaultSqlCommand(string sql, object parameter = null, bool useQuestionMarkParameter = false)
    {
        Sql = sql;
        Parameter = parameter;
        _useQuestionMarkParameter = useQuestionMarkParameter;
    }

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

    public void ConvertParameterToDictionary(bool removeUnusedParameter = true)
    {
        if (Parameter == null)
        {
            return;
        }

        switch (BindSqlParameterType)
        {
            case BindSqlParameterType.BindByName:
                {
                    var dicParameters = SqlParameterUtil.ConvertToDicParameter(Parameter);
                    if (removeUnusedParameter && !_unusedSqlParameterRemoved && !_useQuestionMarkParameter)
                    {
                        SqlParameterUtil.RemoveUnusedParameters(dicParameters, Sql);

                        _unusedSqlParameterRemoved = true;
                    }

                    Parameter = dicParameters;
                    break;
                }
            case BindSqlParameterType.BindByPosition:
                {
                    var dicParameters = SqlParameterUtil.ConvertToDicParameter(Parameter);

                    if (_useQuestionMarkParameter)
                    {
                        Parameter = dicParameters;
                        break;
                    }

                    var dic = new Dictionary<string, object>();
                    if (dicParameters != null)
                    {
                        var sortedSqlParameters = SqlParameterUtil.ParseSqlParameters(Sql);
                        if (sortedSqlParameters != null)
                        {
                            foreach (var keyValuePair in sortedSqlParameters)
                            {
                                var paraName = keyValuePair.Key;
                                if (!dicParameters.ContainsKey(paraName))
                                {
                                    throw new InvalidOperationException($"The sql parameter [{paraName}] does not exist.");
                                }

                                dic.Add(paraName, dicParameters[paraName]);
                            }
                        }
                    }

                    _unusedSqlParameterRemoved = true;
                    _sqlParameterSorted = true;

                    Parameter = dic;
                    break;
                }
            default:
                throw new NotSupportedException($"Unsupported {nameof(BindSqlParameterType)}: {BindSqlParameterType}");
        }
    }

    public void ConvertSqlToUseQuestionMarkParameter()
    {
        if (_useQuestionMarkParameter)
        {
            return;
        }

        Sql = SqlParameterUtil.UseQuestionMarkParameter(Sql);

        if (BindSqlParameterType != BindSqlParameterType.BindByPosition)
        {
            BindSqlParameterType = BindSqlParameterType.BindByPosition;
        }

        _useQuestionMarkParameter = true;
    }

    public void ConvertSqlToNonParameter()
    {
        if (_useQuestionMarkParameter)
        {
            var dicParameters = SqlParameterUtil.ConvertToDicParameter(Parameter);
            if (dicParameters != null && dicParameters.Any())
            {
                var index = 0;
                Sql = Regex.Replace(Sql, @"\?", match =>
                {
                    if (index >= dicParameters.Count)
                    {
                        throw new Exception("Not enough sql parameters passed.");
                    }

                    var sqlParameter = dicParameters.ElementAt(index++);
                    var convertResult = SqlBuilderUtil.ConvertToSqlString(sqlParameter.Value, out var convertible);
                    return convertible ? convertResult : throw new Exception($"The sql parameter [{sqlParameter.Key}] cannot be converted to a string value.");
                });
            }
        }
        else
        {
            var sortedSqlParameters = SqlParameterUtil.ParseSqlParameters(Sql);
            var dicParameters = SqlParameterUtil.ConvertToDicParameter(Parameter);
            if (sortedSqlParameters != null && sortedSqlParameters.Any() && dicParameters != null && dicParameters.Any())
            {
                foreach (var keyValuePair in sortedSqlParameters)
                {
                    var paraName = keyValuePair.Key;
                    if (dicParameters.ContainsKey(paraName))
                    {
                        var convertResult = SqlBuilderUtil.ConvertToSqlString(dicParameters[paraName], out var convertible);
                        if (convertible)
                        {
                            Sql = SqlParameterUtil.ReplaceParameter(Sql, paraName, convertResult);
                        }
                    }
                }
            }
        }
    }
}

public class DefaultSqlCommand<T> : DefaultSqlCommand, ISqlCommand<T>
{
    public new T Parameter { get; set; }
    public new OutputParameterOptions<T> OutputParameterOptions { get; set; }
}