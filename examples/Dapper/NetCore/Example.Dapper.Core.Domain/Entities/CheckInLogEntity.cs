using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Example.Dapper.Core.Domain.Entities
{
    /// <summary>
    /// 签到明细日志表
    /// </summary>
    [Table("CheckInLog")]
    public class CheckInLogEntity
    {
        /// <summary>
        /// 自增主键
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public virtual long Id { get; set; }
        /// <summary>
        /// 用户ID
        /// </summary>
        public virtual long UserId { get; set; }
        /// <summary>
        /// 签到类型
        /// </summary>
        public virtual int CheckInType { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public virtual DateTime CreateTime { get; set; }
        /// <summary>
        /// IP地址
        /// </summary>
        [MaxLength(50)]
        public virtual string IP { get; set; }
    }
}