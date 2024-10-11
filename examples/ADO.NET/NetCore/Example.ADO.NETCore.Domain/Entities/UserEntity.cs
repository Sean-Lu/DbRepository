using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Sean.Core.DbRepository;

namespace Example.ADO.NETCore.Domain.Entities;

/// <summary>
/// 用户表
/// </summary>
//[Table("User")]
[CodeFirst]
public class UserEntity : EntityBase
{
    /// <summary>
    /// 用户编码
    /// </summary>
    public virtual string Code { get; set; }
    /// <summary>
    /// 用户名称
    /// </summary>
    [MaxLength(50)]
    public virtual string Name { get; set; }
    /// <summary>
    /// 年龄
    /// </summary>
    public virtual int Age { get; set; }
    /// <summary>
    /// 性别
    /// </summary>
    public virtual SexType Sex { get; set; }
    /// <summary>
    /// 国家
    /// </summary>
    public virtual CountryType Country { get; set; }
    /// <summary>
    /// 电话号码
    /// </summary>
    [MaxLength(50)]
    public virtual string PhoneNumber { get; set; }
    /// <summary>
    /// 邮箱
    /// </summary>
    [MaxLength(50)]
    public virtual string Email { get; set; }
    /// <summary>
    /// 是否VIP用户
    /// </summary>
    [DefaultValue(false)]
    public virtual bool IsVip { get; set; }
    /// <summary>
    /// 是否黑名单用户
    /// </summary>
    [DefaultValue(false)]
    public virtual bool IsBlack { get; set; }
    /// <summary>
    /// 状态
    /// </summary>
    [DefaultValue(0)]
    public virtual int Status { get; set; }
    /// <summary>
    /// 备注
    /// </summary>
    [MaxLength(255)]
    public virtual string Remark { get; set; }
}