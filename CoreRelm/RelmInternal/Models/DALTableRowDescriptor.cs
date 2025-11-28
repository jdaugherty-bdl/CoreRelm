using CoreRelm.Attributes;
using CoreRelm.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Models
{
    /// <summary>
    /// This class is used to gather information about database tables and their columns to enable automatic bulk table writes.
    /// </summary>
    internal class DALTableRowDescriptor : RelmModel
    {
        [RelmColumn]
        public string Field { get; set; }
        [RelmColumn]
        public string Type { get; set; }
        [RelmColumn]
        public string Null { get; set; }
        [RelmColumn]
        public string Key { get; set; }
        [RelmColumn]
        public string Default { get; set; }
        [RelmColumn]
        public string Extra { get; set; }

        public string TableName { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsAutoIncrement { get; set; }
        public bool IsUniqueConstraint { get; set; }
    }
}
