using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces
{
    public interface IRelmFieldLoaderBase
    {
        string FieldName { get; }
        string[] KeyFields { get; }
        Dictionary<S[], object> GetFieldData<S>(ICollection<S[]> keyData);
    }
}
