using System;
using AutoMapper;
using Example.Domain.Entities;

namespace Example.Application.Dtos
{
    /// <summary>
    /// 签到明细日志表
    /// </summary>
    [AutoMap(typeof(CheckInLogEntity), ReverseMap = true)]
    public partial class CheckInLogDto
    {
        /// <summary>
        /// 自增主键
        /// </summary>
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
        /// 客户端IP地址
        /// </summary>
        public string IP { get; set; }
    }
}