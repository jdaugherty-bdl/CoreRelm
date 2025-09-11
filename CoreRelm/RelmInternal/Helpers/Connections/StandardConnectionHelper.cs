using CoreRelm.RelmInternal.Helpers.Operations;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.Connections
{
    internal class StandardConnectionHelper
    {
        public static void StandardConnectionWrapper(Enum ConnectionType, Action<MySqlConnection, MySqlTransaction> ActionWrapper, Action<Exception, string> ExceptionHandler = null)
        {
            StandardConnectionWrapper(ConnectionType,
                (conn, transaction) =>
                {
                    ActionWrapper(conn, transaction);

                    return true;
                }, ExceptionHandler: ExceptionHandler);
        }

        //public static void StandardConnectionWrapper(Action<MySqlConnection, MySqlTransaction> ActionWrapper, Action<Exception, string> ExceptionHandler = null)
        //{
        //    StandardConnectionWrapper((conn, transaction) =>
        //    {
        //        ActionWrapper(conn, transaction);

        //        return true;
        //    }, ExceptionHandler: ExceptionHandler);
        //}

        ///// <summary>
        ///// Performs a supplied action as wrapped in an auto-generated connection & transaction
        ///// </summary>
        ///// <typeparam name="T">Return type of the action</typeparam>
        ///// <param name="ActionWrapper">A function that takes in a connection and transaction, and returns a type <typeparamref name="T"/></param>
        ///// <returns>An object with a type of <typeparamref name="T"/></returns>
        //public static T StandardConnectionWrapper<T>(Func<MySqlConnection, MySqlTransaction, T> ActionWrapper, Action<Exception, string> ExceptionHandler = null)
        //{
        //    return StandardConnectionWrapper((Enum)(object)0, ActionWrapper, ExceptionHandler: ExceptionHandler);
        //}

        /// <summary>
        /// Performs a supplied action as wrapped in an auto-generated connection & transaction
        /// </summary>
        /// <typeparam name="T">Return type of the action</typeparam>
        /// <param name="ConnectionType">The connection type to use for this wrapper</param>
        /// <param name="ActionWrapper">A function that takes in a connection and transaction, and returns a type <typeparamref name="T"/></param>
        /// <returns>An object with a type of <typeparamref name="T"/></returns>
        public static T StandardConnectionWrapper<T>(Enum ConnectionType, Func<MySqlConnection, MySqlTransaction, T> ActionWrapper, Action<Exception, string> ExceptionHandler = null)
        {
            using (var conn = ConnectionHelper.GetConnectionFromType(ConnectionType))
            {
                conn.Open();

                var transaction = conn.BeginTransaction();

                try
                {
                    var actionResult = ActionWrapper(conn, transaction);

                    if (transaction != null)
                    {
                        if ((conn?.State ?? ConnectionState.Broken) == ConnectionState.Open)
                            transaction?.Commit();
                        else
                            throw new IOException($"Can't commit, MySQL connection state is [{conn?.State}]");
                    }

                    return actionResult;
                }
                catch (Exception ex)
                {
                    // swallow the exception if one happens because sometimes the ActionWrapper lambda can commit/rollback the transaction on their own
                    try
                    {
                        transaction.Rollback();
                    }
                    catch { }

                    ExceptionHandler?.Invoke(ex, ex.Message);

                    throw;
                }
                finally
                {
                    if ((conn?.State ?? ConnectionState.Broken) == ConnectionState.Open)
                        conn.Close();
                }
            }
        }
    }
}
