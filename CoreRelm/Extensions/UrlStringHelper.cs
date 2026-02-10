using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Extensions
{
    public static class UrlStringHelper
    {
        public static string Slugify(string s)
        {
            // keep simple & deterministic
            s = s.Trim().ToLowerInvariant();
            var chars = s.Select(ch =>
                char.IsLetterOrDigit(ch) 
                    ? ch 
                    : (ch is '-' or '_' 
                        ? ch 
                        : '_'));
            var slug = new string([.. chars]);

            // collapse runs of underscores
            while (slug.Contains("__", StringComparison.Ordinal))
                slug = slug.Replace("__", "_", StringComparison.Ordinal);

            return slug.Trim('_');
        }
    }
}
