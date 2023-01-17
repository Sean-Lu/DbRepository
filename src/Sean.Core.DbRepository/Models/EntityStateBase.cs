using System.ComponentModel.DataAnnotations.Schema;

namespace Sean.Core.DbRepository;

public abstract class EntityStateBase
{
    [NotMapped]
    public virtual EntityStateType EntityState { get; set; }
}