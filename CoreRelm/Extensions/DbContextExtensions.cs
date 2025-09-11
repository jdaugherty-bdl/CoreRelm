using CoreRelm.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Extensions
{
    public static class DbContextExtensions
    {
        /// <summary>
        /// Sets the InnoDB lock wait timeout for the current database session
        /// </summary>
        /// <param name="context">The database context</param>
        /// <param name="seconds">Timeout in seconds (default: 300 seconds/5 minutes)</param>
        public static void SetLockWaitTimeout(this IRelmContext context, int seconds = 300)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            RelmHelper.DoDatabaseWork(context, $"SET innodb_lock_wait_timeout = {seconds}");
        }
    }
}
