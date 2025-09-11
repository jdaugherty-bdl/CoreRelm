using CoreRelm.Interfaces;
using CoreRelm.Interfaces.RelmQuick;
using CoreRelm.Models;
using CoreRelm.Options;
using CoreRelm.RelmInternal.Helpers.Operations;
using CoreRelm.RelmInternal.Helpers.Utilities;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.DataCollection
{
    internal class DatabaseWorkHelper
    {
        /// <summary>
        /// Caches the last execution error encountered
        /// </summary>
        internal static Exception LastExecutionException;
        /// <summary>
        /// Convenience function to get the last exception message
        /// </summary>
        internal static string LastExecutionError => LastExecutionException?.Message;
        /// <summary>
        /// Convenience function to check if there's an error cached
        /// </summary>
        internal static bool HasError => LastExecutionException != null;

        /// <summary>
        /// Execute a non-query on the database with the specified parameters without returning a value
        /// </summary>
        /// <param name="ConfigConnectionString">A ConnectionStringTypes type to reference a connection string defined in web.config</param>
        /// <param name="QueryString">SQL query to execute</param>
        /// <param name="Parameters">Dictionary of named parameters</param>
        /// <param name="ThrowException">Throw swallow exception</param>
        internal static void DoDatabaseWork(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false)
        {
            using (var conn = ConnectionHelper.GetConnectionFromType(ConfigConnectionString, AllowUserVariables))
            {
                DoDatabaseWork(conn, QueryString, Parameters: Parameters, ThrowException: ThrowException, UseTransaction: UseTransaction || SqlTransaction != null, SqlTransaction: SqlTransaction);
            }
        }

        /// <summary>
        /// Execute a non-query on the database with the specified parameters without returning a value
        /// </summary>
        /// <param name="EstablishedConnection">An open and established connection to a MySQL database</param>
        /// <param name="QueryString">SQL query to execute</param>
        /// <param name="Parameters">Dictionary of named parameters</param>
        /// <param name="ThrowException">Throw swallow exception</param>
        internal static void DoDatabaseWork(MySqlConnection EstablishedConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null)
        {
            /*
            DoDatabaseWork(EstablishedConnection, QueryString,
                (cmd) =>
                {
                    cmd.Parameters.AddAllParameters(Parameters);

                    return cmd.ExecuteNonQuery();
                },
                ThrowException: ThrowException, UseTransaction: UseTransaction || SqlTransaction != null, SqlTransaction: SqlTransaction);
            */
            DoDatabaseWork(new RelmContext(EstablishedConnection, SqlTransaction), QueryString, Parameters: Parameters, ThrowException: ThrowException, UseTransaction: UseTransaction);
        }

        /// <summary>
        /// Execute a non-query on the database with the specified parameters without returning a value
        /// </summary>
        /// <param name="EstablishedConnection">An open and established connection to a MySQL database</param>
        /// <param name="QueryString">SQL query to execute</param>
        /// <param name="Parameters">Dictionary of named parameters</param>
        /// <param name="ThrowException">Throw swallow exception</param>
        internal static void DoDatabaseWork(IRelmContext relmContext, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false)
        {
            DoDatabaseWork<int>(relmContext, QueryString,
                (cmd) =>
                {
                    cmd.Parameters.AddAllParameters(Parameters);

                    return cmd.ExecuteNonQuery();
                },
                ThrowException: ThrowException, UseTransaction: UseTransaction || relmContext.ContextOptions.DatabaseTransaction != null);
        }

        /// <summary>
        /// Execute a non-query on the database and return the number of rows affected
        /// </summary>
        /// <typeparam name="T">Return type - only accepts String or Int</typeparam>
        /// <param name="ConfigConnectionString">A ConnectionStringTypes type to reference a connection string defined in web.config</param>
        /// <param name="QueryString">SQL query to execute</param>
        /// <param name="Parameters">Dictionary of named parameters</param>
        /// <param name="ThrowException">Throw exception or swallow and return null</param>
        /// <param name="UseTransaction">Specify whether to use a transaction for this call</param>
        /// <returns>Number of rows affected</returns>
        internal static T DoDatabaseWork<T>(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false)
        {
            using (var conn = ConnectionHelper.GetConnectionFromType(ConfigConnectionString, AllowUserVariables))
            {
                return DoDatabaseWork<T>(conn, QueryString, Parameters: Parameters, ThrowException: ThrowException, UseTransaction: UseTransaction || SqlTransaction != null, SqlTransaction: SqlTransaction);
            }
        }

        /// <summary>
        /// Execute a non-query on the database and return the generic type specified
        /// </summary>
        /// <typeparam name="T">Return type - only accepts String or Int</typeparam>
        /// <param name="EstablishedConnection">An open and established connection to a MySQL database</param>
        /// <param name="QueryString">SQL query to execute</param>
        /// <param name="Parameters">Dictionary of named parameters</param>
        /// <param name="ThrowException">Throw exception or swallow and return null</param>
        /// <param name="UseTransaction">Specify whether to use a transaction for this call</param>
        /// <returns>Data in the type specified</returns>
        internal static T DoDatabaseWork<T>(MySqlConnection EstablishedConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null)
        {
            return DoDatabaseWork<T>(new RelmContext(EstablishedConnection, SqlTransaction), QueryString, Parameters: Parameters, ThrowException: ThrowException, UseTransaction: UseTransaction);
        }

        internal static T DoDatabaseWork<T>(IRelmQuickContext relmContext, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false)
        {
            return DoDatabaseWork<T>(relmContext, QueryString,
                (cmd) =>
                {
                    cmd.Parameters.AddAllParameters(Parameters);

                    var executionWork = cmd.ExecuteNonQuery();

                    if (typeof(T) == typeof(string))
                        return executionWork.ToString();
                    else if (typeof(T) == typeof(bool))
                        return executionWork > 0;
                    else if (typeof(T) == typeof(int))
                        return executionWork;
                    else
                        return default;
                },
                //ThrowException: ThrowException, UseTransaction: UseTransaction || SqlTransaction != null, SqlTransaction: SqlTransaction);
                ThrowException: ThrowException, UseTransaction: UseTransaction || relmContext.ContextOptions.DatabaseTransaction != null);
        }

        internal static T DoDatabaseWork<T>(IRelmContext relmContext, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false)
        {
            return DoDatabaseWork<T>(relmContext, QueryString,
                (cmd) =>
                {
                    cmd.Parameters.AddAllParameters(Parameters);

                    var executionWork = cmd.ExecuteNonQuery();

                    if (typeof(T) == typeof(string))
                        return executionWork.ToString();
                    else if (typeof(T) == typeof(bool))
                        return executionWork > 0;
                    else if (typeof(T) == typeof(int))
                        return executionWork;
                    else
                        return default;
                },
                //ThrowException: ThrowException, UseTransaction: UseTransaction || SqlTransaction != null, SqlTransaction: SqlTransaction);
                ThrowException: ThrowException, UseTransaction: UseTransaction || relmContext.ContextOptions.DatabaseTransaction != null);
        }

        /// <summary>
        /// Execute a query on the database using the provided function without returning a value
        /// </summary>
        /// <param name="ConfigConnectionString">A ConnectionStringTypes type to reference a connection string defined in web.config</param>
        /// <param name="QueryString">SQL query to execute</param>
        /// <param name="ActionCallback">Customized function to execute when connected to the database</param>
        /// <param name="ThrowException">Throw or swallow exception</param>
        /// <param name="UseTransaction">Specify whether to use a transaction for this call</param>
        internal static void DoDatabaseWork(Enum ConfigConnectionString, string QueryString, Func<MySqlCommand, object> ActionCallback, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false)
        {
            DoDatabaseWork<object>(ConfigConnectionString, QueryString, ActionCallback, ThrowException: ThrowException, UseTransaction: UseTransaction || SqlTransaction != null, SqlTransaction: SqlTransaction, AllowUserVariables: AllowUserVariables);
        }

        /// <summary>
        /// Execute a query on the database using the provided function without returning a value
        /// </summary>
        /// <param name="EstablishedConnection">An open and established connection to a MySQL database</param>
        /// <param name="QueryString">SQL query to execute</param>
        /// <param name="ActionCallback">Customized function to execute when connected to the database</param>
        /// <param name="ThrowException">Throw or swallow exception</param>
        /// <param name="UseTransaction">Specify whether to use a transaction for this call</param>
        internal static void DoDatabaseWork(MySqlConnection EstablishedConnection, string QueryString, Func<MySqlCommand, object> ActionCallback, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null)
        {
            DoDatabaseWork<object>(EstablishedConnection, QueryString, ActionCallback, ThrowException: ThrowException, UseTransaction: UseTransaction || SqlTransaction != null, SqlTransaction: SqlTransaction);
        }

        /// <summary>
        /// Execute a query on the database using the provided function, returning value of type T
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="ConfigConnectionString">A ConnectionStringTypes type to reference a connection string defined in web.config</param>
        /// <param name="QueryString">SQL query to retrieve the data requested</param>
        /// <param name="ActionCallback">Customized function to execute when connected to the database</param>
        /// <param name="ThrowException">Throw exception or swallow and return default(T)</param>
        /// <param name="UseTransaction">Specify whether to use a transaction for this call</param>
        /// <returns>Data of any type T</returns>
        internal static T DoDatabaseWork<T>(Enum ConfigConnectionString, string QueryString, Func<MySqlCommand, object> ActionCallback, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false)
        {
            using (var conn = ConnectionHelper.GetConnectionFromType(ConfigConnectionString, AllowUserVariables))
            {
                return DoDatabaseWork<T>(conn, QueryString, ActionCallback, ThrowException: ThrowException, UseTransaction: UseTransaction || SqlTransaction != null, SqlTransaction: SqlTransaction);
            }
        }

        /// <summary>
        /// Execute a query on the database using the provided function, returning value of type T
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="EstablishedConnection">An open and established connection to a MySQL database</param>
        /// <param name="QueryString">SQL query to retrieve the data requested</param>
        /// <param name="ActionCallback">Customized function to execute when connected to the database</param>
        /// <param name="ThrowException">Throw exception or swallow and return default(T)</param>
        /// <param name="UseTransaction">Specify whether to use a transaction for this call</param>
        /// <param name="SqlTransaction">An existing SQL transaction</param>
        /// <returns>Data of any type T</returns>
        internal static T DoDatabaseWork<T>(MySqlConnection EstablishedConnection, string QueryString, Func<MySqlCommand, object> ActionCallback, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null)
        {
            return DoDatabaseWork<T>(new RelmContext(EstablishedConnection, SqlTransaction), QueryString, ActionCallback, ThrowException: ThrowException, UseTransaction: UseTransaction);
        }

        internal static T DoDatabaseWork<T>(IRelmContext relmContext, string QueryString, Func<MySqlCommand, object> ActionCallback, bool ThrowException = true, bool UseTransaction = false)
        {
            return DoDatabaseWork<T>(relmContext.ContextOptions, QueryString, ActionCallback, ThrowException: ThrowException, UseTransaction: UseTransaction || relmContext.ContextOptions.DatabaseTransaction != null);
        }

        internal static T DoDatabaseWork<T>(IRelmQuickContext relmContext, string QueryString, Func<MySqlCommand, object> ActionCallback, bool ThrowException = true, bool UseTransaction = false)
        {
            return DoDatabaseWork<T>(relmContext.ContextOptions, QueryString, ActionCallback, ThrowException: ThrowException, UseTransaction: UseTransaction || relmContext.ContextOptions.DatabaseTransaction != null);
        }

        internal static T DoDatabaseWork<T>(RelmContextOptionsBuilder contextOptions, string QueryString, Func<MySqlCommand, object> ActionCallback, bool ThrowException = true, bool UseTransaction = false)
        {
            var internalOpen = false; // indicates whether the connection was already open or not
            var openedNewTransaction = false; // indicates whether a new transaction was created here or not
            //var currentTransaction = SqlTransaction; // preload current transaction

            // reset the last execution error
            LastExecutionException = null;

            try
            {
                // if the connection isn't open, then open it and record that we did that
                //if (EstablishedConnection.State != ConnectionState.Open)
                if (contextOptions.DatabaseConnection.State != ConnectionState.Open)
                {
                    //EstablishedConnection.Open();
                    contextOptions.DatabaseConnection.Open();
                    internalOpen = true;
                }

                // if the caller wants to use transactions but they didn't provide one, create a new one
                //if (UseTransaction && SqlTransaction == null)
                if (UseTransaction && contextOptions.DatabaseTransaction == null)
                {
                    //currentTransaction = EstablishedConnection.BeginTransaction();
                    contextOptions.SetDatabaseTransaction(contextOptions.DatabaseConnection.BeginTransaction());
                    openedNewTransaction = true;
                }

                // execute the SQL
                //using (var cmd = new MySqlCommand(QueryString, EstablishedConnection))
                using (var cmd = new MySqlCommand(QueryString, contextOptions.DatabaseConnection, contextOptions.DatabaseTransaction))
                {
                    cmd.CommandTimeout = int.MaxValue;

                    // execute whatever code the caller provided
                    var result = (T)ActionCallback(cmd);

                    // if we opened the transaction here, just commit it because we're going to be closing it right away
                    if (openedNewTransaction)
                        //currentTransaction?.Commit();
                        contextOptions.DatabaseTransaction?.Commit();

                    return result;
                }
            }
            catch (MySqlException mysqlEx) // use special handling for MySQL exceptions
            {
                // there was an error, roll back the transaction
                if (UseTransaction)
                    //currentTransaction?.Rollback();
                    contextOptions.DatabaseTransaction?.Rollback();

                // if we want exceptions to be thrown, rethrow the current one, otherwise just record the error
                if (ThrowException)
                    throw new Exception(mysqlEx.Message, mysqlEx);
                else
                {
                    LastExecutionException = mysqlEx;

                    return default;
                }
            }
            catch (Exception ex) // handle all other unhandled exceptions
            {
                // there was an error, roll back the transaction
                if (UseTransaction)
                    //currentTransaction?.Rollback();
                    contextOptions.DatabaseTransaction?.Rollback();

                // if we want exceptions to be thrown, rethrow the current one, otherwise just record the error
                if (ThrowException)
                    throw new Exception(ex.Message, ex);
                else
                {
                    LastExecutionException = ex;

                    return default;
                }
            }
            finally
            {
                // if we opened the connection, close it back up before it's disposed
                /*
                if (internalOpen && EstablishedConnection.State == ConnectionState.Open)
                    EstablishedConnection.Close();
                */
                if (internalOpen && contextOptions.DatabaseConnection.State == ConnectionState.Open)
                    contextOptions.DatabaseConnection.Close();
            }
        }
    }
}
