namespace Sean.Core.DbRepository;

public interface IEntityStateBase
{
    EntityStateType EntityState { get; set; }
}