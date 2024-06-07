using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Example.EF.Core.ConsoleApp.Entities;

public abstract class EntityBase
{
    /// <summary>
    /// 主键
    /// </summary>
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column(Order = 1)]
    public virtual long Id { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public virtual DateTime CreateTime { get; set; }
    /// <summary>
    /// 更新时间
    /// </summary>
    public virtual DateTime? UpdateTime { get; set; }
}