using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class RelmDatabase : Attribute
    {
        private string _databaseName;

        public string DatabaseName
        {
            get { return _databaseName; }
            set { _databaseName = value; }
        }

        public RelmDatabase(string TableName)
        {
            if (string.IsNullOrWhiteSpace(TableName))
                throw new ArgumentNullException(nameof(TableName));

            _databaseName = TableName;
        }
    }
}
