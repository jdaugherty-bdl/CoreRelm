using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class RelmTable : Attribute
    {
        private string _tableName;

        public string TableName
        {
            get { return _tableName; }
            set { _tableName = value; }
        }

        public RelmTable(string TableName)
        {
            _tableName = TableName;
        }
    }
}
