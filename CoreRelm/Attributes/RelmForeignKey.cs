using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Struct)]
    public sealed class RelmForeignKey : Attribute
    {
        public string[] ForeignKeys { get; set; } = default;
        public string[] LocalKeys { get; set; } = default;
        public string[] OrderBy { get; set; } = default;

        public RelmForeignKey(string ForeignKey = null, string LocalKey = null, string OrderBy = null)
        {
            if (ForeignKey != null)
                this.ForeignKeys = new string[] { ForeignKey };

            if (LocalKey != null)
                this.LocalKeys = new string[] { LocalKey };

            if (OrderBy != null)
                this.OrderBy = new string[] { OrderBy };
        }

        public RelmForeignKey(string[] ForeignKeys = null, string[] LocalKeys = null, string[] OrderBy = null)
        {
            this.ForeignKeys = ForeignKeys;
            this.LocalKeys = LocalKeys;
            this.OrderBy = OrderBy;
        }
    }
}
