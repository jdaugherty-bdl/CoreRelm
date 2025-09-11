using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.Paths
{
    internal class PathProvider
    {
        public static string GetCurrentPath(IServiceProvider? serviceProvider = null)
        {
            // Attempt to get IWebHostEnvironment via DI
            if (serviceProvider != null)
            {
                try
                {
                    var webHostEnvironment = serviceProvider.GetService(typeof(IWebHostEnvironment)) as IWebHostEnvironment;
                    if (webHostEnvironment != null)
                    {
                        return webHostEnvironment.WebRootPath;
                    }
                }
                catch
                {
                    // Ignore if not a web app or missing dependencies
                }
            }

            // Fallback to the console path
            return AppContext.BaseDirectory;
        }
    }
}
