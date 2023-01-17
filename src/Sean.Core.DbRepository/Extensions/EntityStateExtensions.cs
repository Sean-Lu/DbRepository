using System.Collections.Generic;
using System.Linq;

namespace Sean.Core.DbRepository.Extensions
{
    public static class EntityStateExtensions
    {
        /// <summary>
        /// <see cref="EntityStateBase.EntityState"/>
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        public static void ResetEntityState<TEntity>(this TEntity entity) where TEntity : EntityStateBase
        {
            if (entity == null)
            {
                return;
            }

            entity.EntityState = EntityStateType.Unchanged;
        }

        /// <summary>
        /// <see cref="EntityStateBase.EntityState"/>
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entities"></param>
        public static void ResetEntityState<TEntity>(this IEnumerable<TEntity> entities) where TEntity : EntityStateBase
        {
            if (entities == null || !entities.Any())
            {
                return;
            }

            foreach (var entity in entities)
            {
                entity.ResetEntityState();
            }
        }
    }
}
