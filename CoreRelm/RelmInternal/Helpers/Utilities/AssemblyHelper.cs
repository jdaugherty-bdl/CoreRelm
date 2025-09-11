using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.Utilities
{
    internal class AssemblyHelper
    {
        public static Assembly GetEntryAssembly()
        {
            return Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly() ?? GetWebEntryAssembly();
        }

        static private Assembly GetWebEntryAssembly()
        {
            var type = System.Web.HttpContext.Current?.ApplicationInstance?.GetType();
            while (type?.Namespace == "ASP")
            {
                type = type.BaseType;
            }

            return type?.Assembly;
        }
    }
}
