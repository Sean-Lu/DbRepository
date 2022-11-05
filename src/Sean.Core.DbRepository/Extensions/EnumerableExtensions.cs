using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sean.Core.DbRepository.Extensions
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Paging Execution
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="pageSize">Page size</param>
        /// <param name="func"></param>
        /// <returns>Returns whether the execution succeeded</returns>
        public static bool PagingExecute<T>(this IEnumerable<T> list, int pageSize, Func<int, IEnumerable<T>, bool> func)
        {
            var pageIndex = 1;

            if (list == null || !list.Any()) return false;

            if (list.Count() <= pageSize)
            {
                return func(pageIndex, list);
            }

            do
            {
                var datas = list.Skip((pageIndex - 1) * pageSize).Take(pageSize);
                if (!datas.Any()) break;
                if (!func(pageIndex, datas)) return false;

                pageIndex++;
            } while (true);

            return true;
        }

        /// <summary>
        /// Asynchronous paging execution
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="pageSize">Page size</param>
        /// <param name="func"></param>
        /// <returns>Returns whether the execution succeeded</returns>
        public static async Task<bool> PagingExecuteAsync<T>(this IEnumerable<T> list, int pageSize, Func<int, IEnumerable<T>, Task<bool>> func)
        {
            var pageIndex = 1;

            if (list == null || !list.Any()) return false;

            if (list.Count() <= pageSize)
            {
                return await func(pageIndex, list);
            }

            do
            {
                var datas = list.Skip((pageIndex - 1) * pageSize).Take(pageSize);
                if (!datas.Any()) break;
                if (!await func(pageIndex, datas)) return false;

                pageIndex++;
            } while (true);

            return true;
        }
    }
}
