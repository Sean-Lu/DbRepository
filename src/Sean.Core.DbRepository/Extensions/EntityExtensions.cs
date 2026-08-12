using System.Collections.Generic;

namespace Sean.Core.DbRepository.Extensions;

public static class EntityExtensions
{
    /// <summary>
    /// Reset <see cref="IEntityStateBase.EntityState"/>
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="entity"></param>
    public static void ResetEntityState<TEntity>(this TEntity entity) where TEntity : IEntityStateBase
    {
        if (entity == null)
        {
            return;
        }

        entity.EntityState = EntityStateType.Unchanged;
    }

    /// <summary>
    /// Reset <see cref="IEntityStateBase.EntityState"/>
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="entities"></param>
    public static void ResetEntityState<TEntity>(this IEnumerable<TEntity> entities) where TEntity : IEntityStateBase
    {
        if (entities == null)
        {
            return;
        }

        // 直接遍历即可兼容空集合，也避免一次性枚举器因 Any 检查而被提前消费。
        foreach (var entity in entities)
        {
            entity.ResetEntityState();
        }
    }
}
