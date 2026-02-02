using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations
{

    public sealed record ValidatedModelType(
        Type ClrType,
        string DatabaseName,
        string TableName
    );
}
