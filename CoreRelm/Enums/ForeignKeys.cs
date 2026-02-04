using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Enums
{
    public class ForeignKeys
    {
        public enum ReferentialAction
        {
            NoAction,
            Cascade,
            SetNull,
            SetDefault,
            Restrict
        }
    }
}
