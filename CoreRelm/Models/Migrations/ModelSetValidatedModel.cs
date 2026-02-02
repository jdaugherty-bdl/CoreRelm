using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations
{
    public sealed record ModelSetValidatedModel(
        string TypeName,
        string DatabaseName,
        string TableName
    );
}
