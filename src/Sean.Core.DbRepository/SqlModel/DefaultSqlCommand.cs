using System;
using Sean.Core.DbRepository.Util;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Sean.Core.DbRepository;

public class DefaultSqlCommand : ISqlCommand
{
    public string Sql { get; set; }
    public object Parameter { get; set; }
    public bool Master { get; set; } = true;
    public IDbTransaction Transaction { get; set; }
    public IDbConnection Connection { get; set; }
    public int? CommandTimeout { get; set; }
    public CommandType CommandType { get; set; } = CommandType.Text;
    public BindSqlParameterType BindSqlParameterType { get; set; } = BindSqlParameterType.BindByName;

    public void ConvertParameterToDictionary(bool removeUnusedParameter = true, bool useQuestionMarkParameter = false)
    {
        if (Parameter == null)
        {
            return;
        }

        if (useQuestionMarkParameter && BindSqlParameterType != BindSqlParameterType.BindByPosition)
        {
            BindSqlParameterType = BindSqlParameterType.BindByPosition;
        }

        switch (BindSqlParameterType)
        {
            case BindSqlParameterType.BindByName:
                {
                    var dicParameters = SqlParameterUtil.ConvertToDicParameter(Parameter);
                    if (removeUnusedParameter)
                    {
                        SqlParameterUtil.RemoveUnusedParameters(dicParameters, Sql);
                    }

                    Parameter = dicParameters;
                    break;
                }
            case BindSqlParameterType.BindByPosition:
                {
                    var dicParameters = SqlParameterUtil.ConvertToDicParameter(Parameter);

                    var dic = new Dictionary<string, object>();
                    if (dicParameters != null)
                    {
                        var orderSqlParameters = SqlParameterUtil.ParseSqlParameters(Sql);
                        if (orderSqlParameters != null)
                        {
                            foreach (var keyValuePair in orderSqlParameters)
                            {
                                var paraName = keyValuePair.Key;
                                if (!dicParameters.ContainsKey(paraName))
                                {
                                    continue;
                                }

                                dic.Add(paraName, dicParameters[paraName]);
                            }

                            if (useQuestionMarkParameter && orderSqlParameters.Any())
                            {
                                Sql = SqlParameterUtil.UseQuestionMarkParameter(Sql);
                            }
                        }
                    }

                    Parameter = dic;
                    break;
                }
            default:
                throw new NotSupportedException($"Unsupported {nameof(BindSqlParameterType)}: {BindSqlParameterType}");
        }
    }
}

public class DefaultSqlCommand<T> : DefaultSqlCommand, ISqlCommand<T>
{
    public new T Parameter { get; set; }
}