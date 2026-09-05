using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sean.Core.DbRepository.Dapper;

namespace Sean.Core.DbRepository.Test;

/// <summary>
/// 泛型命令与非泛型执行入口必须共享参数和输出选项。
/// </summary>
[TestClass]
public class GenericSqlCommandCorrectnessTest
{
    [TestMethod]
    public void Parameter_AllReferenceTypesShareTheLatestValue()
    {
        var command = new DefaultSqlCommand<ParameterModel>();
        ISqlCommand<ParameterModel> generic = command;
        DefaultSqlCommand baseCommand = command;
        ISqlCommand untyped = command;

        command.Parameter = new ParameterModel { Value = 1 };
        Assert.AreSame(command.Parameter, untyped.Parameter);
        generic.Parameter = new ParameterModel { Value = 2 };
        Assert.AreSame(generic.Parameter, baseCommand.Parameter);
        baseCommand.Parameter = new ParameterModel { Value = 3 };
        Assert.AreSame(baseCommand.Parameter, command.Parameter);
        untyped.Parameter = new ParameterModel { Value = 4 };
        Assert.AreSame(untyped.Parameter, generic.Parameter);
        untyped.Parameter = null;
        Assert.IsNull(command.Parameter);
        Assert.IsNull(generic.Parameter);
    }

    [TestMethod]
    public void Parameter_ValueAndNullableTypesShareValuesAndPreserveDefaults()
    {
        var command = new DefaultSqlCommand<int>();
        Assert.AreEqual(0, command.Parameter);
        command.Parameter = 7;
        Assert.AreEqual(7, ((ISqlCommand)command).Parameter);
        ((ISqlCommand)command).Parameter = 9;
        Assert.AreEqual(9, command.Parameter);
        ((ISqlCommand)command).Parameter = null;
        Assert.AreEqual(0, command.Parameter);

        var nullable = new DefaultSqlCommand<int?>();
        Assert.IsNull(nullable.Parameter);
        nullable.Parameter = 11;
        Assert.AreEqual(11, ((ISqlCommand)nullable).Parameter);
        ((ISqlCommand)nullable).Parameter = 12;
        Assert.AreEqual(12, nullable.Parameter);
        ((ISqlCommand)nullable).Parameter = null;
        Assert.IsNull(nullable.Parameter);
    }

    [TestMethod]
    public void Parameter_IncompatibleUntypedValueIsPreservedAndTypedReadFails()
    {
        var command = new DefaultSqlCommand<ParameterModel> { Parameter = new ParameterModel() };
        var replacement = new Dictionary<string, object> { { "Value", 8 } };
        ((ISqlCommand)command).Parameter = replacement;

        Assert.AreSame(replacement, ((DefaultSqlCommand)command).Parameter);
        Assert.Throws<InvalidCastException>(() => _ = command.Parameter);
        Assert.Throws<InvalidCastException>(() => _ = ((ISqlCommand<ParameterModel>)command).Parameter);
        Assert.AreSame(replacement, ((ISqlCommand)command).Parameter);
    }

    [TestMethod]
    public void OutputOptions_AllReferenceTypesShareTheLatestValue()
    {
        var command = new DefaultSqlCommand<ParameterModel>();
        ISqlCommand<ParameterModel> generic = command;
        DefaultSqlCommand baseCommand = command;
        ISqlCommand untyped = command;

        command.OutputParameterOptions = new OutputParameterOptions<ParameterModel>();
        Assert.AreSame(command.OutputParameterOptions, untyped.OutputParameterOptions);
        generic.OutputParameterOptions = new OutputParameterOptions<ParameterModel>();
        Assert.AreSame(generic.OutputParameterOptions, baseCommand.OutputParameterOptions);
        baseCommand.OutputParameterOptions = new OutputParameterOptions<ParameterModel>();
        Assert.AreSame(baseCommand.OutputParameterOptions, command.OutputParameterOptions);
        untyped.OutputParameterOptions = new OutputParameterOptions<ParameterModel>();
        Assert.AreSame(untyped.OutputParameterOptions, generic.OutputParameterOptions);
        untyped.OutputParameterOptions = null;
        Assert.IsNull(command.OutputParameterOptions);
    }

    [TestMethod]
    public void OutputOptions_IncompatibleUntypedValueIsNotSilentlyHidden()
    {
        var command = new DefaultSqlCommand<ParameterModel>
        {
            OutputParameterOptions = new OutputParameterOptions<ParameterModel>()
        };
        var replacement = new OutputParameterOptions();
        ((ISqlCommand)command).OutputParameterOptions = replacement;

        Assert.Throws<InvalidCastException>(() => _ = command.OutputParameterOptions);
        Assert.AreSame(replacement, ((DefaultSqlCommand)command).OutputParameterOptions);
    }

    [TestMethod]
    public void OutputTarget_BaseAndGenericPropertiesShareTheLatestValue()
    {
        var target = new ParameterModel { Value = 1 };
        var options = new OutputParameterOptions<ParameterModel> { OutputTarget = target };
        OutputParameterOptions untyped = options;
        Assert.AreSame(target, untyped.OutputTarget);

        var replacement = new ParameterModel { Value = 2 };
        untyped.OutputTarget = replacement;
        Assert.AreSame(replacement, options.OutputTarget);
        untyped.OutputTarget = null;
        Assert.IsNull(options.OutputTarget);
    }

    [TestMethod]
    public void OutputTarget_IncompatibleUntypedValueIsNotSilentlyHidden()
    {
        var options = new OutputParameterOptions<ParameterModel> { OutputTarget = new ParameterModel() };
        var replacement = new object();
        ((OutputParameterOptions)options).OutputTarget = replacement;

        Assert.Throws<InvalidCastException>(() => _ = options.OutputTarget);
        Assert.AreSame(replacement, ((OutputParameterOptions)options).OutputTarget);
    }

    [TestMethod]
    public void ExecuteOutput_UsesGenericTargetThroughNonGenericCommand()
    {
        var original = new ParameterModel { Value = 1 };
        var replacement = new ParameterModel { Value = 2 };
        var options = new OutputParameterOptions<ParameterModel>
        {
            OutputTarget = original,
            OutputPropertyInfo = typeof(ParameterModel).GetProperty(nameof(ParameterModel.Value))
        };
        ISqlCommand command = new DefaultSqlCommand<ParameterModel> { OutputParameterOptions = options };
        Assert.IsNotNull(command.OutputParameterOptions);

        // 回写必须使用最后设置的目标，而不是泛型属性里过期的另一份值。
        ((OutputParameterOptions)options).OutputTarget = replacement;
        command.OutputParameterOptions.ExecuteOutput(name => name == "Value" ? 23L : throw new InvalidOperationException());
        Assert.AreEqual(1, original.Value);
        Assert.AreEqual(23, replacement.Value);
        Assert.AreSame(replacement, options.OutputTarget);
    }

    [TestMethod]
    public void ExecuteOutput_ValueTypeTargetUpdatesTheSharedBox()
    {
        var options = new OutputParameterOptions<StructTarget>
        {
            OutputTarget = new StructTarget { Value = 1 },
            OutputPropertyInfo = typeof(StructTarget).GetProperty(nameof(StructTarget.Value))
        };

        options.ExecuteOutput(_ => 13);
        Assert.AreEqual(13, options.OutputTarget.Value);
        Assert.AreEqual(13, ((StructTarget)((OutputParameterOptions)options).OutputTarget).Value);
    }

    [TestMethod]
    public void ConvertParameterToDictionaryByName_UsesTypedInputAndReplacesSharedState()
    {
        var command = CreateCommand(17);
        command.ConvertParameterToDictionaryByName(true);

        var parameters = (Dictionary<string, object>)((ISqlCommand)command).Parameter;
        Assert.AreEqual(1, parameters.Count);
        Assert.AreEqual(17, parameters["Value"]);
        // 转换 API 会用字典替换参数，之后不能再将它当成原来的 DTO 读取。
        Assert.Throws<InvalidCastException>(() => _ = command.Parameter);
    }

    [TestMethod]
    public void ConvertParameterToDictionaryByPosition_UsesTypedInputAndReplacesSharedState()
    {
        var command = CreateCommand(19);
        command.ConvertParameterToDictionaryByPosition(true);

        Assert.AreEqual("SELECT ?", command.Sql);
        var parameters = (Dictionary<string, object>)((ISqlCommand)command).Parameter;
        Assert.AreEqual(19, parameters["1"]);
        Assert.Throws<InvalidCastException>(() => _ = command.Parameter);
    }

    [TestMethod]
    public void ConvertSqlToNonParameter_UsesTypedInput()
    {
        var command = CreateCommand(29);
        command.ConvertSqlToNonParameter();
        Assert.AreEqual("SELECT 29", command.Sql);
        Assert.AreEqual(29, command.Parameter.Value);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task ExecuteScalar_GenericParametersReachSQLiteForSyncAndAsync(bool useDapper)
    {
        // 每次调用使用独立内存库，避免依赖或污染共享 test.db。
        var options = ConnectionStringOptions.Create("Data Source=:memory:;Version=3;", SQLiteFactory.Instance);
        IBaseRepository repository = useDapper ? new ParameterDapperRepository(options) : new ParameterRepository(options);

        Assert.AreEqual(31L, repository.ExecuteScalar<long>(CreateCommand(31)));
        Assert.AreEqual(37L, await repository.ExecuteScalarAsync<long>(CreateCommand(37)));
    }

    private static DefaultSqlCommand<ParameterModel> CreateCommand(int value)
    {
        return new DefaultSqlCommand<ParameterModel>
        {
            DbType = DatabaseType.SQLite,
            Sql = "SELECT @Value",
            Parameter = new ParameterModel { Value = value, Unused = 99 }
        };
    }

    public sealed class ParameterModel
    {
        public int Value { get; set; }
        public int Unused { get; set; }
    }

    public struct StructTarget
    {
        public int Value { get; set; }
    }

    private sealed class ParameterRepository : BaseRepository
    {
        public ParameterRepository(ConnectionStringOptions options) : base(options)
        {
        }
    }

    private sealed class ParameterDapperRepository : DapperBaseRepository
    {
        public ParameterDapperRepository(ConnectionStringOptions options) : base(options)
        {
        }
    }
}
