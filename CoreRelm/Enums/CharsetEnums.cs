using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Enums
{
    public class CharsetEnums
    {
        public enum DatabaseCharset
        {
            [Description("default")]
            Default,
            [Description("utf8mb4")]
            Utf8mb4,
            [Description("latin1")]
            Latin1,
            [Description("ascii")]
            Ascii,
        }

        public enum DatabaseCollation
        {
            [Description("default")]
            Default,
            [Description("utf8mb4_general_ci")]
            Utf8mb4GeneralCi,
            [Description("utf8mb4_bin")]
            Utf8mb4Bin,
            [Description("utf8mb4_general_cs")]
            Utf8mb4GeneralCs,
            [Description("utf8mb4_bin_ci")]
            Utf8mb4BinCi,
            [Description("utf8mb4_0900_ai_ci")]
            Utf8mb40900AiCi,
            [Description("utf8mb4_unicode_ci")]
            Utf8mb4UnicodeCi,
            [Description("latin1_swedish_ci")]
            Latin1SwedishCi,
            [Description("ascii_general_ci")]
            AsciiGeneralCi,
        }
    }
}
