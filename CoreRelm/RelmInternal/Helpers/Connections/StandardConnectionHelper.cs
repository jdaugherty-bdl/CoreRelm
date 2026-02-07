using MySql.Data.MySqlClient;
using CoreRelm.RelmInternal.Helpers.Operations;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.Connections
{
    internal class StandardConnectionHelper
    {
        /// <summary>
        /// Executes a specified action within the context of a standard database connection and transaction.
        /// </summary>
        /// <remarks>This method ensures that the database connection and transaction are properly
        /// managed, including opening, committing, or rolling back the transaction as needed. The <paramref
        /// name="actionWrapper"/> delegate is executed within the context of the connection and transaction. If an
        /// exception occurs, the optional <paramref name="exceptionHandler"/> delegate is invoked to handle the
        /// exception.</remarks>
        /// <param name="connectionName">An enumeration value representing the name or type of the database connection to use.</param>
        /// <param name="actionWrapper">A delegate that defines the action to perform using the provided <see cref="MySqlConnection"/> and <see
        /// cref="MySqlTransaction"/>.</param>
        /// <param name="exceptionHandler">An optional delegate to handle exceptions that occur during the execution of the action. The delegate
        /// receives the exception and an error message.</param>
        public static void StandardConnectionWrapper(Enum connectionName, Action<MySqlConnection, MySqlTransaction> actionWrapper, Action<Exception, string>? exceptionHandler = null)
        {
            StandardConnectionWrapper(connectionName,
                (conn, transaction) =>
                {
                    actionWrapper(conn, transaction);

                    return true;
                }, exceptionHandler: exceptionHandler);
        }

        /// <summary>
        /// Executes a specified action within the context of a standard database connection and transaction.
        /// </summary>
        /// <remarks>This method ensures that the database connection and transaction are properly
        /// managed, including opening, committing, or rolling back the transaction as needed. The <paramref
        /// name="actionWrapper"/> delegate is executed within the context of the connection and transaction. If an
        /// exception occurs, the optional <paramref name="exceptionHandler"/> delegate is invoked to handle the
        /// exception.</remarks>
        /// <param name="connectionName">An enumeration value representing the name or type of the database connection to use.</param>
        /// <param name="actionWrapper">A delegate that defines the action to perform using the provided <see cref="MySqlConnection"/> and <see
        /// cref="MySqlTransaction"/>.</param>
        /// <param name="exceptionHandler">An optional delegate to handle exceptions that occur during the execution of the action. The delegate
        /// receives the exception and an error message.</param>
        public static async Task StandardConnectionWrapperAsync(Enum connectionName, Func<MySqlConnection, MySqlTransaction, CancellationToken, Task> actionWrapper, Action<Exception, string>? exceptionHandler = null, CancellationToken cancellationToken = default)
        {
            await StandardConnectionWrapperAsync(
                connectionName,
                async (conn, transaction, cancellationToken) => await actionWrapper(conn, transaction, cancellationToken), 
                exceptionHandler: exceptionHandler, 
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Executes a database operation within the context of a MySQL connection and transaction,  ensuring proper
        /// resource management and error handling.
        /// </summary>
        /// <remarks>This method ensures that the connection is opened, the transaction is started, and
        /// resources are  properly disposed of after the operation completes. If the operation completes successfully,
        /// the  transaction is committed. If an exception occurs, the transaction is rolled back, and the exception  is
        /// either passed to the <paramref name="exceptionHandler"/> or rethrown.</remarks>
        /// <typeparam name="T">The type of the result returned by the operation.</typeparam>
        /// <param name="connectionName">An <see cref="Enum"/> value representing the type of connection to use.</param>
        /// <param name="actionWrapper">A delegate that defines the operation to perform. The delegate receives the open  <see
        /// cref="MySqlConnection"/> and the associated <see cref="MySqlTransaction"/> as parameters.</param>
        /// <param name="exceptionHandler">An optional delegate to handle exceptions that occur during the operation. The delegate receives  the
        /// exception and its message as parameters. If not provided, exceptions are rethrown without additional
        /// handling.</param>
        /// <returns>The result of the operation defined by <paramref name="actionWrapper"/>.</returns>
        public static T StandardConnectionWrapper<T>(Enum connectionName, Func<MySqlConnection, MySqlTransaction, T> actionWrapper, Action<Exception, string>? exceptionHandler = null)
        {
            return StandardConnectionWrapperAsync(connectionName,
                async (conn, transaction, cancellationToken) => actionWrapper(conn, transaction), 
                exceptionHandler: exceptionHandler)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Executes a database operation within the context of a MySQL connection and transaction,  ensuring proper
        /// resource management and error handling.
        /// </summary>
        /// <remarks>This method ensures that the connection is opened, the transaction is started, and
        /// resources are  properly disposed of after the operation completes. If the operation completes successfully,
        /// the  transaction is committed. If an exception occurs, the transaction is rolled back, and the exception  is
        /// either passed to the <paramref name="exceptionHandler"/> or rethrown.</remarks>
        /// <typeparam name="T">The type of the result returned by the operation.</typeparam>
        /// <param name="connectionName">An <see cref="Enum"/> value representing the type of connection to use.</param>
        /// <param name="actionWrapper">A delegate that defines the operation to perform. The delegate receives the open  <see
        /// cref="MySqlConnection"/> and the associated <see cref="MySqlTransaction"/> as parameters.</param>
        /// <param name="exceptionHandler">An optional delegate to handle exceptions that occur during the operation. The delegate receives  the
        /// exception and its message as parameters. If not provided, exceptions are rethrown without additional
        /// handling.</param>
        /// <returns>The result of the operation defined by <paramref name="actionWrapper"/>.</returns>
        public static async Task<T> StandardConnectionWrapperAsync<T>(Enum connectionName, Func<MySqlConnection, MySqlTransaction, CancellationToken, Task<T>> actionWrapper, Action<Exception, string>? exceptionHandler = null, CancellationToken cancellationToken = default)
        {
            using (var conn = RelmHelper.ConnectionHelper?.GetConnectionFromType(connectionName))
            {
                await conn?.OpenAsync(cancellationToken);

                if (conn == null)
                    throw new IOException("MySQL connection is null");

                var transaction = await conn.BeginTransactionAsync(cancellationToken);

                try
                {
                    var actionResult = await actionWrapper(conn, transaction, cancellationToken);

                    if (transaction != null)
                    {
                        if ((conn?.State ?? ConnectionState.Broken) == ConnectionState.Open)
                            await transaction?.CommitAsync(cancellationToken);
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
                        await transaction.RollbackAsync(cancellationToken);
                    }
                    catch { }

                    exceptionHandler?.Invoke(ex, ex.Message);

                    throw;
                }
                finally
                {
                    if ((conn?.State ?? ConnectionState.Broken) == ConnectionState.Open)
                        await conn?.CloseAsync();
                }
            }
        }
    }
}
