using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Example.EF.Core.ConsoleApp.Entities
{
    /// <summary>
    /// 测试表（仅供测试使用）
    /// </summary>
    [Table("TEST"/*, Schema = "DB2ADMIN"*/)]
    public class TestUpperEntity
    {
        /// <summary>
        /// 自增主键
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("ID")]
        public virtual long Id { get; set; }
        /// <summary>
        /// 用户id
        /// </summary>
        [Column("USER_ID")]
        public virtual long UserId { get; set; }
        /// <summary>
        /// 用户名称
        /// </summary>
        [Column("USER_NAME")]
        [MaxLength(50)]
        public virtual string? UserName { get; set; }
        /// <summary>
        /// 年龄
        /// </summary>
        [Column("AGE")]
        public virtual int Age { get; set; }
        /// <summary>
        /// 性别
        /// </summary>
        [Column("SEX")]
        public virtual SexType Sex { get; set; }
        /// <summary>
        /// 电话号码
        /// </summary>
        [Column("PHONE_NUMBER")]
        [MaxLength(50)]
        public virtual string? PhoneNumber { get; set; }
        /// <summary>
        /// 邮箱
        /// </summary>
        [Column("EMAIL")]
        [MaxLength(50)]
        public virtual string? Email { get; set; }
        /// <summary>
        /// 是否是VIP用户
        /// </summary>
        [Column("IS_VIP")]
        public virtual bool IsVip { get; set; }
        /// <summary>
        /// 是否是黑名单用户
        /// </summary>
        [Column("IS_BLACK")]
        public virtual bool IsBlack { get; set; }
        /// <summary>
        /// 国家
        /// </summary>
        [Column("COUNTRY")]
        public virtual CountryType Country { get; set; }
        /// <summary>
        /// 账户余额
        /// </summary>
        [Column("ACCOUNT_BALANCE")]
        public virtual decimal AccountBalance { get; set; }
        /// <summary>
        /// 账户余额
        /// </summary>
        [Column("ACCOUNT_BALANCE2")]
        public virtual decimal AccountBalance2 { get; set; }
        /// <summary>
        /// 状态
        /// </summary>
        [Column("STATUS")]
        public virtual int Status { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        [Column("REMARK")]
        [MaxLength(255)]
        public virtual string? Remark { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        [Column("CREATE_TIME")]
        public virtual DateTime CreateTime { get; set; }
        /// <summary>
        /// 更新时间
        /// </summary>
        [Column("UPDATE_TIME")]
        public virtual DateTime? UpdateTime { get; set; }
    }
}
