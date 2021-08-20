using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AutoMapper;
using Sean.Core.DbRepository.Attributes;

namespace Example.Domain.Entities
{
    /// <summary>
    /// 签到明细日志表
    /// </summary>
    [Table("CheckInLog", Schema = "public")]
    public class CheckInLogEntity
    {
        /// <summary>
        /// 自增主键
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        /// <summary>
        /// 用户ID
        /// </summary>
        public long UserId { get; set; }
        /// <summary>
        /// 签到类型
        /// </summary>
        public int CheckInType { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
        /// <summary>
        /// IP地址
        /// </summary>
        public string IP { get; set; }
    }
}