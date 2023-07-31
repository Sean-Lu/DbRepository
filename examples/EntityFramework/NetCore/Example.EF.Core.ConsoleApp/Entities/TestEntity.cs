using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Example.EF.Core.ConsoleApp.Entities
{
    /// <summary>
    /// 测试表
    /// </summary>
    [Table("Test")]
    public class TestEntity
    {
        /// <summary>
        /// 自增主键
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public virtual long Id { get; set; }
        /// <summary>
        /// 用户id
        /// </summary>
        [Column("UserId")]
        public virtual long UserId { get; set; }
        /// <summary>
        /// 用户名称
        /// </summary>
        [Column("UserName")]
        [MaxLength(50)]
        public virtual string? UserName { get; set; }
        /// <summary>
        /// 年龄
        /// </summary>
        public virtual int Age { get; set; }
        /// <summary>
        /// 性别
        /// </summary>
        public virtual SexType Sex { get; set; }
        /// <summary>
        /// 电话号码
        /// </summary>
        [MaxLength(50)]
        public virtual string? PhoneNumber { get; set; }
        /// <summary>
        /// 邮箱
        /// </summary>
        [MaxLength(50)]
        public virtual string? Email { get; set; }
        /// <summary>
        /// 是否VIP用户
        /// </summary>
        public virtual bool IsVip { get; set; }
        /// <summary>
        /// 是否黑名单用户
        /// </summary>
        public virtual bool IsBlack { get; set; }
        /// <summary>
        /// 国家
        /// </summary>
        public virtual CountryType Country { get; set; }
        /// <summary>
        /// 账户余额
        /// </summary>
        public virtual decimal AccountBalance { get; set; }
        /// <summary>
        /// 账户余额
        /// </summary>
        public virtual decimal AccountBalance2 { get; set; }
        /// <summary>
        /// 状态
        /// </summary>
        public virtual int Status { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        [MaxLength(255)]
        public virtual string? Remark { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public virtual DateTime CreateTime { get; set; }
        /// <summary>
        /// 更新时间
        /// </summary>
        public virtual DateTime? UpdateTime { get; set; }

        #region 忽略字段
        [NotMapped]
        public virtual int? NullableTest { get; set; }
        #endregion
    }

    public enum SexType
    {
        Unknown = 0,
        Male = 1,
        Female = 2
    }

    public enum CountryType
    {
        Unknown = 0,
        China,
        America,
        England,
        Russia,
        Italy,
        Japan
    }
}
