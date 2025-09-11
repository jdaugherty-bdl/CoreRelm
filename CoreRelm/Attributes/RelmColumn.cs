using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Struct)]
    public sealed class RelmColumn : Attribute
    {
        public string ColumnName { get; private set; }
        public int ColumnSize { get; private set; }
        public int[] CompoundColumnSize { get; private set; }
        public bool AllowDataTruncation { get; private set; }
        public bool IsNullable { get; private set; }
        public bool PrimaryKey { get; private set; }
        public bool Autonumber { get; private set; }
        public bool Unique { get; private set; }
        public string DefaultValue { get; private set; }
        public string Index { get; private set; }
        public bool IndexDescending { get; private set; }
        public bool Virtual { get; private set; }

        public RelmColumn(string ColumnName = null, int ColumnSize = -1, int[] CompoundColumnSize = null, bool IsNullable = true, bool PrimaryKey = false, bool Autonumber = false, bool Unique = false, string DefaultValue = null, string Index = null, bool IndexDescending = false, bool AllowDataTruncation = false, bool Virtual = false)
        {
            this.ColumnName = ColumnName;
            this.ColumnSize = ColumnSize;
            this.CompoundColumnSize = CompoundColumnSize;
            this.IsNullable = IsNullable;
            this.PrimaryKey = PrimaryKey;
            this.Autonumber = Autonumber;
            this.Unique = Unique;
            this.DefaultValue = DefaultValue;
            this.Index = Index;
            this.IndexDescending = IndexDescending;
            this.AllowDataTruncation = AllowDataTruncation;
            this.Virtual = Virtual;
        }
    }
}
