using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.EventArguments
{
    public class DtoEventArgs : EventArgs
    {
        public Dictionary<string, object> AdditionalObjectProperties { get; set; }
    }
}