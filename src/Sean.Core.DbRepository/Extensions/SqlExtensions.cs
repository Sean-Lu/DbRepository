using Sean.Core.DbRepository.Util;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Sean.Core.DbRepository.Extensions
{
    public static class SqlExtensions
    {
        #region Synchronous method
        public static int Execute(this ISqlWithParameter sql, DbFactory dbFactory, bool master = true, IDbTransaction transaction = null)
        {
            if (transaction != null)
            {
                return dbFactory.ExecuteNonQuery(transaction, sql.Sql, SqlParameterUtil.ConvertToDbParameters(sql.Parameter, () => dbFactory.ProviderFactory.CreateParameter()));
            }

            return dbFactory.ExecuteNonQuery(sql.Sql, SqlParameterUtil.ConvertToDbParameters(sql.Parameter, () => dbFactory.ProviderFactory.CreateParameter()), master: master);
        }

        public static IEnumerable<T> Query<T>(this ISqlWithParameter sql, DbFactory dbFactory, bool master = true, IDbTransaction transaction = null)
        {
            if (transaction != null)
            {
                return dbFactory.Query<T>(transaction, sql.Sql, SqlParameterUtil.ConvertToDbParameters(sql.Parameter, () => dbFactory.ProviderFactory.CreateParameter()));
            }

            return dbFactory.Query<T>(sql.Sql, SqlParameterUtil.ConvertToDbParameters(sql.Parameter, () => dbFactory.ProviderFactory.CreateParameter()), master: master);
        }

        public static T Get<T>(this ISqlWithParameter sql, DbFactory dbFactory, bool master = true, IDbTransaction transaction = null)
        {
            if (transaction != null)
            {
                return dbFactory.Get<T>(transaction, sql.Sql, SqlParameterUtil.ConvertToDbParameters(sql.Parameter, () => dbFactory.ProviderFactory.CreateParameter()));
            }

            return dbFactory.Get<T>(sql.Sql, SqlParameterUtil.ConvertToDbParameters(sql.Parameter, () => dbFactory.ProviderFactory.CreateParameter()), master: master);
        }

        public static T ExecuteScalar<T>(this ISqlWithParameter sql, DbFactory dbFactory, bool master = true, IDbTransaction transaction = null)
        {
            if (transaction != null)
            {
                return dbFactory.ExecuteScalar<T>(transaction, sql.Sql, SqlParameterUtil.ConvertToDbParameters(sql.Parameter, () => dbFactory.ProviderFactory.CreateParameter()));
            }

            return dbFactory.ExecuteScalar<T>(sql.Sql, SqlParameterUtil.ConvertToDbParameters(sql.Parameter, () => dbFactory.ProviderFactory.CreateParameter()), master: master);
        }

        public static object ExecuteScalar(this ISqlWithParameter sql, DbFactory dbFactory, bool master = true, IDbTransaction transaction = null)
        {
            if (transaction != null)
            {
                return dbFactory.ExecuteScalar(transaction, sql.Sql, SqlParameterUtil.ConvertToDbParameters(sql.Parameter, () => dbFactory.ProviderFactory.CreateParameter()));
            }

            return dbFactory.ExecuteScalar(sql.Sql, SqlParameterUtil.ConvertToDbParameters(sql.Parameter, () => dbFactory.ProviderFactory.CreateParameter()), master: master);
        }
        #endregion

        #region Asynchronous method
#if NETSTANDARD || NET45_OR_GREATER
        public static async Task<int> ExecuteAsync(this ISqlWithParameter sql, DbFactory dbFactory, bool master = true, IDbTransaction transaction = null)
        {
            if (transaction != null)
            {
                return await dbFactory.ExecuteNonQueryAsync(transaction, sql.Sql, SqlParameterUtil.ConvertToDbParameters(sql.Parameter, () => dbFactory.ProviderFactory.CreateParameter()));
            }
            return await dbFactory.ExecuteNonQueryAsync(sql.Sql, SqlParameterUtil.ConvertToDbParameters(sql.Parameter, () => dbFactory.ProviderFactory.CreateParameter()), master: master);
        }

        public static async Task<IEnumerable<T>> QueryAsync<T>(this ISqlWithParameter sql, DbFactory dbFactory, bool master = true, IDbTransaction transaction = null)
        {
            if (transaction != null)
            {
                return await dbFactory.QueryAsync<T>(transaction, sql.Sql, SqlParameterUtil.ConvertToDbParameters(sql.Parameter, () => dbFactory.ProviderFactory.CreateParameter()));
            }

            return await dbFactory.QueryAsync<T>(sql.Sql, SqlParameterUtil.ConvertToDbParameters(sql.Parameter, () => dbFactory.ProviderFactory.CreateParameter()), master: master);
        }

        public static async Task<T> GetAsync<T>(this ISqlWithParameter sql, DbFactory dbFactory, bool master = true, IDbTransaction transaction = null)
        {
            if (transaction != null)
            {
                return await dbFactory.GetAsync<T>(transaction, sql.Sql, SqlParameterUtil.ConvertToDbParameters(sql.Parameter, () => dbFactory.ProviderFactory.CreateParameter()));
            }

            return await dbFactory.GetAsync<T>(sql.Sql, SqlParameterUtil.ConvertToDbParameters(sql.Parameter, () => dbFactory.ProviderFactory.CreateParameter()), master: master);
        }

        public static async Task<T> ExecuteScalarAsync<T>(this ISqlWithParameter sql, DbFactory dbFactory, bool master = true, IDbTransaction transaction = null)
        {
            if (transaction != null)
            {
                return await dbFactory.ExecuteScalarAsync<T>(transaction, sql.Sql, SqlParameterUtil.ConvertToDbParameters(sql.Parameter, () => dbFactory.ProviderFactory.CreateParameter()));
            }

            return await dbFactory.ExecuteScalarAsync<T>(sql.Sql, SqlParameterUtil.ConvertToDbParameters(sql.Parameter, () => dbFactory.ProviderFactory.CreateParameter()), master: master);
        }

        public static async Task<object> ExecuteScalarAsync(this ISqlWithParameter sql, DbFactory dbFactory, bool master = true, IDbTransaction transaction = null)
        {
            if (transaction != null)
            {
                return await dbFactory.ExecuteScalarAsync(transaction, sql.Sql, SqlParameterUtil.ConvertToDbParameters(sql.Parameter, () => dbFactory.ProviderFactory.CreateParameter()));
            }

            return await dbFactory.ExecuteScalarAsync(sql.Sql, SqlParameterUtil.ConvertToDbParameters(sql.Parameter, () => dbFactory.ProviderFactory.CreateParameter()), master: master);
        }
#endif
        #endregion
    }
}
