using CoreRelm.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Extensions
{
    /// <summary>
    /// Provides extension methods for configuring and interacting with database contexts.
    /// </summary>
    /// <remarks>This class contains utility methods that extend the functionality of database contexts,  such
    /// as setting session-specific database parameters. These methods are designed to simplify  common database
    /// operations and enhance developer productivity when working with database contexts.</remarks>
    public static class DbContextExtensions
    {
        /// <summary>
        /// Sets the InnoDB lock wait timeout for the current database session.
        /// </summary>
        /// <remarks>This method executes a SQL command to set the InnoDB lock wait timeout for the
        /// current session. The timeout determines how long a transaction will wait for a lock before timing
        /// out.</remarks>
        /// <param name="context">The database context. This parameter cannot be <see langword="null"/>.</param>
        /// <param name="seconds">The lock wait timeout, in seconds. The default value is 300 seconds (5 minutes).</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="context"/> is <see langword="null"/>.</exception>
        public static void SetLockWaitTimeout(this IRelmContext context, int seconds = 300)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            RelmHelper.DoDatabaseWork(context, $"SET innodb_lock_wait_timeout = {seconds}");
        }
    }
}
