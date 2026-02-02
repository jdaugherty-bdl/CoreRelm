using CoreRelm.Models.Migrations.Introspection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces.Migrations
{
    public interface IMigrationOperation
    {
        string Description { get; }
    }
}
