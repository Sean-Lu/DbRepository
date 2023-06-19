using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Sean.Core.DbRepository;

namespace Example.Dapper.Core.Domain.Entities
{
    /// <summary>
    /// 测试表（仅供测试使用）
    /// </summary>
    [Table("Test")]
    public class TestEntity : EntityStateBase
    {
        /// <summary>
        /// 自增主键
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Sequence("SQ_Test")]
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
        public virtual string UserName { get; set; }
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
        public virtual string PhoneNumber { get; set; }
        /// <summary>
        /// 邮箱
        /// </summary>
        public virtual string Email { get; set; }
        /// <summary>
        /// 是否是VIP用户
        /// </summary>
        public virtual bool IsVip { get; set; }
        /// <summary>
        /// 是否是黑名单用户
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
        public virtual string Remark { get; set; }
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
        [NotMapped]
        public virtual TestEntity NestedClassMemberTest { get; set; }
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
