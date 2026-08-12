using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sean.Core.DbRepository.Test;

/// <summary>
/// 测试实体基类。
/// </summary>
public abstract class EntityBase : IEntityStateBase
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column(Order = 1)]
    [Description("主键")]
    public virtual long Id { get; set; }

    [Description("创建时间")]
    public virtual DateTime CreateTime { get; set; }

    [AutoUpdateCurrentTimestamp(true)]
    [Description("更新时间")]
    public virtual DateTime? UpdateTime { get; set; }

    [NotMapped]
    public virtual EntityStateType EntityState { get; set; }
}

/// <summary>
/// 测试表
/// </summary>
[Description("测试表")]
[CodeFirst]
[Index(nameof(PhoneNumber), IndexType = DbIndexType.Unique)]
[Index(nameof(Email), IndexType = DbIndexType.Unique)]
public class TestEntity : EntityBase
{
    [Description("用户主键")]
    public virtual long UserId { get; set; }

    [MaxLength(50)]
    [Description("用户名称")]
    public virtual string UserName { get; set; }

    [DefaultValue(18)]
    [Description("年龄")]
    public virtual int Age { get; set; }

    [Description("性别")]
    public virtual SexType Sex { get; set; }

    [MaxLength(50)]
    [Description("电话号码")]
    public virtual string PhoneNumber { get; set; }

    [MaxLength(50)]
    [DefaultValue("user@sample.com")]
    [Description("邮箱")]
    public virtual string Email { get; set; }

    [DefaultValue(true)]
    [Description("是否VIP用户")]
    public virtual bool IsVip { get; set; }

    [DefaultValue(false)]
    [Description("是否黑名单用户")]
    public virtual bool IsBlack { get; set; }

    [DefaultValue(CountryType.China)]
    [Description("国家")]
    public virtual CountryType Country { get; set; }

    [Numeric(18, 2)]
    [DefaultValue(999.98)]
    [Description("账户余额")]
    public virtual decimal AccountBalance { get; set; }

    [Numeric(18, 2)]
    [DefaultValue(9.98)]
    [Description("账户余额")]
    public virtual decimal AccountBalance2 { get; set; }

    [DefaultValue(0)]
    [Description("状态")]
    public virtual int Status { get; set; }

    [MaxLength(255)]
    [Description("备注")]
    public virtual string Remark { get; set; }

    [NotMapped]
    public virtual int? NullableTest { get; set; }

    [NotMapped]
    public virtual TestEntity NestedClassMemberTest { get; set; }
}

[Table("Test")]
[LeftJoin(typeof(UserEntity), nameof(UserId), nameof(UserEntity.Id), "u")]
public class Test2Entity : EntityBase
{
    public virtual long UserId { get; set; }

    [NotMapped]
    [LeftJoinField("u", nameof(UserEntity.Name))]
    public virtual string UserName { get; set; }

    [NotMapped]
    [LeftJoinField("u", nameof(UserEntity.Code))]
    public virtual string UserCode { get; set; }

    [NotMapped]
    [LeftJoinField("u", nameof(UserEntity.Email))]
    public virtual string UserEmail { get; set; }
}

[CodeFirst]
public class UserEntity : EntityBase
{
    public virtual string Code { get; set; }

    [MaxLength(50)]
    public virtual string Name { get; set; }
    public virtual int Age { get; set; }
    public virtual SexType Sex { get; set; }
    public virtual CountryType Country { get; set; }

    [MaxLength(50)]
    public virtual string PhoneNumber { get; set; }

    [MaxLength(50)]
    public virtual string Email { get; set; }

    [DefaultValue(false)]
    public virtual bool IsVip { get; set; }

    [DefaultValue(false)]
    public virtual bool IsBlack { get; set; }

    [DefaultValue(0)]
    public virtual int Status { get; set; }

    [MaxLength(255)]
    public virtual string Remark { get; set; }
}

public class CheckInLogEntity
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public virtual long Id { get; set; }
    public virtual long UserId { get; set; }
    public virtual int CheckInType { get; set; }
    public virtual DateTime CreateTime { get; set; }

    [MaxLength(50)]
    public virtual string IP { get; set; }
}

public enum SexType
{
    Unknown,
    Male,
    Female
}

public enum CountryType
{
    Unknown,
    China,
    America,
    England,
    Russia,
    Italy,
    Japan
}

public class TestDto
{
    public virtual long UserId { get; set; }
    public virtual string UserName { get; set; }
    public virtual string Email { get; set; }
    public virtual bool IsBlack { get; set; }
    public virtual string Remark { get; set; }
}
