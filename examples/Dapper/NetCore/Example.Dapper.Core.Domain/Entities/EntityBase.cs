using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Sean.Core.DbRepository;

namespace Example.Dapper.Core.Domain.Entities;

//[NamingConvention(NamingConvention.SnakeCase)]
public abstract class EntityBase : IEntityStateBase
{
    /// <summary>
    /// 主键
    /// </summary>
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column(Order = 1)]
    //[Sequence("SQ_Test")]
    public virtual long Id { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public virtual DateTime CreateTime { get; set; }
    /// <summary>
    /// 更新时间
    /// </summary>
    [AutoUpdateCurrentTimestamp(true)]
    public virtual DateTime? UpdateTime { get; set; }

    [NotMapped]
    public virtual EntityStateType EntityState { get; set; }
}